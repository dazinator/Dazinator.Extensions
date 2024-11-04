namespace Dazinator.Extensions.Pipelines.Features.ProducerConsumer;
using Dazinator.Extensions.Pipelines.Features.Branching;
using System.Threading.Channels;

public class ChannelReaderBuilder<T> : ItemBranchBuilder<ChannelReader<T>>
{
    public ChannelReaderBuilder(IPipelineBuilder inner, ChannelReader<T> reader)
        : base(inner, reader)
    {
    }

    public ChannelReader<T> Reader => Input;

    /// <summary>
    /// Convenience method.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public IAsyncEnumerable<T> ReadAllAsync(PipelineContext context) => Reader.ReadAllAsync(context.CancellationToken);


}

