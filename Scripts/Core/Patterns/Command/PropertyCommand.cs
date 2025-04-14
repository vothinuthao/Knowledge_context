using System;

namespace Core.Patterns
{
    /// <summary>
    /// Generic command for modifying properties
    /// </summary>
    /// <typeparam name="T">Type of object being modified</typeparam>
    /// <typeparam name="TValue">Type of value being modified</typeparam>
    public class PropertyCommand<T, TValue> : ACommandBase
    {
        protected T target;
        protected Func<T, TValue> Getter;
        protected Action<T, TValue> setter;
        protected TValue newValue;
        protected TValue oldValue;
        
        /// <summary>
        /// Constructor
        /// </summary>
        public PropertyCommand(T target, Func<T, TValue> getter, Action<T, TValue> setter, TValue newValue, string description)
            : base(description)
        {
            this.target = target;
            this.Getter = getter;
            this.setter = setter;
            this.newValue = newValue;
            
            // Store old value for undo
            this.oldValue = getter(target);
        }
        
        /// <summary>
        /// Execute the command
        /// </summary>
        public override void Execute()
        {
            setter(target, newValue);
        }
        
        /// <summary>
        /// Undo the command
        /// </summary>
        public override void Undo()
        {
            setter(target, oldValue);
        }
    }
}