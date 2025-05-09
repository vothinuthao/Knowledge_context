using System;
using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Core.DI;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;
using Zenject;

namespace VikingRaven.Core.Factory
{
    public class SquadFactory : MonoBehaviour
    {
        [SerializeField] private int _nextSquadId = 1;

        [Inject] private UnitFactory _unitFactory;
        
        private void Awake()
        {
            Debug.Log("SquadFactory.Awake() - Starting");
        }

        [Obsolete("Obsolete")]
        private void Start()
        {
            if (_unitFactory == null)
            {
                Debug.LogWarning("SquadFactory: _unitFactory is null, trying to find manually");
                _unitFactory = FindObjectOfType<UnitFactory>();
        
                if (_unitFactory == null)
                    Debug.LogError("SquadFactory: Failed to find UnitFactory!");
            }
    
            Debug.Log($"SquadFactory.Start() - _unitFactory: {(_unitFactory != null ? "OK" : "NULL")}");
        }
        
        public List<IEntity> CreateSquad(UnitType unitType, int unitCount, Vector3 position, Quaternion rotation)
        {
            int squadId = _nextSquadId++;
            List<IEntity> squadEntities = new List<IEntity>();
            if (_unitFactory == null)
            {
                Debug.LogError("SquadFactory: Cannot create squad - UnitFactory is null");
                return squadEntities;
            }
            // Create units
            for (int i = 0; i < unitCount; i++)
            {
                // Create the unit
                IEntity unitEntity = _unitFactory.CreateUnit(unitType, position, rotation);

                if (unitEntity != null)
                {
                    // Set formation info
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

            int slotIndex = 0;

            // Create units of each type
            foreach (var kvp in unitCounts)
            {
                UnitType unitType = kvp.Key;
                int count = kvp.Value;

                for (int i = 0; i < count; i++)
                {
                    IEntity unitEntity = _unitFactory.CreateUnit(unitType, position, rotation);

                    if (unitEntity != null)
                    {
                        // Set formation info
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