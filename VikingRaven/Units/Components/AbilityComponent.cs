using UnityEngine;
using System.Collections;
using VikingRaven.Core.ECS;

namespace VikingRaven.Units.Components
{
    /// <summary>
    /// Component for handling unit special abilities
    /// </summary>
    public class AbilityComponent : BaseComponent
    {
        [SerializeField] private string _abilityName;
        [SerializeField] private float _abilityCost;
        [SerializeField] private float _abilityCooldown;
        [SerializeField] private string _abilityParameters;
        
        // Internal state tracking
        private float _lastUseTime = -100f;
        private bool _isOnCooldown = false;
        private float _cooldownRemaining = 0f;
        private bool _isAbilityActive = false;
        private float _abilityDuration = 0f;
        
        // Effect visualization
        private GameObject _abilityEffectObject;
        private ParticleSystem _abilityParticles;
        
        // Events
        public delegate void AbilityEvent(string abilityName, Vector3 targetPosition, IEntity targetEntity);
        public event AbilityEvent OnAbilityActivated;
        public event AbilityEvent OnAbilityCompleted;
        
        // Public properties
        public string AbilityName => _abilityName;
        public float AbilityCost => _abilityCost;
        public float AbilityCooldown => _abilityCooldown;
        public string AbilityParameters => _abilityParameters;
        public bool IsReady => !_isOnCooldown && Time.time - _lastUseTime >= _abilityCooldown;
        public float CooldownRemaining => _cooldownRemaining;
        public bool IsAbilityActive => _isAbilityActive;
        
        /// <summary>
        /// Initialize the component
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            _isOnCooldown = false;
            _cooldownRemaining = 0f;
            _isAbilityActive = false;
        }
        
        /// <summary>
        /// Set ability parameters
        /// </summary>
        public void SetAbility(string name, float cost, float cooldown, string parameters)
        {
            _abilityName = name;
            _abilityCost = cost;
            _abilityCooldown = cooldown;
            _abilityParameters = parameters;
            
            // Parse parameters for ability duration if present
            if (!string.IsNullOrEmpty(parameters))
            {
                string[] paramPairs = parameters.Split(',');
                foreach (string param in paramPairs)
                {
                    string[] keyValue = param.Trim().Split('=');
                    if (keyValue.Length == 2)
                    {
                        if (keyValue[0].Trim().ToLower() == "duration")
                        {
                            if (float.TryParse(keyValue[1].Trim(), out float duration))
                            {
                                _abilityDuration = duration;
                            }
                        }
                    }
                }
            }
            
            // If no duration was specified, set a default
            if (_abilityDuration <= 0)
            {
                _abilityDuration = 3f; // Default duration
            }
        }
        
        /// <summary>
        /// Update component state
        /// </summary>
        private void Update()
        {
            if (!IsActive)
                return;
                
            // Update cooldown remaining
            if (_isOnCooldown)
            {
                _cooldownRemaining = Mathf.Max(0, _lastUseTime + _abilityCooldown - Time.time);
                if (_cooldownRemaining <= 0)
                {
                    _isOnCooldown = false;
                }
            }
        }
        
        /// <summary>
        /// Activate the ability
        /// </summary>
        /// <param name="targetPosition">Target position for the ability</param>
        /// <param name="targetEntity">Optional target entity</param>
        /// <returns>True if ability was activated successfully</returns>
        public bool ActivateAbility(Vector3 targetPosition, IEntity targetEntity = null)
        {
            if (!IsReady || string.IsNullOrEmpty(_abilityName))
                return false;
                
            // Execute ability based on name
            bool success = ExecuteAbilityLogic(targetPosition, targetEntity);
            
            if (success)
            {
                _lastUseTime = Time.time;
                _isOnCooldown = true;
                _cooldownRemaining = _abilityCooldown;
                
                // Trigger event
                OnAbilityActivated?.Invoke(_abilityName, targetPosition, targetEntity);
                
                // Start ability duration coroutine if this is a duration-based ability
                if (_abilityDuration > 0)// && IsActiveAndEnabled)
                {
                    StartCoroutine(AbilityDurationRoutine(targetPosition, targetEntity));
                }
                
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Handle ability duration and completion
        /// </summary>
        private IEnumerator AbilityDurationRoutine(Vector3 targetPosition, IEntity targetEntity)
        {
            _isAbilityActive = true;
            
            // Wait for ability duration
            yield return new WaitForSeconds(_abilityDuration);
            
            // Complete ability
            _isAbilityActive = false;
            
            // Execute any completion logic
            CompleteAbility(targetPosition, targetEntity);
            
            // Trigger completion event
            OnAbilityCompleted?.Invoke(_abilityName, targetPosition, targetEntity);
        }
        
        /// <summary>
        /// Execute ability-specific logic
        /// </summary>
        private bool ExecuteAbilityLogic(Vector3 targetPosition, IEntity targetEntity)
        {
            // Implementation for different ability types
            switch (_abilityName.ToLower())
            {
                case "charge":
                    return ExecuteCharge(targetPosition);
                    
                case "heal":
                    return ExecuteHeal();
                    
                case "rangedattack":
                    return ExecuteRangedAttack(targetPosition, targetEntity);
                    
                case "shield":
                    return ExecuteShield();
                    
                case "stealth":
                    return ExecuteStealth();
                    
                case "taunt":
                    return ExecuteTaunt(targetEntity);
                    
                default:
                    Debug.LogWarning($"AbilityComponent: Unknown ability {_abilityName}");
                    return false;
            }
        }
        
        /// <summary>
        /// Handle ability completion logic
        /// </summary>
        private void CompleteAbility(Vector3 targetPosition, IEntity targetEntity)
        {
            // Implementation for different ability types
            switch (_abilityName.ToLower())
            {
                case "charge":
                    CompleteCharge();
                    break;
                    
                case "shield":
                    CompleteShield();
                    break;
                    
                case "stealth":
                    CompleteStealth();
                    break;
                    
                // Other abilities might not need completion logic
                default:
                    break;
            }
            
            // Cleanup any visual effects
            if (_abilityEffectObject != null)
            {
                Destroy(_abilityEffectObject);
                _abilityEffectObject = null;
            }
        }
        
        #region Ability Implementations
        
        /// <summary>
        /// Execute Charge ability
        /// </summary>
        private bool ExecuteCharge(Vector3 targetPosition)
        {
            var transformComponent = Entity.GetComponent<TransformComponent>();
            var navigationComponent = Entity.GetComponent<NavigationComponent>();
            
            if (transformComponent == null || navigationComponent == null)
                return false;
                
            // Set destination with high priority
            navigationComponent.SetDestination(targetPosition, NavigationCommandPriority.Critical);
            
            // Apply speed boost
            float speedMultiplier = GetAbilityParamFloat("speedMultiplier", 2.0f);
            // float originalSpeed = navigationComponent.MoveSpeed;
            // navigationComponent.SetTemporaryMoveSpeed(originalSpeed * speedMultiplier, _abilityDuration);
            
            // Activate ability visuals
            CreateAbilityVisuals("Charge");
            
            return true;
        }
        
        /// <summary>
        /// Complete Charge ability
        /// </summary>
        private void CompleteCharge()
        {
            var navigationComponent = Entity.GetComponent<NavigationComponent>();
            if (navigationComponent != null)
            {
                // Reset speed if needed
                // navigationComponent.ResetMoveSpeed();
            }
        }
        
        /// <summary>
        /// Execute Heal ability
        /// </summary>
        private bool ExecuteHeal()
        {
            var healthComponent = Entity.GetComponent<HealthComponent>();
            if (healthComponent == null)
                return false;
                
            // Calculate heal amount
            float healPercent = GetAbilityParamFloat("healPercent", 0.25f);
            float healAmount = healthComponent.MaxHealth * healPercent;
            
            // Apply heal
            healthComponent.Heal(healAmount);
            
            // Create visual effect
            CreateAbilityVisuals("Heal");
            
            return true;
        }
        
        /// <summary>
        /// Execute RangedAttack ability
        /// </summary>
        private bool ExecuteRangedAttack(Vector3 targetPosition, IEntity targetEntity)
        {
            var combatComponent = Entity.GetComponent<CombatComponent>();
            if (combatComponent == null)
                return false;
                
            // If we have a target entity, attack directly
            if (targetEntity != null)
            {
                // combatComponent.RangedAttack(targetEntity);
            }
            else
            {
                // Otherwise, use area attack at position
                float radius = GetAbilityParamFloat("radius", 3.0f);
                float damage = GetAbilityParamFloat("damage", combatComponent.AttackDamage * 1.5f);
                
                // Create projectile visual
                StartCoroutine(RangedAttackCoroutine(targetPosition, radius, damage));
            }
            
            return true;
        }
        
        /// <summary>
        /// Handle ranged attack coroutine
        /// </summary>
        private IEnumerator RangedAttackCoroutine(Vector3 targetPosition, float radius, float damage)
        {
            var transformComponent = Entity.GetComponent<TransformComponent>();
            if (transformComponent == null)
                yield break;
                
            // Create projectile visual
            GameObject projectile = CreateProjectileVisual(transformComponent.Position, targetPosition);
            
            // Wait for projectile to reach target
            float distance = Vector3.Distance(transformComponent.Position, targetPosition);
            float speed = 20f; // Projectile speed
            float travelTime = distance / speed;
            
            yield return new WaitForSeconds(travelTime);
            
            // Destroy projectile
            if (projectile != null)
            {
                Destroy(projectile);
            }
            
            // Create impact effect
            CreateImpactEffect(targetPosition);
            
            // Apply area damage
            Collider[] colliders = Physics.OverlapSphere(targetPosition, radius);
            foreach (var collider in colliders)
            {
                // Get entity component
                var entityComponent = collider.GetComponent<BaseEntity>();
                if (entityComponent != null && entityComponent != Entity)
                {
                    // Check if entity is an enemy
                    var targetHealth = entityComponent.GetComponent<HealthComponent>();
                    if (targetHealth != null)
                    {
                        targetHealth.TakeDamage(damage, Entity);
                    }
                }
            }
        }
        
        /// <summary>
        /// Execute Shield ability
        /// </summary>
        private bool ExecuteShield()
        {
            var healthComponent = Entity.GetComponent<HealthComponent>();
            if (healthComponent == null)
                return false;
                
            // Calculate shield amount
            float shieldAmount = GetAbilityParamFloat("shieldAmount", 50f);
            
            // Apply temporary shield
            // healthComponent.AddTemporaryShield(shieldAmount, _abilityDuration);
            
            // Create shield visual
            CreateAbilityVisuals("Shield");
            
            return true;
        }
        
        /// <summary>
        /// Complete Shield ability
        /// </summary>
        private void CompleteShield()
        {
            var healthComponent = Entity.GetComponent<HealthComponent>();
            if (healthComponent != null)
            {
                // Remove temporary shield
                // healthComponent.RemoveTemporaryShield();
            }
        }
        
        /// <summary>
        /// Execute Stealth ability
        /// </summary>
        private bool ExecuteStealth()
        {
            // Apply stealth logic
            var aggroComponent = Entity.GetComponent<AggroDetectionComponent>();
            if (aggroComponent != null)
            {
                // aggroComponent.SetStealthed(true);
            }
            
            // Adjust visual appearance
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                // Store original materials
                StartCoroutine(FadeRenderer(renderer, 0.3f, _abilityDuration));
            }
            
            return true;
        }
        
        /// <summary>
        /// Complete Stealth ability
        /// </summary>
        private void CompleteStealth()
        {
            // Remove stealth
            var aggroComponent = Entity.GetComponent<AggroDetectionComponent>();
            if (aggroComponent != null)
            {
                // aggroComponent.SetStealthed(false);
            }
            
            // Restore normal appearance
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                StartCoroutine(FadeRenderer(renderer, 1.0f, 0.5f));
            }
        }
        
        /// <summary>
        /// Execute Taunt ability
        /// </summary>
        private bool ExecuteTaunt(IEntity targetEntity)
        {
            if (targetEntity == null)
                return false;
                
            // Get aggro component from target
            var targetAggroComponent = targetEntity.GetComponent<AggroDetectionComponent>();
            if (targetAggroComponent == null)
                return false;
                
            // Force target to aggro on this entity
            // targetAggroComponent.ForceAggro(Entity, _abilityDuration);
            
            // Create taunt visual
            CreateAbilityVisuals("Taunt");
            
            return true;
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Get a float parameter from the ability parameters string
        /// </summary>
        private float GetAbilityParamFloat(string paramName, float defaultValue)
        {
            if (string.IsNullOrEmpty(_abilityParameters))
                return defaultValue;
                
            string[] paramPairs = _abilityParameters.Split(',');
            foreach (string param in paramPairs)
            {
                string[] keyValue = param.Trim().Split('=');
                if (keyValue.Length == 2)
                {
                    if (keyValue[0].Trim().ToLower() == paramName.ToLower())
                    {
                        if (float.TryParse(keyValue[1].Trim(), out float value))
                        {
                            return value;
                        }
                    }
                }
            }
            
            return defaultValue;
        }
        
        /// <summary>
        /// Create visual effects for an ability
        /// </summary>
        private void CreateAbilityVisuals(string abilityType)
        {
            // Cleanup any existing effects
            if (_abilityEffectObject != null)
            {
                Destroy(_abilityEffectObject);
            }
            
            // Create a new effect object
            _abilityEffectObject = new GameObject($"Effect_{abilityType}");
            _abilityEffectObject.transform.SetParent(transform);
            _abilityEffectObject.transform.localPosition = Vector3.zero;
            
            // Add particle system
            _abilityParticles = _abilityEffectObject.AddComponent<ParticleSystem>();
            
            // Configure particle system based on ability type
            var main = _abilityParticles.main;
            var emission = _abilityParticles.emission;
            var shape = _abilityParticles.shape;
            
            switch (abilityType.ToLower())
            {
                case "charge":
                    main.startColor = new ParticleSystem.MinMaxGradient(Color.red);
                    main.startSpeed = 2f;
                    main.startSize = 0.3f;
                    main.duration = _abilityDuration;
                    emission.rateOverTime = 10;
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    shape.radius = 1f;
                    break;
                    
                case "heal":
                    main.startColor = new ParticleSystem.MinMaxGradient(Color.green);
                    main.startSpeed = 1f;
                    main.startSize = 0.2f;
                    main.duration = 2f;
                    emission.rateOverTime = 20;
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    shape.radius = 1f;
                    break;
                    
                case "shield":
                    main.startColor = new ParticleSystem.MinMaxGradient(Color.blue);
                    main.startSpeed = 0f;
                    main.startSize = 0.5f;
                    main.duration = _abilityDuration;
                    emission.rateOverTime = 5;
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    shape.radius = 1.2f;
                    break;
                    
                case "taunt":
                    main.startColor = new ParticleSystem.MinMaxGradient(Color.yellow);
                    main.startSpeed = 1f;
                    main.startSize = 0.3f;
                    main.duration = 1f;
                    emission.rateOverTime = 15;
                    shape.shapeType = ParticleSystemShapeType.Cone;
                    shape.angle = 30f;
                    break;
            }
            
            // Play the particles
            _abilityParticles.Play();
        }
        
        /// <summary>
        /// Create a projectile visual effect
        /// </summary>
        private GameObject CreateProjectileVisual(Vector3 startPosition, Vector3 targetPosition)
        {
            GameObject projectile = new GameObject("Projectile");
            projectile.transform.position = startPosition;
            
            // Add a visual component (sphere)
            GameObject visualObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visualObject.transform.SetParent(projectile.transform);
            visualObject.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            visualObject.transform.localPosition = Vector3.zero;
            
            // Add a trail renderer
            TrailRenderer trail = projectile.AddComponent<TrailRenderer>();
            trail.startWidth = 0.2f;
            trail.endWidth = 0.05f;
            trail.time = 0.5f;
            
            // Setup material
            trail.material = new Material(Shader.Find("Particles/Standard Unlit"));
            trail.material.color = Color.red;
            
            // Add a script to move the projectile
            StartCoroutine(MoveProjectile(projectile, startPosition, targetPosition, 20f));
            
            return projectile;
        }
        
        /// <summary>
        /// Move projectile along a path
        /// </summary>
        private IEnumerator MoveProjectile(GameObject projectile, Vector3 startPos, Vector3 endPos, float speed)
        {
            float journeyLength = Vector3.Distance(startPos, endPos);
            float startTime = Time.time;
            
            while (projectile != null)
            {
                float distanceCovered = (Time.time - startTime) * speed;
                float journeyFraction = distanceCovered / journeyLength;
                
                if (journeyFraction >= 1.0f)
                    break;
                    
                projectile.transform.position = Vector3.Lerp(startPos, endPos, journeyFraction);
                
                yield return null;
            }
        }
        
        /// <summary>
        /// Create impact effect at target position
        /// </summary>
        private void CreateImpactEffect(Vector3 position)
        {
            GameObject impactEffect = new GameObject("ImpactEffect");
            impactEffect.transform.position = position;
            
            // Add a particle system
            ParticleSystem particles = impactEffect.AddComponent<ParticleSystem>();
            
            // Configure the particle system
            var main = particles.main;
            main.startColor = new ParticleSystem.MinMaxGradient(Color.red, new Color(1, 0.5f, 0, 1));
            main.startSpeed = 5f;
            main.startSize = 0.5f;
            main.duration = 1f;
            
            var emission = particles.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 50) });
            
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;
            
            // Play the particles
            particles.Play();
            
            // Destroy after duration
            Destroy(impactEffect, 2f);
        }
        
        /// <summary>
        /// Fade a renderer's opacity
        /// </summary>
        private IEnumerator FadeRenderer(Renderer renderer, float targetAlpha, float duration)
        {
            if (renderer == null)
                yield break;
                
            Material material = renderer.material;
            Color originalColor = material.color;
            Color targetColor = new Color(originalColor.r, originalColor.g, originalColor.b, targetAlpha);
            
            float startTime = Time.time;
            float currentTime = 0;
            
            while (currentTime < duration)
            {
                currentTime = Time.time - startTime;
                float t = currentTime / duration;
                
                material.color = Color.Lerp(originalColor, targetColor, t);
                
                yield return null;
            }
            
            material.color = targetColor;
        }
        
        #endregion
        
        /// <summary>
        /// Clean up resources
        /// </summary>
        public override void Cleanup()
        {
            // Clean up ability effect
            if (_abilityEffectObject != null)
            {
                Destroy(_abilityEffectObject);
                _abilityEffectObject = null;
            }
            
            // Reset any ability-specific states
            if (_isAbilityActive)
            {
                Vector3 position = transform.position;
                CompleteAbility(position, null);
            }
            
            base.Cleanup();
        }
    }
}