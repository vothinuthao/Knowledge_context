using Core.Patterns.StrategyPattern;
using UnityEngine;

namespace SteeringBehavior
{
    public interface ISteeringBehavior : IStrategy<SteeringContext, Vector3>
    {
        string GetName();
    }
}