namespace Dazinator.Extensions.Pipelines.Features.Filter;
using System.Threading.Tasks;

public interface IStepFilter
{
    Task BeforeStepAsync(PipelineStepContext context);
    Task AfterStepAsync(PipelineStepContext context);
}
