using System.Collections.Generic;
using SteeringBehavior;
using UnityEngine;

namespace Troop
{
    [CreateAssetMenu(fileName = "Behavior Template Set", menuName = "Wiking Raven/Behavior Templates/Template Set")]
    public class BehaviorTemplateSet : ScriptableObject
    {
        [SerializeField]
        private List<BehaviorTemplate> templates = new List<BehaviorTemplate>();

        // Lấy tất cả behavior trong một category
        public List<SteeringBehaviorSO> GetBehaviorsByCategory(BehaviorCategory category)
        {
            List<SteeringBehaviorSO> result = new List<SteeringBehaviorSO>();
            
            foreach (var template in templates)
            {
                if (template.category == category)
                {
                    result.Add(template.behaviorSO);
                }
            }
            
            return result;
        }

        // Lấy tất cả behavior mặc định
        public List<SteeringBehaviorSO> GetDefaultBehaviors()
        {
            List<SteeringBehaviorSO> result = new List<SteeringBehaviorSO>();
            
            foreach (var template in templates)
            {
                if (template.isDefault)
                {
                    result.Add(template.behaviorSO);
                }
            }
            
            return result;
        }

        // Lấy tất cả behavior cần thiết (Essential)
        public List<SteeringBehaviorSO> GetEssentialBehaviors()
        {
            return GetBehaviorsByCategory(BehaviorCategory.Essential);
        }
        
        // Lấy template theo tên
        public BehaviorTemplate GetTemplateByName(string name)
        {
            return templates.Find(t => t.name == name);
        }
        
        // Thêm template mới
        public void AddTemplate(BehaviorTemplate template)
        {
            // Kiểm tra xem đã có template với tên này chưa
            if (templates.Find(t => t.name == template.name) == null)
            {
                templates.Add(template);
            }
        }
        
        // Xóa template
        public void RemoveTemplate(string name)
        {
            templates.RemoveAll(t => t.name == name);
        }
    }
}