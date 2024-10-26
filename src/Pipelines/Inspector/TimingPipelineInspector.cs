namespace Dazinator.Extensions.Pipelines.Inspector;
using System.Threading.Tasks;

public class TimingPipelineInspector : IPipelineInspector
{
    private readonly List<PipelineStepContext> _timings = new();

    public Task BeforeStepAsync(PipelineStepContext context)
        => Task.CompletedTask;

    public Task AfterStepAsync(PipelineStepContext context)
    {
        _timings.Add(context);
        return Task.CompletedTask;
    }

    public Task OnExceptionAsync(PipelineStepContext context)
    {
        _timings.Add(context);
        return Task.CompletedTask;
    }

    public IReadOnlyList<PipelineStepContext> GetTimings() => _timings;
}
