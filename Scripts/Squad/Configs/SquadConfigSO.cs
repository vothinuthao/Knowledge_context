using Core;
using UnityEngine;

namespace Troops.Config
{
    /// <summary>
    /// Scriptable Object containing squad configuration
    /// </summary>
    [CreateAssetMenu(fileName = "SquadConfig", menuName = "WikingRaven/SquadConfig")]
    public class SquadConfigSO : ScriptableObject
    {
        [Header("Squad Settings")]
        public string squadName = "New Squad";
        public GameDefineData.SquadType squadType;
        public GameDefineData.Formation.FormationType formationType = GameDefineData.Formation.FormationType.Square;
        public int maxTroops = 9;
        public Color squadColor = Color.white;
        
        [Header("Troop Settings")]
        public TroopConfigSO troopConfig;
        public float troopSpacing = 1.0f;
        
        [Header("Behavior Settings")]
        public float squadCohesionStrength = 1.0f;
        public float moveSpeed = 4.0f;
        
        [Header("Visual Settings")]
        public GameObject squadBannerPrefab;
    }
}