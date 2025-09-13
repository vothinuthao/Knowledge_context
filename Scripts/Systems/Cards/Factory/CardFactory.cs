using RavenDeckbuilding.Core.Architecture.Factory;
using UnityEngine;

namespace RavenDeckbuilding.Systems.Cards
{
    /// <summary>
    /// Factory for creating cards using Factory pattern
    /// </summary>
    public class CardFactory : BaseFactory<StrategyBaseCard, CardData>
    {
        [Header("Card Prefabs")]
        [SerializeField] private GameObject damageCardPrefab;
        [SerializeField] private GameObject healCardPrefab;
        [SerializeField] private GameObject movementCardPrefab;
        
        protected override void RegisterCreators()
        {
            RegisterCreator<DamageCard>("Damage", CreateDamageCard);
            RegisterCreator<HealCard>("Heal", CreateHealCard);
            RegisterCreator<MovementCard>("Movement", CreateMovementCard);
        }
        
        protected override string GetTypeName(CardData data)
        {
            return data.cardType;
        }
        
        private StrategyBaseCard CreateDamageCard(CardData data)
        {
            GameObject cardObj = Instantiate(damageCardPrefab);
            DamageCard card = cardObj.GetComponent<DamageCard>();
            card.Initialize(data);
            return card;
        }
        
        private StrategyBaseCard CreateHealCard(CardData data)
        {
            GameObject cardObj = Instantiate(healCardPrefab);
            HealCard card = cardObj.GetComponent<HealCard>();
            card.Initialize(data);
            return card;
        }
        
        private StrategyBaseCard CreateMovementCard(CardData data)
        {
            GameObject cardObj = Instantiate(movementCardPrefab);
            MovementCard card = cardObj.GetComponent<MovementCard>();
            card.Initialize(data);
            return card;
        }
    }
}