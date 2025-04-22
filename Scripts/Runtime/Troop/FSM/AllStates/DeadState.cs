using Troop;
using UnityEngine;

public class DeadState : TroopStateBase
    {
        private float _despawnTimer = 5f;
        private float _bodyFadeTimer = 3f; 
        private float _fadeSpeed = 1.0f;

        public DeadState()
        {
            stateEnum = TroopState.Dead;
        }

        public override void Enter(TroopController troop)
        {
            // Vô hiệu hóa tất cả behaviors
            DisableAllBehaviors(troop);
            
            // Set velocity và acceleration về 0
            troop.GetModel().Velocity = Vector3.zero;
            troop.GetModel().Acceleration = Vector3.zero;
            
            // Kích hoạt animation chết
            troop.TroopView.TriggerAnimation("Death");
            
            // Vô hiệu hóa colliders
            Collider[] colliders = troop.GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
            {
                collider.enabled = false;
            }
            
            // Vô hiệu hóa rigidbody nếu có
            Rigidbody rb = troop.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }
            
            // Hủy đăng ký khỏi TroopManager và SquadSystem
            if (TroopManager.Instance != null)
            {
                TroopManager.Instance.UnregisterTroop(troop);
            }
            
            var squadSystem = TroopControllerSquadExtensions.Instance?.GetSquad(troop);
            if (squadSystem != null)
            {
                squadSystem.RemoveTroop(troop);
            }
            
            // Reset timers
            _despawnTimer = 5f;
            _bodyFadeTimer = 3f;
        }

        public override void Update(TroopController troop)
        {
            // Đếm ngược thời gian despawn
            _despawnTimer -= Time.deltaTime;
            _bodyFadeTimer -= Time.deltaTime;
            
            // Sau một khoảng thời gian, bắt đầu làm mờ dần body
            if (_bodyFadeTimer <= 0)
            {
                // Lấy tất cả renderers và làm mờ dần
                Renderer[] renderers = troop.GetComponentsInChildren<Renderer>();
                
                foreach (var renderer in renderers)
                {
                    // Lấy tất cả materials
                    Material[] materials = renderer.materials;
                    
                    for (int i = 0; i < materials.Length; i++)
                    {
                        // Đảm bảo material hỗ trợ transparent
                        if (materials[i].HasProperty("_Mode"))
                        {
                            materials[i].SetFloat("_Mode", 3); // Transparent mode
                        }
                        
                        // Enable transparent mode
                        materials[i].SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        materials[i].SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        materials[i].SetInt("_ZWrite", 0);
                        materials[i].DisableKeyword("_ALPHATEST_ON");
                        materials[i].EnableKeyword("_ALPHABLEND_ON");
                        materials[i].DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        materials[i].renderQueue = 3000;
                        
                        // Giảm dần alpha
                        Color color = materials[i].color;
                        color.a = Mathf.Max(0, color.a - _fadeSpeed * Time.deltaTime);
                        materials[i].color = color;
                    }
                    
                    // Áp dụng materials mới
                    renderer.materials = materials;
                }
            }
            
            if (_despawnTimer <= 0)
            {
                // Despawn troop
                GameObject.Destroy(troop.gameObject);
            }
        }

        public override void Exit(TroopController troop)
        {
            // Dead là trạng thái cuối cùng, không có exit
        }
        
        private void DisableAllBehaviors(TroopController troop)
        {
            foreach (var behavior in troop.GetModel().SteeringBehavior.GetSteeringBehaviors())
            {
                troop.EnableBehavior(behavior.GetName(), false);
            }
        }
    }
