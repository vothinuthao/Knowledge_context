using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Core.ECS;

namespace VikingRaven.Units.Components
{
    public class AnimationComponent : BaseComponent
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private string _currentAnimation;
        
        private Dictionary<string, int> _animationHashes = new Dictionary<string, int>();
        
        public string CurrentAnimation => _currentAnimation;

        public override void Initialize()
        {
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }
            
            // Pre-compute animation hashes
            InitializeAnimationHashes();
        }

        private void InitializeAnimationHashes()
        {
            // Common animation names
            string[] commonAnimations = new string[] 
            {
                "Idle", "Walk", "Run", "Attack", "Aggro", "Stun", "Knockback", "Death"
            };
            
            foreach (var animName in commonAnimations)
            {
                _animationHashes[animName] = Animator.StringToHash(animName);
            }
        }

        public void PlayAnimation(string animationName)
        {
            if (!_animator || string.IsNullOrEmpty(animationName) || IsActive)
                return;
                
            // Check if the animation exists in our hash table
            if (!_animationHashes.TryGetValue(animationName, out int hash))
            {
                // If not, compute it and store for future use
                hash = Animator.StringToHash(animationName);
                _animationHashes[animationName] = hash;
            }
            
            // Play the animation
            _animator.SetTrigger(hash);
            _currentAnimation = animationName;
        }

        public void SetFloat(string paramName, float value)
        {
            if (_animator == null)
                return;
                
            _animator.SetFloat(paramName, value);
        }

        public void SetBool(string paramName, bool value)
        {
            if (_animator == null)
                return;
                
            _animator.SetBool(paramName, value);
        }
    }
}