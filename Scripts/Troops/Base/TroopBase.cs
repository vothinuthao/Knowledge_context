using System.Collections.Generic;
using Core;
using Core.Behaviors;
using Core.Patterns;
using Troops.Config;
using UnityEngine;

namespace Troops.Base
{
    /// <summary>
    /// Base class for all troops (friendly and enemy)
    /// </summary>
    public class TroopBase : MonoBehaviour
    {
        [Header("Components")] [SerializeField]
        protected Animator animator;

        [SerializeField] protected Collider troopCollider;
        [SerializeField] protected Rigidbody troopRigidbody;

        [Header("Configuration")] [SerializeField]
        protected TroopConfigSO config;

        [Header("Runtime State")] [SerializeField]
        protected int currentHealth;

        [SerializeField] protected TroopState currentState;
        [SerializeField] protected Transform formationPositionTarget;
        [SerializeField] protected Transform squadMoveTarget;
        [SerializeField] protected Transform attackTarget;

        // Behavior management
        protected SteeringManager steeringManager;
        protected ITroopState activeState;
        protected List<ITroopBehaviorStrategy> behaviorStrategies = new List<ITroopBehaviorStrategy>();

        // Properties
        public TroopState CurrentState => currentState;
        public float CurrentHealthPercentage => (float)currentHealth / config.health;
        public bool IsSquadMoving => squadMoveTarget != null;
        public bool HasEnemyInRange => attackTarget != null;

        public bool IsInAttackRange => attackTarget != null &&
                                       Vector3.Distance(transform.position, attackTarget.position) <=
                                       config.attackRange;

        public Transform NearestEnemyTransform => attackTarget;
        public Transform AttackTarget => attackTarget;
        public Transform FormationPositionTarget => formationPositionTarget;
        public Transform SquadMoveTarget => squadMoveTarget;
        public float AttackRate => config.attackRate;
        public SteeringManager SteeringManager => steeringManager;
        public bool IsPlayingDeathAnimation { get; protected set; }

        void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (troopCollider == null) troopCollider = GetComponent<Collider>();
            if (troopRigidbody == null) troopRigidbody = GetComponent<Rigidbody>();
            steeringManager = GetComponent<SteeringManager>();
            if (steeringManager == null)
            {
                steeringManager = gameObject.AddComponent<SteeringManager>();
            }

            AddDefaultBehaviorStrategies();
        }

        void Start()
        {
            ChangeState(new IdleState());
        }

        void Update()
        {
            if (activeState != null)
            {
                activeState.Update(this);
            }
        }

        /// <summary>
        /// Initialize the troop with configuration
        /// </summary>
        public virtual void Initialize(TroopConfigSO config)
        {
            this.config = config;
            currentHealth = config.health;
            List<ISteeringComponent> behaviors = config.CreateBehaviors();
            foreach (var behavior in behaviors)
            {
                steeringManager.AddBehavior(behavior);
            }

            SteeringContext context = new SteeringContext();
            config.ApplyToContext(context);
        }

        /// <summary>
        /// Change to a new state
        /// </summary>
        public void ChangeState(ITroopState newState)
        {
            if (activeState != null)
            {
                activeState.Exit(this);
            }

            activeState = newState;
            currentState = newState.GetStateType();
            activeState.Enter(this);
        }

        /// <summary>
        /// Add default behavior strategies
        /// </summary>
        protected virtual void AddDefaultBehaviorStrategies()
        {
            behaviorStrategies.Add(new IdleStrategy());
            behaviorStrategies.Add(new MoveStrategy());
            behaviorStrategies.Add(new AttackStrategy());
            behaviorStrategies.Add(new FleeStrategy());
            behaviorStrategies.Add(new DeadStrategy());
        }

        /// <summary>
        /// Execute the highest priority behavior strategy that should execute
        /// </summary>
        protected virtual void ExecuteHighestPriorityStrategy()
        {
            behaviorStrategies.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            foreach (var strategy in behaviorStrategies)
            {
                if (strategy.ShouldExecute(this))
                {
                    strategy.Execute(this);
                    break;
                }
            }
        }

        /// <summary>
        /// Set the formation position target
        /// </summary>
        public void SetFormationPositionTarget(Transform target)
        {
            formationPositionTarget = target;
        }

        /// <summary>
        /// Set the squad move target
        /// </summary>
        public void SetSquadMoveTarget(Transform target)
        {
            squadMoveTarget = target;
        }

        /// <summary>
        /// Clear the squad move target
        /// </summary>
        public void ClearSquadMoveTarget()
        {
            squadMoveTarget = null;
        }

        /// <summary>
        /// Set the attack target
        /// </summary>
        public void SetAttackTarget(Transform target)
        {
            attackTarget = target;
        }

        /// <summary>
        /// Take damage
        /// </summary>
        public virtual void TakeDamage(float damage)
        {
            currentHealth -= Mathf.RoundToInt(damage);

            EventManager.Instance.TriggerEvent(EventTypeInGame.TroopDamaged, damage);
            if (currentHealth <= 0)
            {
                currentHealth = 0;
                Die();
            }
        }

        /// <summary>
        /// Die
        /// </summary>
        protected virtual void Die()
        {
            ChangeState(new DeadState());
        }

        /// <summary>
        /// Attack
        /// </summary>
        public virtual void Attack()
        {
            if (attackTarget == null) return;
            TroopBase targetTroop = attackTarget.GetComponent<TroopBase>();

            if (targetTroop != null)
            {
                targetTroop.TakeDamage(config.attackDamage);
                EventManager.Instance.TriggerEvent(EventTypeInGame.TroopAttack,targetTroop);
            }
        }

        /// <summary>
        /// Play animation
        /// </summary>
        public virtual void PlayAnimation(string animationName)
        {
            if (animator != null)
            {
                animator.SetTrigger(animationName);
            }
        }

        /// <summary>
        /// Play death animation
        /// </summary>
        public virtual void PlayDeathAnimation()
        {
            if (animator != null)
            {
                animator.SetTrigger("Death");
                IsPlayingDeathAnimation = true;
            }
        }

        /// <summary>
        /// Disable collisions
        /// </summary>
        public virtual void DisableCollisions()
        {
            if (troopCollider != null)
            {
                troopCollider.enabled = false;
            }

            if (troopRigidbody != null)
            {
                troopRigidbody.isKinematic = true;
            }
        }

        /// <summary>
        /// Maintain formation (used in idle state)
        /// </summary>
        public virtual void MaintainFormation()
        {
            steeringManager.ClearBehaviors();

            // Add arrival behavior
            var arrival = new ArrivalBehavior();
            steeringManager.AddBehavior(arrival);

            // Set target to formation position
            steeringManager.SetTarget(formationPositionTarget);
        }

        /// <summary>
        /// Move with squad (used in moving state)
        /// </summary>
        public virtual void MoveWithSquad()
        {
            steeringManager.ClearBehaviors();

            // Add cohesion behavior
            var cohesion = new CohesionBehavior();
            steeringManager.AddBehavior(cohesion);

            // Add separation behavior
            var separation = new SeparationBehavior();
            steeringManager.AddBehavior(separation);

            // Add arrival behavior
            var arrival = new ArrivalBehavior();
            steeringManager.AddBehavior(arrival);

            // Add obstacle avoidance
            var obstacleAvoidance = new ObstacleAvoidanceBehavior();
            steeringManager.AddBehavior(obstacleAvoidance);

            // Set target to formation position
            steeringManager.SetTarget(formationPositionTarget);
        }

        /// <summary>
        /// Move to attack target (used in attacking state)
        /// </summary>
        public virtual void MoveToAttackTarget()
        {
            steeringManager.ClearBehaviors();

            // Add seek behavior
            var seek = new SeekBehavior();
            steeringManager.AddBehavior(seek);

            // Add separation behavior
            var separation = new SeparationBehavior();
            steeringManager.AddBehavior(separation);

            // Set target to attack target
            steeringManager.SetTarget(attackTarget);
        }

        /// <summary>
        /// Flee from threats (used in fleeing state)
        /// </summary>
        public virtual void FleeFromThreats()
        {
            steeringManager.ClearBehaviors();

            // Add flee behavior
            var flee = new FleeBehavior();
            steeringManager.AddBehavior(flee);

            // Set target to nearest enemy
            steeringManager.SetTarget(attackTarget);
        }
    }
}
    