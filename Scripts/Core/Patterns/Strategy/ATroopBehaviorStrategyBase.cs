using Troops.Base;

namespace Core.Patterns
{
    /// <summary>
    /// Base class for all troop behavior strategies
    /// </summary>
    public abstract class ATroopBehaviorStrategyBase : ITroopBehaviorStrategy
    {
        protected string strategyName;
        protected int priority;
        
        public string Name => strategyName;
        public int Priority => priority;
        
        public ATroopBehaviorStrategyBase(string name, int priority)
        {
            this.strategyName = name;
            this.priority = priority;
        }
        
        public abstract bool ShouldExecute(TroopBase troop);
        public abstract void Execute(TroopBase troop);
    }
}