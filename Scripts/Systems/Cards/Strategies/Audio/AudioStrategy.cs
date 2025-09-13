using UnityEngine;

namespace RavenDeckbuilding.Systems.Cards.Strategies
{
    /// <summary>
    /// Strategy for audio effects
    /// </summary>
    public class AudioStrategy : ICardStrategy
    {
        private AudioClip _audioClip;
        
        public string StrategyName => "Audio Effects";
        public CardStrategyCategory Category => CardStrategyCategory.Audio;
        public int Priority => 50; // Lowest priority for audio
        
        public AudioStrategy(AudioClip audioClip)
        {
            _audioClip = audioClip;
        }
        
        public bool CanExecute(CardExecutionContext context)
        {
            return _audioClip != null;
        }
        
        public void Execute(CardExecutionContext context)
        {
            if (_audioClip != null && context.caster != null)
            {
                AudioSource.PlayClipAtPoint(_audioClip, context.caster.transform.position);
                Debug.Log($"Played audio clip: {_audioClip.name}");
            }
        }
    }
}