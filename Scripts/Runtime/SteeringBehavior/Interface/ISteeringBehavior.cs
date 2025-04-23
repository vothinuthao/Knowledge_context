using Core.Patterns.StrategyPattern;
using UnityEngine;

namespace SteeringBehavior
{
    public interface ISteeringBehavior : IStrategy<SteeringContext, Vector3>
    {
        string GetName();
        int GetPriority();
        void SetEnabled(bool enabled);
        bool IsEnabled();
        void SetWeight(float newWeight);
        void SetPriority(int newPriority);
    }
}