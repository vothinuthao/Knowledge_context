using System.Collections.Generic;

namespace Core.Patterns.StrategyPattern
{
    public class CompositeStrategy<TContext, TOutput> where TOutput : new()
    {
        protected List<IStrategy<TContext, TOutput>> strategies = new List<IStrategy<TContext, TOutput>>();
        public void AddStrategy(IStrategy<TContext, TOutput> strategy)
        {
            strategies.Add(strategy);
        }
        public void RemoveStrategy(IStrategy<TContext, TOutput> strategy)
        {
            strategies.Remove(strategy);
        }
        public void ClearStrategies()
        {
            strategies.Clear();
        }
        public virtual TOutput Execute(TContext context, System.Func<List<(TOutput, float)>, TOutput> combiner)
        {
            var results = new List<(TOutput, float)>();
            
            foreach (var strategy in strategies)
            {
                TOutput result = strategy.Execute(context);
                float weight = strategy.GetWeight();
                results.Add((result, weight));
            }
            
            return combiner(results);
        }
    }
}