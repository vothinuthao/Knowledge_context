using UnityEngine;
using Zenject;

namespace VikingRaven.Core.DI
{
    public class ZenjectBinding : MonoBehaviour
    {
        private bool _hasBeenInjected = false;
        private DiContainer _container;

        /// <summary>
        /// Get reference to container when the component is injected
        /// </summary>
        [Inject]
        public void Construct(DiContainer container)
        {
            _container = container;
            _hasBeenInjected = true;
            Debug.Log($"ZenjectBinding: Successfully injected on {gameObject.name}");
        }

        private void Start()
        {
            if (!_hasBeenInjected)
            {
                Debug.LogWarning($"ZenjectBinding: GameObject {gameObject.name} was not injected by Zenject!");
            }
        }

        /// <summary>
        /// Helper method to inject a GameObject manually
        /// </summary>
        public void InjectGameObject(GameObject gameObject)
        {
            if (_container != null)
            {
                _container.InjectGameObject(gameObject);
                Debug.Log($"Manually injected GameObject: {gameObject.name}");
            }
            else
            {
                Debug.LogError($"Cannot inject {gameObject.name} - Container is null!");
            }
        }
    }
}