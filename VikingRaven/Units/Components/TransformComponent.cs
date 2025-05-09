using UnityEngine;
using VikingRaven.Core.ECS;

namespace VikingRaven.Units.Components
{
    public class TransformComponent : BaseComponent
    {
        [SerializeField] private Transform _transform;
        [SerializeField] private float _rotationSpeed = 5.0f;
        
        public Vector3 Position => _transform.position;
        public Quaternion Rotation => _transform.rotation;
        public Vector3 Forward => _transform.forward;
        public Vector3 Right => _transform.right;

        public override void Initialize()
        {
            if (_transform == null)
            {
                _transform = GetComponent<Transform>();
            }
        }

        public void Move(Vector3 movement)
        {
            _transform.position += movement;
        }

        public void LookAt(Vector3 target)
        {
            Vector3 direction = (target - _transform.position).normalized;
            direction.y = 0; // Keep rotation in horizontal plane
            
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                _transform.rotation = Quaternion.Slerp(_transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
            }
        }

        public void SetPosition(Vector3 position)
        {
            _transform.position = position;
        }

        public void SetRotation(Quaternion rotation)
        {
            _transform.rotation = rotation;
        }
    }
}