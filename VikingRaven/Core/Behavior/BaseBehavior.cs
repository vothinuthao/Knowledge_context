using VikingRaven.Core.ECS;

namespace VikingRaven.Core.Behavior
{
    public abstract class BaseBehavior : IBehavior
    {
        protected readonly IEntity _entity;
        protected readonly string _name;
        protected float _weight;
        protected bool _isActive = true;

        public string Name => _name;
        public float Weight
        {
            get => _weight;
            set => _weight = value;
        }

        public bool IsActive { get => _isActive; set => _isActive = value; }
        public IEntity Entity => _entity;

        protected BaseBehavior(string name, IEntity entity)
        {
            _name = name;
            _entity = entity;
            _weight = 0f;
        }

        public virtual void SetWeight(float weight)
        {
            _weight = weight;
        }

        public virtual float CalculateWeight()
        {
            return _weight;
        }

        public abstract void Execute();
        
        public virtual void Initialize() { }
        public virtual void Cleanup() { }
    }
}