namespace Dazinator.Extensions.Pipelines.Features.Job;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

public interface IJob
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}
