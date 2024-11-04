namespace Dazinator.Extensions.Pipelines.Features.ProducerConsumer;
using Dazinator.Extensions.Pipelines.Features.Branching;
using System.Threading.Channels;

public class ChannelWriterBuilder<T> : ItemBranchBuilder<ChannelWriter<T>>
{
    public ChannelWriterBuilder(IPipelineBuilder inner, ChannelWriter<T> writer)
        : base(inner, writer)
    {
    }

    public ChannelWriter<T> Writer => Input;
}

