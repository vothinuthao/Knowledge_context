using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using VikingRaven.Core.ECS;
using System;

namespace VikingRaven.Units.Components
{
    /// <summary>
    /// Enhanced Animation Component with proper loop animation handling and comprehensive debugging
    /// </summary>
    public class AnimationComponent : BaseComponent
    {
        #region Animation State Definition
        
        [System.Serializable]
        public enum AnimationState
        {
            None,
            Idle,
            Moving,
            Attack,
            Death,
            Aggro,
            Stun,
            Knockback,
            SpecialAttack,
            Block,
            Hurt
        }
        
        [System.Serializable]
        public class AnimationData
        {
            [Tooltip("Animation state identifier")]
            public AnimationState state;
            
            [Tooltip("Animation clip name in Animator Controller")]
            public string clipName;
            
            [Tooltip("Should this animation loop?")]
            public bool isLooping;
            
            [Tooltip("Animation transition duration")]
            [Range(0f, 1f)]
            public float transitionDuration = 0.25f;
            
            [Tooltip("Animation priority (higher = more important)")]
            [Range(0, 10)]
            public int priority = 1;
            
            [Tooltip("Use Speed parameter instead of CrossFade for this animation")]
            public bool useSpeedParameter = false;
            
            [ShowIf("useSpeedParameter")]
            [Tooltip("Speed value to set when playing this animation")]
            [Range(0f, 3f)]
            public float speedValue = 1f;
            
            public AnimationData(AnimationState state, string clipName, bool isLooping = true, float transitionDuration = 0.25f, int priority = 1)
            {
                this.state = state;
                this.clipName = clipName;
                this.isLooping = isLooping;
                this.transitionDuration = transitionDuration;
                this.priority = priority;
                this.useSpeedParameter = false;
                this.speedValue = 1f;
            }
        }
        
        #endregion

        #region Inspector Fields
        
        [Title("Animation Setup")]
        [Required, Tooltip("Animator component reference")]
        [SerializeField] private Animator _animator;
        
        [Title("Animation Configuration")]
        [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true)]
        [Tooltip("List of available animations for this unit")]
        [SerializeField] private List<AnimationData> _animationDataList = new List<AnimationData>();
        
        [Title("Real-time Monitoring")]
        [ReadOnly, ShowInInspector] 
        private AnimationState _currentState = AnimationState.None;
        
        [ReadOnly, ShowInInspector]
        private string _currentClipName;
        
        [ReadOnly, ShowInInspector]
        private float _currentAnimationTime;
        
        [ReadOnly, ShowInInspector]
        [Tooltip("Current animator state name")]
        private string _currentAnimatorStateName = "Unknown";
        
        [ReadOnly, ShowInInspector]
        [Tooltip("Current Speed parameter value")]
        private float _currentSpeedValue = 0f;
        
        [Title("Manual Testing Controls")]
        [InfoBox("Use these controls to manually test animations and debug issues", InfoMessageType.Info)]
        
        [Range(0f, 3f)]
        [OnValueChanged(nameof(OnManualSpeedChanged))]
        [Tooltip("Manually control Speed parameter for testing")]
        public float manualSpeedControl = 0f;
        
        [Button("Force Set Moving Speed"), ButtonGroup("Manual Control")]
        private void ForceSetMovingSpeed()
        {
            if (IsAnimatorValid)
            {
                _animator.SetFloat(_speedParameterName, 1f);
                manualSpeedControl = 1f;
                Debug.Log("Forced Speed parameter to 1.0 for Moving");
            }
        }
        
        [Button("Force Set Idle Speed"), ButtonGroup("Manual Control")]
        private void ForceSetIdleSpeed()
        {
            if (IsAnimatorValid)
            {
                _animator.SetFloat(_speedParameterName, 0f);
                manualSpeedControl = 0f;
                Debug.Log("Forced Speed parameter to 0.0 for Idle");
            }
        }
        
        [Title("Animation Parameters")]
        [Tooltip("Movement speed parameter name in Animator")]
        [SerializeField] private string _speedParameterName = "Speed";
        
        [Tooltip("Attack trigger parameter name in Animator")]
        [SerializeField] private string _attackTriggerName = "Attack";
        
        [Tooltip("Death trigger parameter name in Animator")]
        [SerializeField] private string _deathTriggerName = "Death";
        
        [Title("Performance Settings")]
        [Tooltip("Enable animation caching for better performance")]
        [SerializeField] private bool _enableCaching = true;
        
        [ShowIf("_enableCaching")]
        [Tooltip("Maximum number of cached animation hashes")]
        [SerializeField] private int _maxCacheSize = 20;
        
        #endregion

        #region Private Fields
        
        private Dictionary<AnimationState, AnimationData> _animationMap = new Dictionary<AnimationState, AnimationData>();
        private Dictionary<string, int> _animationHashes = new Dictionary<string, int>();
        private AnimationState _requestedState = AnimationState.None;
        private bool _isTransitioning = false;
        private float _transitionStartTime;
        private int _currentPriority = 0;
        
        // State monitoring
        private int _lastStateHash = 0;
        private bool _speedParameterExists = false;
        
        #endregion

        #region Events
        
        public event System.Action<AnimationState> OnAnimationStateChanged;
        public event System.Action<string> OnAnimationCompleted;
        public event System.Action OnAttackAnimationEvent;
        public event System.Action OnDeathAnimationComplete;
        
        #endregion

        #region Properties
        
        public AnimationState CurrentState => _currentState;
        public string CurrentClipName => _currentClipName;
        public bool IsTransitioning => _isTransitioning;
        public float CurrentAnimationTime => _currentAnimationTime;
        public bool IsAnimatorValid => _animator != null && _animator.isActiveAndEnabled && _animator.runtimeAnimatorController != null;
        
        #endregion

        #region Initialization
        
        public override void Initialize()
        {
            ValidateAnimator();
            InitializeAnimationSystem();
            SetupDefaultAnimations();
            ValidateParameters();
            
            Debug.Log($"[AnimationComponent] Initialized for entity {Entity?.Id} - Ready for testing");
        }
        
        private void ValidateAnimator()
        {
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
                if (_animator == null)
                {
                    Debug.LogError($"[AnimationComponent] No Animator found on {gameObject.name}!");
                    return;
                }
            }
            
            if (_animator.runtimeAnimatorController == null)
            {
                Debug.LogError($"[AnimationComponent] No Animator Controller assigned to {gameObject.name}!");
                return;
            }
            
            Debug.Log($"[AnimationComponent] Animator validation passed for {gameObject.name}");
        }
        
        private void InitializeAnimationSystem()
        {
            _animationMap.Clear();
            _animationHashes.Clear();
            
            foreach (var animData in _animationDataList)
            {
                if (!_animationMap.ContainsKey(animData.state))
                {
                    _animationMap[animData.state] = animData;
                    
                    if (_enableCaching && !string.IsNullOrEmpty(animData.clipName))
                    {
                        CacheAnimationHash(animData.clipName);
                    }
                }
                else
                {
                    Debug.LogWarning($"[AnimationComponent] Duplicate animation state {animData.state} found in {gameObject.name}");
                }
            }
        }
        
        private void SetupDefaultAnimations()
        {
            if (_animationDataList.Count == 0)
            {
                Debug.Log($"[AnimationComponent] Setting up default animations for {gameObject.name}");
                
                _animationDataList.Add(new AnimationData(AnimationState.Idle, "Idle", true, 0.25f, 1));
                _animationDataList.Add(new AnimationData(AnimationState.Moving, "Moving", true, 0.25f, 2));
                _animationDataList.Add(new AnimationData(AnimationState.Attack, "Attack", false, 0.1f, 5));
                _animationDataList.Add(new AnimationData(AnimationState.Death, "Death", false, 0.1f, 10));
                
                InitializeAnimationSystem();
            }
        }
        
        private void ValidateParameters()
        {
            if (!IsAnimatorValid) return;
            
            _speedParameterExists = HasParameter(_speedParameterName, AnimatorControllerParameterType.Float);
            
            Debug.Log($"[AnimationComponent] Parameter validation:");
            Debug.Log($"  Speed ({_speedParameterName}): {(_speedParameterExists ? "✓ Found" : "✗ Missing")}");
            Debug.Log($"  Attack ({_attackTriggerName}): {(HasParameter(_attackTriggerName, AnimatorControllerParameterType.Trigger) ? "✓ Found" : "✗ Missing")}");
            Debug.Log($"  Death ({_deathTriggerName}): {(HasParameter(_deathTriggerName, AnimatorControllerParameterType.Trigger) ? "✓ Found" : "✗ Missing")}");
        }
        
        #endregion

        #region Manual Control
        
        private void OnManualSpeedChanged()
        {
            if (IsAnimatorValid && _speedParameterExists)
            {
                _animator.SetFloat(_speedParameterName, manualSpeedControl);
                Debug.Log($"[Manual Control] Speed parameter set to: {manualSpeedControl:F2}");
            }
        }
        
        #endregion

        #region Animation Control
        
        public bool PlayAnimation(AnimationState state, bool forcePlay = false)
        {
            if (!IsAnimatorValid)
            {
                Debug.LogWarning($"[AnimationComponent] Cannot play animation - Animator is not valid on {gameObject.name}");
                return false;
            }
            
            if (!_animationMap.TryGetValue(state, out AnimationData animData))
            {
                Debug.LogWarning($"[AnimationComponent] Animation state {state} not found in animation map for {gameObject.name}");
                return false;
            }
            
            if (!forcePlay && animData.priority < _currentPriority && !CanInterruptCurrentAnimation())
            {
                Debug.Log($"[AnimationComponent] Animation {state} blocked by higher priority animation on {gameObject.name}");
                return false;
            }
            
            return PlayAnimationInternal(animData);
        }
        
        public bool PlayAnimation(string clipName, bool forcePlay = false)
        {
            foreach (var kvp in _animationMap)
            {
                if (kvp.Value.clipName.Equals(clipName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return PlayAnimation(kvp.Key, forcePlay);
                }
            }
            
            Debug.LogWarning($"[AnimationComponent] Animation clip '{clipName}' not found in animation map for {gameObject.name}");
            return false;
        }
        
        private bool PlayAnimationInternal(AnimationData animData)
        {
            try
            {
                bool success = false;
                
                // Choose animation method based on configuration
                if (animData.useSpeedParameter && _speedParameterExists)
                {
                    success = PlayAnimationWithSpeedParameter(animData);
                }
                else
                {
                    success = PlayAnimationWithCrossFadeOrTrigger(animData);
                }
                
                if (success)
                {
                    UpdateAnimationState(animData);
                    Debug.Log($"[AnimationComponent] ✓ Playing animation: {animData.state} ({animData.clipName}) on {gameObject.name}");
                }
                else
                {
                    Debug.LogError($"[AnimationComponent] ✗ Failed to play animation {animData.state} on {gameObject.name}");
                    
                    // Try fallback method
                    if (!animData.useSpeedParameter && _speedParameterExists)
                    {
                        Debug.Log($"[AnimationComponent] Trying fallback Speed parameter method for {animData.state}");
                        success = TrySpeedParameterFallback(animData);
                    }
                }
                
                return success;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AnimationComponent] Exception playing animation {animData.state}: {e.Message}");
                return false;
            }
        }
        
        private bool PlayAnimationWithSpeedParameter(AnimationData animData)
        {
            try
            {
                _animator.SetFloat(_speedParameterName, animData.speedValue);
                manualSpeedControl = animData.speedValue; // Sync manual control
                
                Debug.Log($"[AnimationComponent] Set Speed parameter to {animData.speedValue:F2} for {animData.state}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AnimationComponent] Error setting Speed parameter: {e.Message}");
                return false;
            }
        }
        
        private bool PlayAnimationWithCrossFadeOrTrigger(AnimationData animData)
        {
            try
            {
                if (animData.state == AnimationState.Attack)
                {
                    if (HasParameter(_attackTriggerName, AnimatorControllerParameterType.Trigger))
                    {
                        _animator.SetTrigger(_attackTriggerName);
                        Debug.Log($"[AnimationComponent] Triggered {_attackTriggerName} for Attack");
                        return true;
                    }
                }
                else if (animData.state == AnimationState.Death)
                {
                    if (HasParameter(_deathTriggerName, AnimatorControllerParameterType.Trigger))
                    {
                        _animator.SetTrigger(_deathTriggerName);
                        Debug.Log($"[AnimationComponent] Triggered {_deathTriggerName} for Death");
                        return true;
                    }
                }
                
                // Use CrossFade for other animations
                int animHash = GetAnimationHash(animData.clipName);
                _animator.CrossFade(animHash, animData.transitionDuration);
                Debug.Log($"[AnimationComponent] CrossFade to {animData.clipName} with duration {animData.transitionDuration:F2}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AnimationComponent] Error with CrossFade/Trigger: {e.Message}");
                return false;
            }
        }
        
        private bool TrySpeedParameterFallback(AnimationData animData)
        {
            try
            {
                float speedValue = animData.state switch
                {
                    AnimationState.Idle => 0f,
                    AnimationState.Moving => 1f,
                    _ => 1f
                };
                
                _animator.SetFloat(_speedParameterName, speedValue);
                manualSpeedControl = speedValue;
                
                Debug.Log($"[AnimationComponent] Fallback: Set Speed to {speedValue:F2} for {animData.state}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AnimationComponent] Fallback method failed: {e.Message}");
                return false;
            }
        }
        
        private void UpdateAnimationState(AnimationData animData)
        {
            var previousState = _currentState;
            _currentState = animData.state;
            _currentClipName = animData.clipName;
            _currentPriority = animData.priority;
            _isTransitioning = true;
            _transitionStartTime = Time.time;
            
            if (previousState != _currentState)
            {
                OnAnimationStateChanged?.Invoke(_currentState);
            }
        }
        
        #endregion

        #region Animation Parameters
        
        public void SetMovementSpeed(float speed)
        {
            if (IsAnimatorValid && _speedParameterExists)
            {
                _animator.SetFloat(_speedParameterName, speed);
                manualSpeedControl = speed; // Sync manual control
            }
        }
        
        public void SetFloat(string paramName, float value)
        {
            if (IsAnimatorValid && !string.IsNullOrEmpty(paramName))
            {
                _animator.SetFloat(paramName, value);
            }
        }
        
        public void SetBool(string paramName, bool value)
        {
            if (IsAnimatorValid && !string.IsNullOrEmpty(paramName))
            {
                _animator.SetBool(paramName, value);
            }
        }
        
        public void SetTrigger(string paramName)
        {
            if (IsAnimatorValid && !string.IsNullOrEmpty(paramName))
            {
                _animator.SetTrigger(paramName);
            }
        }
        
        #endregion

        #region Helper Methods
        
        private bool HasParameter(string paramName, AnimatorControllerParameterType paramType)
        {
            if (!IsAnimatorValid || string.IsNullOrEmpty(paramName))
                return false;
                
            foreach (var param in _animator.parameters)
            {
                if (param.name == paramName && param.type == paramType)
                    return true;
            }
            
            return false;
        }
        
        private int GetAnimationHash(string clipName)
        {
            if (!_animationHashes.TryGetValue(clipName, out int hash))
            {
                hash = Animator.StringToHash(clipName);
                CacheAnimationHash(clipName, hash);
            }
            return hash;
        }
        
        private void CacheAnimationHash(string clipName, int? hash = null)
        {
            if (!_enableCaching || string.IsNullOrEmpty(clipName))
                return;
                
            if (_animationHashes.Count >= _maxCacheSize)
            {
                var oldestKey = "";
                foreach (var key in _animationHashes.Keys)
                {
                    oldestKey = key;
                    break;
                }
                _animationHashes.Remove(oldestKey);
            }
            
            _animationHashes[clipName] = hash ?? Animator.StringToHash(clipName);
        }
        
        private bool CanInterruptCurrentAnimation()
        {
            if (_currentState == AnimationState.Death)
                return false;
                
            if (_isTransitioning && Time.time - _transitionStartTime < 0.1f)
                return false;
                
            return true;
        }
        
        #endregion

        #region Update & Events
        
        private void Update()
        {
            if (!IsAnimatorValid)
                return;
                
            UpdateAnimationState();
            UpdateTransitionState();
            UpdateMonitoring();
        }
        
        private void UpdateAnimationState()
        {
            if (_animator.layerCount > 0)
            {
                var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
                _currentAnimationTime = stateInfo.normalizedTime;
                
                // Update animator state name for debugging
                if (stateInfo.shortNameHash != _lastStateHash)
                {
                    _lastStateHash = stateInfo.shortNameHash;
                    _currentAnimatorStateName = $"Hash_{stateInfo.shortNameHash}";
                    Debug.Log($"[State Monitor] Animator state changed to: {_currentAnimatorStateName}");
                }
                
                // Check if non-looping animation completed
                if (_currentAnimationTime >= 1.0f && _animationMap.TryGetValue(_currentState, out AnimationData animData))
                {
                    if (!animData.isLooping && !animData.useSpeedParameter)
                    {
                        OnAnimationCompleted?.Invoke(_currentClipName);
                        
                        if (_currentState == AnimationState.Death)
                        {
                            OnDeathAnimationComplete?.Invoke();
                        }
                        else if (_currentState != AnimationState.Idle)
                        {
                            PlayAnimation(AnimationState.Idle);
                        }
                    }
                }
            }
        }
        
        private void UpdateTransitionState()
        {
            if (_isTransitioning && Time.time - _transitionStartTime > 0.5f)
            {
                _isTransitioning = false;
            }
        }
        
        private void UpdateMonitoring()
        {
            if (_speedParameterExists)
            {
                _currentSpeedValue = _animator.GetFloat(_speedParameterName);
            }
        }
        
        public void OnAttackEvent()
        {
            OnAttackAnimationEvent?.Invoke();
        }
        
        #endregion

        #region Debug Methods
        
        [Title("Debug Tools")]
        [InfoBox("Use these tools to diagnose and fix animation issues", InfoMessageType.Info)]
        
        [Button("Initialize Animation System"), ButtonGroup("Setup")]
        private void DebugInitializeAnimationSystem()
        {
            Debug.Log("=== INITIALIZING ANIMATION SYSTEM ===");
            Initialize();
            
            Debug.Log($"✓ Animator Valid: {IsAnimatorValid}");
            Debug.Log($"✓ Animation Map Count: {_animationMap.Count}");
            Debug.Log($"✓ Speed Parameter Exists: {_speedParameterExists}");
            
            foreach (var kvp in _animationMap)
            {
                var method = kvp.Value.useSpeedParameter ? "Speed Parameter" : "CrossFade/Trigger";
                Debug.Log($"  - {kvp.Key}: '{kvp.Value.clipName}' ({method}, Loop: {kvp.Value.isLooping})");
            }
        }
        
        [Button("Diagnose Moving Animation"), ButtonGroup("Diagnosis")]
        private void DiagnoseMovingAnimation()
        {
            Debug.Log("=== DIAGNOSING MOVING ANIMATION ===");
            
            if (!_animationMap.ContainsKey(AnimationState.Moving))
            {
                Debug.LogError("Moving animation not configured!");
                return;
            }
            
            var movingData = _animationMap[AnimationState.Moving];
            Debug.Log($"Moving Animation Configuration:");
            Debug.Log($"  Clip Name: '{movingData.clipName}'");
            Debug.Log($"  Is Looping: {movingData.isLooping}");
            Debug.Log($"  Use Speed Parameter: {movingData.useSpeedParameter}");
            Debug.Log($"  Speed Value: {movingData.speedValue:F2}");
            Debug.Log($"  Transition Duration: {movingData.transitionDuration:F2}");
            
            // Check current state
            Debug.Log($"Current State: {_currentState}");
            Debug.Log($"Current Animator State: {_currentAnimatorStateName}");
            Debug.Log($"Current Speed Value: {_currentSpeedValue:F2}");
            Debug.Log($"Animation Time: {_currentAnimationTime:F2}");
            
            // Recommendations
            if (!movingData.isLooping)
            {
                Debug.LogWarning("⚠️ Moving animation is set to NOT loop! This might cause the issue.");
                Debug.LogWarning("💡 Solution: Set isLooping = true for Moving animation");
            }
            
            if (!_speedParameterExists)
            {
                Debug.LogWarning("⚠️ Speed parameter not found! Consider using Speed parameter approach.");
                Debug.LogWarning("💡 Solution: Add Speed parameter to Animator Controller or enable useSpeedParameter");
            }
        }
        
        [Button("Test Moving Loop"), ButtonGroup("Testing")]
        private void TestMovingLoop()
        {
            Debug.Log("=== TESTING MOVING LOOP ===");
            
            if (!_animationMap.ContainsKey(AnimationState.Moving))
            {
                Debug.LogError("Moving animation not configured!");
                return;
            }
            
            // Test with Speed parameter first
            if (_speedParameterExists)
            {
                Debug.Log("Testing with Speed parameter approach...");
                _animator.SetFloat(_speedParameterName, 1f);
                manualSpeedControl = 1f;
                
                // Update moving animation to use Speed parameter
                var movingData = _animationMap[AnimationState.Moving];
                movingData.useSpeedParameter = true;
                movingData.speedValue = 1f;
                
                Debug.Log("✓ Set Speed parameter to 1.0 - Moving should loop continuously");
                Debug.Log("Use 'Force Set Idle Speed' button to stop or set Manual Speed Control to 0");
            }
            else
            {
                Debug.Log("Testing with CrossFade approach...");
                bool success = PlayAnimation(AnimationState.Moving, true);
                Debug.Log($"CrossFade result: {(success ? "SUCCESS" : "FAILED")}");
                
                if (!success)
                {
                    Debug.LogError("❌ CrossFade failed! State might not exist in Animator Controller");
                    Debug.LogError("💡 Solution: Check that state name matches in Animator Controller");
                }
            }
        }
        
        [Button("Test Idle Animation"), ButtonGroup("Test Animations")]
        private void TestIdleAnimation()
        {
            bool success = PlayAnimation(AnimationState.Idle, true);
            Debug.Log($"Test Idle Animation: {(success ? "SUCCESS" : "FAILED")}");
        }
        
        [Button("Test Moving Animation"), ButtonGroup("Test Animations")]
        private void TestMovingAnimation()
        {
            bool success = PlayAnimation(AnimationState.Moving, true);
            Debug.Log($"Test Moving Animation: {(success ? "SUCCESS" : "FAILED")}");
        }
        
        [Button("Test Attack Animation"), ButtonGroup("Test Animations")]
        private void TestAttackAnimation()
        {
            bool success = PlayAnimation(AnimationState.Attack, true);
            Debug.Log($"Test Attack Animation: {(success ? "SUCCESS" : "FAILED")}");
        }
        
        [Button("Test Death Animation"), ButtonGroup("Test Animations")]
        private void TestDeathAnimation()
        {
            bool success = PlayAnimation(AnimationState.Death, true);
            Debug.Log($"Test Death Animation: {(success ? "SUCCESS" : "FAILED")}");
        }
        
        [Button("Debug Animation Info"), ButtonGroup("Debug")]
        private void DebugAnimationInfo()
        {
            Debug.Log("=== ANIMATION COMPONENT DEBUG ===");
            Debug.Log($"Current State: {_currentState}");
            Debug.Log($"Current Clip: {_currentClipName}");
            Debug.Log($"Current Animator State: {_currentAnimatorStateName}");
            Debug.Log($"Animation Time: {_currentAnimationTime:F2}");
            Debug.Log($"Current Speed Value: {_currentSpeedValue:F2}");
            Debug.Log($"Manual Speed Control: {manualSpeedControl:F2}");
            Debug.Log($"Is Transitioning: {_isTransitioning}");
            Debug.Log($"Current Priority: {_currentPriority}");
            Debug.Log($"Cached Animations: {_animationHashes.Count}");
            Debug.Log($"Configured Animations: {_animationMap.Count}");
            
            foreach (var kvp in _animationMap)
            {
                var loopInfo = kvp.Value.isLooping ? "LOOP" : "ONCE";
                var methodInfo = kvp.Value.useSpeedParameter ? "SPEED" : "CROSSFADE";
                Debug.Log($"  - {kvp.Key}: '{kvp.Value.clipName}' ({methodInfo}, {loopInfo}, Priority: {kvp.Value.priority})");
            }
        }
        
        [Button("Clear Animation Cache"), ButtonGroup("Debug")]
        private void ClearAnimationCache()
        {
            _animationHashes.Clear();
            Debug.Log("Animation cache cleared");
        }
        
        #endregion
    }
}