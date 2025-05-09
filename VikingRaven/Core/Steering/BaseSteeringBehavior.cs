using VikingRaven.Core.ECS;

namespace VikingRaven.Core.Steering
{
    public abstract class BaseSteeringBehavior : ISteeringBehavior
    {
        protected readonly string _name;
        protected float _weight = 1.0f;
        protected bool _isActive = true;
        
        public string Name => _name;
        public float Weight { get => _weight; set => _weight = value; }
        public bool IsActive { get => _isActive; set => _isActive = value; }
        
        protected BaseSteeringBehavior(string name)
        {
            _name = name;
        }
        
        public abstract SteeringOutput Calculate(IEntity entity);
    }
}