using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Core.Factory;
using VikingRaven.Units.Components;
using Zenject;

namespace VikingRaven.Game
{
    public class GameManager : MonoBehaviour
    {
        [Inject] private UnitFactory _unitFactory;
        [Inject] private SquadFactory _squadFactory;
        [Inject] private SystemRegistry _systemRegistry;
        
        /// <summary>
        /// Creates a squad of units of the specified type
        /// </summary>
        public void CreateSquad(UnitType unitType, int count, Vector3 position)
        {
            _squadFactory.CreateSquad(unitType, count, position, Quaternion.identity);
        }

        /// <summary>
        /// Creates a mixed squad with different unit types
        /// </summary>
        public void CreateMixedSquad(Vector3 position)
        {
            Dictionary<UnitType, int> unitCounts = new Dictionary<UnitType, int>
            {
                { UnitType.Infantry, 4 },
                { UnitType.Archer, 2 },
                { UnitType.Pike, 2 }
            };
            
            _squadFactory.CreateMixedSquad(unitCounts, position, Quaternion.identity);
        }
    }
}