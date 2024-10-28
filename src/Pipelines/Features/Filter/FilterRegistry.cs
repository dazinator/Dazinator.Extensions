namespace Dazinator.Extensions.Pipelines.Features.Filter;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

public class FilterRegistry
{
     private readonly Dictionary<int, List<Func<IServiceProvider, IStepFilter>>> _filterFactories = new();

    public int CurrentStepIndex { get; set; }
   

    /// <summary>
    /// Adds a filter that will be created using the supplied factory method.
    /// </summary>
    /// <param name="stepIndex"></param>
    /// <param name="filterFactory"></param>
    public void AddFilter(Func<IServiceProvider, IStepFilter> filterFactory, int? stepIndex = null)
    {
        var targetStepIndex = stepIndex ?? CurrentStepIndex;
        var factories = EnsureFactoriesForStep(targetStepIndex);
        factories.Add(filterFactory);
    }

    /// <summary>
    /// Filters registered here must be registered with the DI container as a service.
    /// </summary>
    /// <typeparam name="TFilter"></typeparam>
    /// <param name="stepIndex"></param>
    public void AddFilterFromServices<TFilter>(int? stepIndex = null)
        where TFilter : IStepFilter
    {
        var targetStepIndex = stepIndex ?? CurrentStepIndex;
        var factories = EnsureFactoriesForStep(targetStepIndex);
        factories.Add((sp)=>sp.GetRequiredService<TFilter>());       
    }

    private List<Func<IServiceProvider, IStepFilter>> EnsureFactoriesForStep(int stepIndex)
    {
        if (!_filterFactories.TryGetValue(stepIndex, out var factories))
        {
            factories = new List<Func<IServiceProvider, IStepFilter>>();
            _filterFactories[stepIndex] = factories;
        }

        return factories;
    }   


    public IReadOnlyList<IStepFilter> GetFilters(IServiceProvider serviceProvider, int? stepIndex)
    {
        var targetStepIndex = stepIndex ?? CurrentStepIndex;
        if (_filterFactories.TryGetValue(targetStepIndex, out var factories))
        {
            return factories.Select(f => f(serviceProvider)).ToList();
        }
        return Array.Empty<IStepFilter>();
    }
}
