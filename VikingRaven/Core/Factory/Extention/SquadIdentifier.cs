// <summary>

using Sirenix.OdinInspector;
using UnityEngine;
using VikingRaven.Units.Data;

namespace VikingRaven.Core.Factory
{
    
    public class SquadIdentifier : MonoBehaviour
    {
        [Header("Squad Information")]
        [SerializeField, ReadOnly] private int _squadId;
        [SerializeField, ReadOnly] private string _squadName;
        [SerializeField, ReadOnly] private uint _squadDataId;
        
        public int SquadId => _squadId;
        public string SquadName => _squadName;
        public uint SquadDataId => _squadDataId;
        
        public void SetSquadData(int squadId, SquadDataSO squadData)
        {
            _squadId = squadId;
            _squadName = squadData?.DisplayName ?? "Unknown Squad";
            _squadDataId = squadData?.SquadId ?? 0;
            
            // Update GameObject name
            gameObject.name = $"Squad_{squadId}_{_squadName}";
        }
    }
}