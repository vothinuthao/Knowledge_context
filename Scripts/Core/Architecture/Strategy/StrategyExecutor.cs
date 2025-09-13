using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RavenDeckbuilding.Core.Architecture.Strategy
{
    /// <summary>
    /// Generic strategy executor that manages and executes strategies
    /// </summary>
    public class StrategyExecutor<TContext, TStrategy> 
        where TStrategy : IStrategy<TContext>
    {
        private List<TStrategy> _strategies;
        private bool _needsSorting = false;
        
        public StrategyExecutor()
        {
            _strategies = new List<TStrategy>();
        }
        
        /// <summary>
        /// Add strategy to executor
        /// </summary>
        public void AddStrategy(TStrategy strategy)
        {
            _strategies.Add(strategy);
            _needsSorting = true;
        }
        
        /// <summary>
        /// Remove strategy from executor
        /// </summary>
        public bool RemoveStrategy(TStrategy strategy)
        {
            return _strategies.Remove(strategy);
        }
        
        /// <summary>
        /// Execute all valid strategies in priority order
        /// </summary>
        public void ExecuteAll(TContext context)
        {
            if (_needsSorting)
            {
                SortStrategies();
                _needsSorting = false;
            }
            
            foreach (var strategy in _strategies)
            {
                if (strategy.CanExecute(context))
                {
                    strategy.Execute(context);
                }
            }
        }
        
        /// <summary>
        /// Execute first valid strategy
        /// </summary>
        public bool ExecuteFirst(TContext context)
        {
            if (_needsSorting)
            {
                SortStrategies();
                _needsSorting = false;
            }
            
            var validStrategy = _strategies.FirstOrDefault(s => s.CanExecute(context));
            if (validStrategy != null)
            {
                validStrategy.Execute(context);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Get all strategies that can execute with given context
        /// </summary>
        public IEnumerable<TStrategy> GetValidStrategies(TContext context)
        {
            return _strategies.Where(s => s.CanExecute(context));
        }
        
        /// <summary>
        /// Sort strategies by priority (highest first)
        /// </summary>
        private void SortStrategies()
        {
            _strategies.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }
        
        /// <summary>
        /// Clear all strategies
        /// </summary>
        public void Clear()
        {
            _strategies.Clear();
        }
        
        /// <summary>
        /// Get strategy count
        /// </summary>
        public int Count => _strategies.Count;
    }
}