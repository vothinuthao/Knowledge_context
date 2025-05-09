using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.Systems
{
    public class FormationSystem : BaseSystem
    {
        // Dictionary to track squad formations by squad ID
        private Dictionary<int, Dictionary<FormationType, Vector3[]>> _formationTemplates = 
            new Dictionary<int, Dictionary<FormationType, Vector3[]>>();
        
        private Dictionary<int, FormationType> _currentFormations = new Dictionary<int, FormationType>();
        private Dictionary<int, List<IEntity>> _squadMembers = new Dictionary<int, List<IEntity>>();
        
        public override void Initialize()
        {
            base.Initialize();
            Debug.Log("FormationSystem initialized");
        }
        
        public override void Execute()
        {
            // Group entities by squad
            UpdateSquadMembers();
            
            // Update formation positions for each squad
            foreach (var squadId in _squadMembers.Keys)
            {
                var members = _squadMembers[squadId];
                if (members.Count == 0) continue;
                
                // Get current formation type
                FormationType formationType = FormationType.Line; // Default
                if (_currentFormations.TryGetValue(squadId, out var currentFormation))
                {
                    formationType = currentFormation;
                }
                
                // Ensure we have a formation template for this squad and formation type
                EnsureFormationTemplate(squadId, formationType, members.Count);
                
                // Update formation positions
                UpdateFormationPositions(squadId, members, formationType);
            }
        }
        
        private void UpdateSquadMembers()
        {
            _squadMembers.Clear();
            
            var entities = EntityRegistry.GetEntitiesWithComponent<FormationComponent>();
            
            foreach (var entity in entities)
            {
                var formationComponent = entity.GetComponent<FormationComponent>();
                if (formationComponent == null) continue;
                
                int squadId = formationComponent.SquadId;
                
                if (!_squadMembers.ContainsKey(squadId))
                {
                    _squadMembers[squadId] = new List<IEntity>();
                }
                
                _squadMembers[squadId].Add(entity);
                
                // Update current formation type for the squad
                _currentFormations[squadId] = formationComponent.CurrentFormationType;
            }
        }
        
        private void EnsureFormationTemplate(int squadId, FormationType formationType, int memberCount)
        {
            if (!_formationTemplates.ContainsKey(squadId))
            {
                _formationTemplates[squadId] = new Dictionary<FormationType, Vector3[]>();
            }
            
            if (!_formationTemplates[squadId].ContainsKey(formationType) || 
                _formationTemplates[squadId][formationType].Length != memberCount)
            {
                _formationTemplates[squadId][formationType] = GenerateFormationTemplate(formationType, memberCount);
            }
        }
        
        private Vector3[] GenerateFormationTemplate(FormationType formationType, int count)
        {
            Vector3[] positions = new Vector3[count];
            
            // Generate positions based on formation type
            switch (formationType)
            {
                case FormationType.Line:
                    // Line formation: units side by side
                    for (int i = 0; i < count; i++)
                    {
                        positions[i] = new Vector3(i * 1.5f, 0, 0);
                    }
                    break;
                
                case FormationType.Column:
                    // Column formation: units in a line front to back
                    for (int i = 0; i < count; i++)
                    {
                        positions[i] = new Vector3(0, 0, i * 1.5f);
                    }
                    break;
                
                case FormationType.Phalanx:
                    // Phalanx: grid formation
                    int rows = Mathf.CeilToInt(Mathf.Sqrt(count));
                    for (int i = 0; i < count; i++)
                    {
                        int row = i / rows;
                        int col = i % rows;
                        positions[i] = new Vector3(col * 1.0f, 0, row * 1.0f);
                    }
                    break;
                
                case FormationType.Testudo:
                    // Testudo: tight grid formation
                    rows = Mathf.CeilToInt(Mathf.Sqrt(count));
                    for (int i = 0; i < count; i++)
                    {
                        int row = i / rows;
                        int col = i % rows;
                        positions[i] = new Vector3(col * 0.7f, 0, row * 0.7f);
                    }
                    break;
                
                case FormationType.Circle:
                    // Circle formation
                    float angleStep = 360f / count;
                    for (int i = 0; i < count; i++)
                    {
                        float angle = i * angleStep * Mathf.Deg2Rad;
                        positions[i] = new Vector3(Mathf.Cos(angle) * 3.0f, 0, Mathf.Sin(angle) * 3.0f);
                    }
                    break;
                
                default:
                    // Default to line formation
                    for (int i = 0; i < count; i++)
                    {
                        positions[i] = new Vector3(i * 1.5f, 0, 0);
                    }
                    break;
            }
            
            return positions;
        }
        
        private void UpdateFormationPositions(int squadId, List<IEntity> members, FormationType formationType)
        {
            var formationTemplate = _formationTemplates[squadId][formationType];
            
            // Ensure we don't exceed the template size
            int count = Mathf.Min(members.Count, formationTemplate.Length);
            
            for (int i = 0; i < count; i++)
            {
                var entity = members[i];
                var formationComponent = entity.GetComponent<FormationComponent>();
                
                if (formationComponent != null)
                {
                    formationComponent.SetFormationSlot(i);
                    formationComponent.SetFormationOffset(formationTemplate[i]);
                    formationComponent.SetFormationType(formationType);
                }
            }
        }
        
        // Public method to change formation type for a squad
        public void ChangeFormation(int squadId, FormationType formationType)
        {
            _currentFormations[squadId] = formationType;
            
            // Update all entities in the squad
            if (_squadMembers.TryGetValue(squadId, out var members))
            {
                foreach (var entity in members)
                {
                    var formationComponent = entity.GetComponent<FormationComponent>();
                    if (formationComponent != null)
                    {
                        formationComponent.SetFormationType(formationType);
                    }
                }
            }
        }
    }
}