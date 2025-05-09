using System;
using System.Collections.Generic;
using Core.Utils;
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Game;
using VikingRaven.Units.Components;

namespace VikingRaven.Core.Factory
{
    public class SquadFactory : Singleton<SquadFactory>
    {
        [SerializeField] private int _nextSquadId = 1;
        
        
        public List<IEntity> CreateSquad(UnitType unitType, int unitCount, Vector3 position, Quaternion rotation)
        {
            int squadId = _nextSquadId++;
            List<IEntity> squadEntities = new List<IEntity>();
            
            // Get the UnitFactory instance
            UnitFactory unitFactory = GameManager.Instance.UnitFactory;
            if (unitFactory == null)
            {
                Debug.LogError("SquadFactory: Cannot create squad - UnitFactory is null");
                return squadEntities;
            }
            
            // Create units
            for (int i = 0; i < unitCount; i++)
            {
                IEntity unitEntity = unitFactory.CreateUnit(unitType, position, rotation);

                if (unitEntity != null)
                {
                    var formationComponent = unitEntity.GetComponent<FormationComponent>();
                    if (formationComponent != null)
                    {
                        formationComponent.SetSquadId(squadId);
                        formationComponent.SetFormationSlot(i);
                        formationComponent.SetFormationType(FormationType.Line); // Default formation
                    }

                    squadEntities.Add(unitEntity);
                }
            }

            return squadEntities;
        }

        public List<IEntity> CreateMixedSquad(Dictionary<UnitType, int> unitCounts, Vector3 position,
            Quaternion rotation)
        {
            int squadId = _nextSquadId++;
            List<IEntity> squadEntities = new List<IEntity>();
            
            UnitFactory unitFactory = GameManager.Instance.UnitFactory;
            if (unitFactory == null)
            {
                Debug.LogError("SquadFactory: Cannot create squad - UnitFactory is null");
                return squadEntities;
            }
            int slotIndex = 0;
            foreach (var kvp in unitCounts)
            {
                UnitType unitType = kvp.Key;
                int count = kvp.Value;

                for (int i = 0; i < count; i++)
                {
                    IEntity unitEntity = unitFactory.CreateUnit(unitType, position, rotation);

                    if (unitEntity != null)
                    {
                        var formationComponent = unitEntity.GetComponent<FormationComponent>();
                        if (formationComponent != null)
                        {
                            formationComponent.SetSquadId(squadId);
                            formationComponent.SetFormationSlot(slotIndex++);
                            formationComponent.SetFormationType(FormationType.Line); // Default formation
                        }

                        squadEntities.Add(unitEntity);
                    }
                }
            }

            return squadEntities;   
        }
    }
}