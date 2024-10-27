#pragma warning disable IDE0130 // Namespace does not match folder structure - for discoverability
namespace Dazinator.Extensions.Pipelines;
#pragma warning restore IDE0130 // Namespace does not match folder structure
using Dazinator.Extensions.Pipelines.Features.StepOptions;


public static class StepConfigurationExtensions
{
    public static PipelineBuilder Configure<TOptions>(
         this PipelineBuilder builder,
         Action<TOptions> configure)
         where TOptions : class, IStepOptions, new()
    {
        var options = new TOptions();
        configure(options);

        return builder.WrapLastComponent(originalComponent =>
        {
            return (sp, next) =>
            {
                var originalStep = originalComponent(sp, next);
                return async context =>
                {
                    context.StepState.Set(options);
                    await originalStep(context);
                };
            };
        });
    }
}
