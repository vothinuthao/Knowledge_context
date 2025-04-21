using SteeringBehavior;

namespace Troop
{
    public enum BehaviorCategory
    {
        Movement,       // Di chuyển cơ bản
        Formation,      // Đội hình
        Combat,         // Chiến đấu
        Special,        // Đặc biệt
        Essential       // Cần thiết (luôn có)
    }
    [System.Serializable]
    public class BehaviorTemplate
    {
        public string name;
        public SteeringBehaviorSO behaviorSO;
        public BehaviorCategory category;
        public bool isDefault;
        public string description;
    }
}