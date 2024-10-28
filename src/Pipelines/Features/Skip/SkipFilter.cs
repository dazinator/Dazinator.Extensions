#pragma warning disable IDE0130 // Namespace does not match folder structure - for discoverability
namespace Dazinator.Extensions.Pipelines.Features.Skip;
#pragma warning restore IDE0130 // Namespace does not match folder structure

using System.Threading.Tasks;
using Dazinator.Extensions.Pipelines.Features.Filter;

public class SkipConditionFilter : IStepFilter
{
    private readonly Func<PipelineContext, Task<bool>> _shouldSkip;

    public SkipConditionFilter(Func<PipelineContext, Task<bool>> shouldSkip)
    {
        _shouldSkip = shouldSkip;
    }

    public async Task BeforeStepAsync(PipelineStepContext context)
    {
        if (await _shouldSkip(context.PipelineContext))
        {
            context.ShouldSkip = true;
        }
    }

    public Task AfterStepAsync(PipelineStepContext context) => Task.CompletedTask;
}
