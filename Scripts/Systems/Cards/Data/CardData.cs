using UnityEngine;
using System;

namespace RavenDeckbuilding.Systems.Cards
{
    [CreateAssetMenu(fileName = "New Card", menuName = "Cards/Card Data")]
    public class CardData : ScriptableObject
    {
        [Header("Basic Info")]
        public string cardName;
        public string cardId;
        [TextArea(2, 4)]
        public string description;
        public Sprite cardIcon;
        
        [Header("Stats")]
        public int manaCost = 1;
        public float cooldown = 1f;
        public float castRange = 5f;
        
        [Header("Card Type")]
        public string cardType = "Damage"; // For factory creation
        public CardTargetType targetType = CardTargetType.Enemy;
        
        [Header("Effects")]
        public CardEffectData[] effects;
        
        [Header("Visual & Audio")]
        public GameObject castEffectPrefab;
        public GameObject impactEffectPrefab;
        public AudioClip castSound;
    }
    
    [Serializable]
    public struct CardEffectData
    {
        public string effectType;
        public float value;
        public float duration;
        public string[] parameters;
    }
    
}