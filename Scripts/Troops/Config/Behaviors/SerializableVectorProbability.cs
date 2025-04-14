using System;
using Core.Behaviors;
using UnityEngine;

namespace Troops.Config
{
    [Serializable]
    public class SerializableVectorProbability
    {
        public Vector3 direction;
        public float probability;
        
        public SerializableVectorProbability(Vector3 direction, float probability)
        {
            this.direction = direction;
            this.probability = probability;
        }
        
        public VectorProbability ToVectorProbability()
        {
            return new VectorProbability(direction, probability);
        }
    }
}