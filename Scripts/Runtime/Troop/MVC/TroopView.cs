using UnityEngine;

namespace Troop
{
    public class TroopView : MonoBehaviour
    {
        // [SerializeField]
        // private Animator _animator;
        [SerializeField]
        private SpriteRenderer _spriteRenderer;
        [SerializeField]
        private int isMovingHash;
        private int isAttackingHash;
        private int speedMultiplierHash;
    
        private void Awake()
        {
            // if(!_animator)
            //     _animator = GetComponent<Animator>();
            // if(!_spriteRenderer)
            //     _spriteRenderer = GetComponent<SpriteRenderer>();
            // isMovingHash = Animator.StringToHash("IsMoving");
            // isAttackingHash = Animator.StringToHash("IsAttacking");
            // speedMultiplierHash = Animator.StringToHash("SpeedMultiplier");
            if (transform.localScale.magnitude < 0.1f)
            {
                transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            }
        }
    
        public void UpdateTransform(Vector3 position, Quaternion rotation)
        {
            if (transform.position != position)
            {
                transform.position = position;
            }
    
            if (transform.rotation != rotation)
            {
                transform.rotation = rotation;
            }
    
            // Debug log
            Debug.Log($"TroopView {name} updated position: {transform.position}");
        }
    
        public void UpdateAnimation(TroopState state, float speedMultiplier = 1f)
        {
            // switch (state)
            // {
            //     case TroopState.Moving:
            //         _animator.SetBool(isMovingHash, true);
            //         _animator.SetBool(isAttackingHash, false);
            //         break;
            //     case TroopState.Attacking:
            //         _animator.SetBool(isAttackingHash, true);
            //         break;
            //     case TroopState.Idle:
            //     case TroopState.Defending:
            //         _animator.SetBool(isMovingHash, false);
            //         _animator.SetBool(isAttackingHash, false);
            //         break;
            //     case TroopState.Fleeing:
            //         _animator.SetBool(isMovingHash, true);
            //         _animator.SetBool(isAttackingHash, false);
            //         break;
            //     case TroopState.Dead:
            //         _animator.SetTrigger("Die");
            //         break;
            // }
            //
            // _animator.SetFloat(speedMultiplierHash, speedMultiplier);
        }
    
        public void TriggerAnimation(string triggerName)
        {
            // _animator.SetTrigger(triggerName);
        }
    
        public void SetAnimatorController(RuntimeAnimatorController controller)
        {
            if (controller != null)
            {
                // _animator.runtimeAnimatorController = controller;
            }
        }
    
        public void SetVisualProperties(Color color)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = color;
            }
        }
    }
}