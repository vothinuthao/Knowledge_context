using UnityEngine;
using RavenDeckbuilding.Core.Interfaces;
using System;

namespace RavenDeckbuilding.Systems.Events
{
    /// <summary>
    /// Static event dispatcher with pre-allocated callback arrays for zero-allocation performance
    /// Provides immediate visual and audio feedback for all game events
    /// </summary>
    public static class GameEventBus
    {
        private const int MAX_LISTENERS_PER_EVENT = 16;
        
        // Pre-allocated callback arrays for different event types
        private static Action<int, Vector3, bool>[] _cardPreviewListeners = new Action<int, Vector3, bool>[MAX_LISTENERS_PER_EVENT];
        private static Action<int, Vector3, Vector2>[] _cardDragListeners = new Action<int, Vector3, Vector2>[MAX_LISTENERS_PER_EVENT];
        private static Action<int, Vector3>[] _cardDropListeners = new Action<int, Vector3>[MAX_LISTENERS_PER_EVENT];
        private static Action<int>[] _cardHotkeyListeners = new Action<int>[MAX_LISTENERS_PER_EVENT];
        private static Action[] _cancelActionListeners = new Action[MAX_LISTENERS_PER_EVENT];
        
        // Command execution events
        private static Action<ICardCommand>[] _commandStartListeners = new Action<ICardCommand>[MAX_LISTENERS_PER_EVENT];
        private static Action<ICardCommand, CommandResult>[] _commandCompleteListeners = new Action<ICardCommand, CommandResult>[MAX_LISTENERS_PER_EVENT];
        private static Action<ICardCommand, CommandResult, string>[] _commandFailedListeners = new Action<ICardCommand, CommandResult, string>[MAX_LISTENERS_PER_EVENT];
        private static Action<ICardCommand>[] _commandCancelledListeners = new Action<ICardCommand>[MAX_LISTENERS_PER_EVENT];
        private static Action<ICardCommand>[] _commandEnqueuedListeners = new Action<ICardCommand>[MAX_LISTENERS_PER_EVENT];
        private static Action<ICardCommand>[] _commandRollbackListeners = new Action<ICardCommand>[MAX_LISTENERS_PER_EVENT];
        
        // Game state events
        private static Action<CardCastEvent>[] _cardCastListeners = new Action<CardCastEvent>[MAX_LISTENERS_PER_EVENT];
        private static Action<DamageEvent>[] _damageListeners = new Action<DamageEvent>[MAX_LISTENERS_PER_EVENT];
        private static Action<MovementEvent>[] _movementListeners = new Action<MovementEvent>[MAX_LISTENERS_PER_EVENT];
        
        // Listener counts for efficient iteration
        private static int _cardPreviewListenerCount = 0;
        private static int _cardDragListenerCount = 0;
        private static int _cardDropListenerCount = 0;
        private static int _cardHotkeyListenerCount = 0;
        private static int _cancelActionListenerCount = 0;
        private static int _commandStartListenerCount = 0;
        private static int _commandCompleteListenerCount = 0;
        private static int _commandFailedListenerCount = 0;
        private static int _commandCancelledListenerCount = 0;
        private static int _commandEnqueuedListenerCount = 0;
        private static int _commandRollbackListenerCount = 0;
        private static int _cardCastListenerCount = 0;
        private static int _damageListenerCount = 0;
        private static int _movementListenerCount = 0;

        // Input Event Subscriptions
        public static bool SubscribeToCardPreview(Action<int, Vector3, bool> callback)
        {
            return AddListener(callback, _cardPreviewListeners, ref _cardPreviewListenerCount);
        }

        public static bool UnsubscribeFromCardPreview(Action<int, Vector3, bool> callback)
        {
            return RemoveListener(callback, _cardPreviewListeners, ref _cardPreviewListenerCount);
        }

        public static bool SubscribeToCardDrag(Action<int, Vector3, Vector2> callback)
        {
            return AddListener(callback, _cardDragListeners, ref _cardDragListenerCount);
        }

        public static bool UnsubscribeFromCardDrag(Action<int, Vector3, Vector2> callback)
        {
            return RemoveListener(callback, _cardDragListeners, ref _cardDragListenerCount);
        }

        public static bool SubscribeToCardDrop(Action<int, Vector3> callback)
        {
            return AddListener(callback, _cardDropListeners, ref _cardDropListenerCount);
        }

        public static bool UnsubscribeFromCardDrop(Action<int, Vector3> callback)
        {
            return RemoveListener(callback, _cardDropListeners, ref _cardDropListenerCount);
        }

        public static bool SubscribeToCardHotkey(Action<int> callback)
        {
            return AddListener(callback, _cardHotkeyListeners, ref _cardHotkeyListenerCount);
        }

        public static bool UnsubscribeFromCardHotkey(Action<int> callback)
        {
            return RemoveListener(callback, _cardHotkeyListeners, ref _cardHotkeyListenerCount);
        }

        public static bool SubscribeToCancelAction(Action callback)
        {
            return AddListener(callback, _cancelActionListeners, ref _cancelActionListenerCount);
        }

        public static bool UnsubscribeFromCancelAction(Action callback)
        {
            return RemoveListener(callback, _cancelActionListeners, ref _cancelActionListenerCount);
        }

        // Command Event Subscriptions
        public static bool SubscribeToCommandStart(Action<ICardCommand> callback)
        {
            return AddListener(callback, _commandStartListeners, ref _commandStartListenerCount);
        }

        public static bool SubscribeToCommandComplete(Action<ICardCommand, CommandResult> callback)
        {
            return AddListener(callback, _commandCompleteListeners, ref _commandCompleteListenerCount);
        }

        public static bool SubscribeToCommandFailed(Action<ICardCommand, CommandResult, string> callback)
        {
            return AddListener(callback, _commandFailedListeners, ref _commandFailedListenerCount);
        }

        // Game Event Subscriptions
        public static bool SubscribeToCardCast(Action<CardCastEvent> callback)
        {
            return AddListener(callback, _cardCastListeners, ref _cardCastListenerCount);
        }

        public static bool SubscribeToDamage(Action<DamageEvent> callback)
        {
            return AddListener(callback, _damageListeners, ref _damageListenerCount);
        }

        // Event Firing Methods - Input Events
        public static void FireCardPreviewEvent(int cardIndex, Vector3 position, bool isShowing)
        {
            FireEvent(_cardPreviewListeners, _cardPreviewListenerCount, cardIndex, position, isShowing);
        }

        public static void FireCardDragPrediction(int cardIndex, Vector3 position, Vector2 direction)
        {
            FireEvent(_cardDragListeners, _cardDragListenerCount, cardIndex, position, direction);
        }

        public static void FireCardDropPrediction(int cardIndex, Vector3 position)
        {
            FireEvent(_cardDropListeners, _cardDropListenerCount, cardIndex, position);
        }

        public static void FireCardHotkeyPressed(int cardIndex)
        {
            FireEvent(_cardHotkeyListeners, _cardHotkeyListenerCount, cardIndex);
        }

        public static void FireCancelAction()
        {
            FireEvent(_cancelActionListeners, _cancelActionListenerCount);
        }

        // Event Firing Methods - Command Events
        public static void FireCommandExecutionStarted(ICardCommand command)
        {
            FireEvent(_commandStartListeners, _commandStartListenerCount, command);
        }

        public static void FireCommandExecutionCompleted(ICardCommand command, CommandResult result)
        {
            FireEvent(_commandCompleteListeners, _commandCompleteListenerCount, command, result);
        }

        public static void FireCommandExecutionFailed(ICardCommand command, CommandResult result, string reason)
        {
            FireEvent(_commandFailedListeners, _commandFailedListenerCount, command, result, reason);
        }

        public static void FireCommandCancelled(ICardCommand command)
        {
            FireEvent(_commandCancelledListeners, _commandCancelledListenerCount, command);
        }

        public static void FireCommandEnqueued(ICardCommand command)
        {
            FireEvent(_commandEnqueuedListeners, _commandEnqueuedListenerCount, command);
        }

        public static void FireCommandRolledBack(ICardCommand command)
        {
            FireEvent(_commandRollbackListeners, _commandRollbackListenerCount, command);
        }

        // Event Firing Methods - Game Events
        public static void FireCardCastEvent(CardCastEvent cardCast)
        {
            FireEvent(_cardCastListeners, _cardCastListenerCount, cardCast);
        }

        public static void FireDamageEvent(DamageEvent damage)
        {
            FireEvent(_damageListeners, _damageListenerCount, damage);
        }

        public static void FireMovementEvent(MovementEvent movement)
        {
            FireEvent(_movementListeners, _movementListenerCount, movement);
        }

        // Generic helper methods for listener management
        private static bool AddListener<T>(T callback, T[] listenerArray, ref int listenerCount)
        {
            if (callback == null || listenerCount >= MAX_LISTENERS_PER_EVENT) return false;
            
            // Check for duplicates
            for (int i = 0; i < listenerCount; i++)
            {
                if (listenerArray[i]?.Equals(callback) == true)
                    return false;
            }
            
            listenerArray[listenerCount] = callback;
            listenerCount++;
            return true;
        }

        private static bool RemoveListener<T>(T callback, T[] listenerArray, ref int listenerCount)
        {
            if (callback == null || listenerCount == 0) return false;
            
            for (int i = 0; i < listenerCount; i++)
            {
                if (listenerArray[i]?.Equals(callback) == true)
                {
                    // Move last element to this position
                    listenerArray[i] = listenerArray[listenerCount - 1];
                    listenerArray[listenerCount - 1] = default(T);
                    listenerCount--;
                    return true;
                }
            }
            return false;
        }

        // Generic helper methods for event firing
        private static void FireEvent<T>(Action<T>[] listeners, int count, T arg)
        {
            for (int i = 0; i < count; i++)
            {
                try
                {
                    listeners[i]?.Invoke(arg);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error firing event: {ex.Message}");
                }
            }
        }

        private static void FireEvent<T1, T2>(Action<T1, T2>[] listeners, int count, T1 arg1, T2 arg2)
        {
            for (int i = 0; i < count; i++)
            {
                try
                {
                    listeners[i]?.Invoke(arg1, arg2);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error firing event: {ex.Message}");
                }
            }
        }

        private static void FireEvent<T1, T2, T3>(Action<T1, T2, T3>[] listeners, int count, T1 arg1, T2 arg2, T3 arg3)
        {
            for (int i = 0; i < count; i++)
            {
                try
                {
                    listeners[i]?.Invoke(arg1, arg2, arg3);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error firing event: {ex.Message}");
                }
            }
        }

        private static void FireEvent(Action[] listeners, int count)
        {
            for (int i = 0; i < count; i++)
            {
                try
                {
                    listeners[i]?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error firing event: {ex.Message}");
                }
            }
        }

        // Cleanup method
        public static void ClearAllListeners()
        {
            // Reset all listener counts
            _cardPreviewListenerCount = 0;
            _cardDragListenerCount = 0;
            _cardDropListenerCount = 0;
            _cardHotkeyListenerCount = 0;
            _cancelActionListenerCount = 0;
            _commandStartListenerCount = 0;
            _commandCompleteListenerCount = 0;
            _commandFailedListenerCount = 0;
            _commandCancelledListenerCount = 0;
            _commandEnqueuedListenerCount = 0;
            _commandRollbackListenerCount = 0;
            _cardCastListenerCount = 0;
            _damageListenerCount = 0;
            _movementListenerCount = 0;

            // Clear all arrays
            Array.Clear(_cardPreviewListeners, 0, MAX_LISTENERS_PER_EVENT);
            Array.Clear(_cardDragListeners, 0, MAX_LISTENERS_PER_EVENT);
            Array.Clear(_cardDropListeners, 0, MAX_LISTENERS_PER_EVENT);
            Array.Clear(_cardHotkeyListeners, 0, MAX_LISTENERS_PER_EVENT);
            Array.Clear(_cancelActionListeners, 0, MAX_LISTENERS_PER_EVENT);
            Array.Clear(_commandStartListeners, 0, MAX_LISTENERS_PER_EVENT);
            Array.Clear(_commandCompleteListeners, 0, MAX_LISTENERS_PER_EVENT);
            Array.Clear(_commandFailedListeners, 0, MAX_LISTENERS_PER_EVENT);
            Array.Clear(_commandCancelledListeners, 0, MAX_LISTENERS_PER_EVENT);
            Array.Clear(_commandEnqueuedListeners, 0, MAX_LISTENERS_PER_EVENT);
            Array.Clear(_commandRollbackListeners, 0, MAX_LISTENERS_PER_EVENT);
            Array.Clear(_cardCastListeners, 0, MAX_LISTENERS_PER_EVENT);
            Array.Clear(_damageListeners, 0, MAX_LISTENERS_PER_EVENT);
            Array.Clear(_movementListeners, 0, MAX_LISTENERS_PER_EVENT);
        }

        // Debug information
        public static int GetTotalListenerCount()
        {
            return _cardPreviewListenerCount + _cardDragListenerCount + _cardDropListenerCount + 
                   _cardHotkeyListenerCount + _cancelActionListenerCount + _commandStartListenerCount + 
                   _commandCompleteListenerCount + _commandFailedListenerCount + _commandCancelledListenerCount + 
                   _commandEnqueuedListenerCount + _commandRollbackListenerCount + _cardCastListenerCount + 
                   _damageListenerCount + _movementListenerCount;
        }
    }

    // Event data structures
    [System.Serializable]
    public struct CardCastEvent
    {
        public uint playerId;
        public string cardId;
        public Vector3 castPosition;
        public Vector3 targetPosition;
        public float timestamp;
        
        public static CardCastEvent Create(uint playerId, string cardId, Vector3 castPos, Vector3 targetPos)
        {
            return new CardCastEvent
            {
                playerId = playerId,
                cardId = cardId,
                castPosition = castPos,
                targetPosition = targetPos,
                timestamp = Time.realtimeSinceStartup
            };
        }
    }

    [System.Serializable]
    public struct DamageEvent
    {
        public uint attackerId;
        public uint targetId;
        public float damage;
        public Vector3 position;
        public string damageType;
        public float timestamp;
        
        public static DamageEvent Create(uint attackerId, uint targetId, float damage, Vector3 pos, string type)
        {
            return new DamageEvent
            {
                attackerId = attackerId,
                targetId = targetId,
                damage = damage,
                position = pos,
                damageType = type,
                timestamp = Time.realtimeSinceStartup
            };
        }
    }

    [System.Serializable]
    public struct MovementEvent
    {
        public uint entityId;
        public Vector3 fromPosition;
        public Vector3 toPosition;
        public float duration;
        public float timestamp;
        
        public static MovementEvent Create(uint entityId, Vector3 from, Vector3 to, float duration)
        {
            return new MovementEvent
            {
                entityId = entityId,
                fromPosition = from,
                toPosition = to,
                duration = duration,
                timestamp = Time.realtimeSinceStartup
            };
        }
    }
}