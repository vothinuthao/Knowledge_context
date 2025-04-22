using UnityEngine;

namespace SteeringBehavior
{
    public abstract class SteeringBehaviorBase : ISteeringBehavior
    {
        protected float weight;
        protected string behaviorName;
        protected bool isEnabled = true;
        protected int priority = 0; // Higher priority behaviors take precedence
    
        public SteeringBehaviorBase(float weight, string name, int priority = 0)
        {
            this.weight = weight;
            this.behaviorName = name;
            this.priority = priority;
        }
    
        public abstract Vector3 Execute(SteeringContext context);
    
        public float GetWeight()
        {
            return isEnabled ? weight : 0f;
        }
    
        public string GetName()
        {
            return behaviorName;
        }
        
        public int GetPriority()
        {
            return priority;
        }
        
        public void SetEnabled(bool enabled)
        {
            isEnabled = enabled;
        }
        
        public bool IsEnabled()
        {
            return isEnabled;
        }
        
        public void SetWeight(float newWeight)
        {
            weight = newWeight;
        }
        
        public void SetPriority(int newPriority)
        {
            priority = newPriority;
        }
        
        protected Vector3 CalculateSteeringForce(Vector3 desiredVelocity, SteeringContext context)
        {
            if (context.TroopModel == null) return Vector3.zero;
            return desiredVelocity - context.TroopModel.Velocity;
        }
    }
}