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
        private UnitFactory UnitFactory => UnitFactory.Instance;
        private SquadFactory SquadFactory => SquadFactory.Instance;
        private SystemRegistry SystemRegistry => SystemRegistry.Instance;
        
        protected override void OnInitialize()
        {
            base.OnInitialize();
            Debug.Log("GameManager initialized as singleton");
            
            if (UnitFactory == null || SquadFactory == null || SystemRegistry == null)
            {
                Debug.LogError("GameManager: One or more dependencies are missing!");
            }
        }
        
        /// <summary>
        /// Creates a squad of units of the specified type
        /// </summary>
        public void CreateSquad(UnitType unitType, int count, Vector3 position)
        {
            if (SquadFactory != null)
            {
                SquadFactory.CreateSquad(unitType, count, position, Quaternion.identity);
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
                SquadFactory.CreateMixedSquad(unitCounts, position, Quaternion.identity);
            }
            else
            {
                Debug.LogError("GameManager: SquadFactory is null, cannot create mixed squad");
            }
        }
    }
}