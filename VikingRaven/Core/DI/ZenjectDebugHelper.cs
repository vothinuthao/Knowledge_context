using System.Linq;
using System.Text;
using UnityEngine;
using Zenject;

namespace VikingRaven.Core.DI
{
    public static class ZenjectDebugHelper
    {
        /// <summary>
        /// Log detailed information about all bindings in the container
        /// </summary>
        public static void LogBindings(DiContainer container)
        {
            var bindings = container.AllContracts.ToList();
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"=== Zenject Container has {bindings.Count} bindings ===");
            
            foreach (var binding in bindings)
            {
                sb.AppendLine($"Binding for: {binding.Type.Name}");
            }
            
            Debug.Log(sb.ToString());
        }
        
        /// <summary>
        /// Check if all required dependencies are bound in the container
        /// </summary>
        public static void ValidateRequiredBindings(DiContainer container, params System.Type[] requiredTypes)
        {
            var bindings = container.AllContracts.Select(x => x.Type).ToList();
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== Validating required bindings ===");
            
            bool allValid = true;
            foreach (var type in requiredTypes)
            {
                bool found = bindings.Any(b => b == type || type.IsAssignableFrom(b));
                sb.AppendLine($"- {type.Name}: {(found ? "FOUND" : "MISSING")}");
                
                if (!found)
                {
                    allValid = false;
                }
            }
            
            sb.AppendLine($"Validation result: {(allValid ? "SUCCESS" : "FAILURE")}");
            
            if (allValid)
            {
                Debug.Log(sb.ToString());
            }
            else
            {
                Debug.LogError(sb.ToString());
            }
        }
    }
}