using UnityEngine;

namespace VikingRaven.Game.Examples
{
    public class SelectionMarker : MonoBehaviour
    {
        [SerializeField] private float _pulseSpeed = 1.0f;
        [SerializeField] private float _minScale = 0.8f;
        [SerializeField] private float _maxScale = 1.2f;
    
        private void Update()
        {
            float scale = Mathf.Lerp(_minScale, _maxScale, 
                (Mathf.Sin(Time.time * _pulseSpeed) + 1) * 0.5f);
        
            transform.localScale = new Vector3(scale, 1, scale);
        }
    }
}