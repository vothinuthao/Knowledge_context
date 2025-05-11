using System;
using System.Collections.Generic;
using Core.Utils;
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Core.Factory;
using VikingRaven.Units.Components;

namespace VikingRaven.Game
{
    public class GameManager : Singleton<GameManager>
    {
        [SerializeField] private UnitFactory _unitFactory;
        private SquadFactory SquadFactory => SquadFactory.Instance;
        private SystemRegistry SystemRegistry => SystemRegistry.Instance;
        
        public UnitFactory UnitFactory => _unitFactory;
        
        
        /// <summary>
        /// Creates a squad of units of the specified type
        /// </summary>
        public void CreateSquad(UnitType unitType, int count, Vector3 position)
        {
            if (SquadFactory != null)
            {
                // SquadFactory.CreateSquad(unitType, count, position, Quaternion.identity);
            }
            else
            {
                Debug.LogError("GameManager: SquadFactory is null, cannot create squad");
            }
        }

        /// <summary>
        /// Creates a mixed squad with different unit types
        /// </summary>
        public void CreateMixedSquad(Vector3 position)
        {
            if (SquadFactory != null)
            {
                Dictionary<UnitType, int> unitCounts = new Dictionary<UnitType, int>
                {
                    { UnitType.Infantry, 4 },
                    { UnitType.Archer, 2 },
                    { UnitType.Pike, 2 }
                };
                // SquadFactory.CreateMixedSquad(unitCounts, position, Quaternion.identity);
            }
            else
            {
                Debug.LogError("GameManager: SquadFactory is null, cannot create mixed squad");
            }
        }
    }
}