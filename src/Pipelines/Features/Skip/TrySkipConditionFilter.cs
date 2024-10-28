#pragma warning disable IDE0130 // Namespace does not match folder structure - for discoverability
namespace Dazinator.Extensions.Pipelines.Features.Skip;
#pragma warning restore IDE0130 // Namespace does not match folder structure

using System.Threading.Tasks;
using Dazinator.Extensions.Pipelines.Features.Filter;

public class TrySkipConditionFilter : IStepFilter
{
    private readonly Func<PipelineContext, Task<bool>> _condition;
    private readonly Action<Exception>? _onError;

    public TrySkipConditionFilter(
        Func<PipelineContext, Task<bool>> condition,
        Action<Exception>? onError)
    {
        _condition = condition;
        _onError = onError;
    }

    public async Task BeforeStepAsync(PipelineStepContext context)
    {
        try
        {
            if (!await _condition(context.PipelineContext))
            {
                context.ShouldSkip = true;
            }
        }
        catch (Exception ex)
        {
            _onError?.Invoke(ex);
            context.ShouldSkip = true;  // Skip on error
        }
    }

    public Task AfterStepAsync(PipelineStepContext context) => Task.CompletedTask;
}
