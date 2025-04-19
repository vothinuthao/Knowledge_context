using UnityEngine;

namespace SteeringBehavior
{
    public abstract class SteeringBehaviorBase : ISteeringBehavior
    {
        protected float weight;
        protected string behaviorName;
    
        public SteeringBehaviorBase(float weight, string name)
        {
            this.weight = weight;
            this.behaviorName = name;
        }
    
        public abstract Vector3 Execute(SteeringContext context);
    
        public float GetWeight()
        {
            return weight;
        }
    
        public string GetName()
        {
            return behaviorName;
        }
        protected Vector3 CalculateSteeringForce(Vector3 desiredVelocity, SteeringContext context)
        {
            if (context.TroopModel == null) return Vector3.zero;
            return desiredVelocity - context.TroopModel.Velocity;
        }
    }
}