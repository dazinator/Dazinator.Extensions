namespace Dazinator.Extensions.Pipelines.Features.Branching.Channel;
using System.Threading.Channels;

public class ChannelWriterBuilder<T> : ItemBranchBuilder<ChannelWriter<T>>
{
    public ChannelWriterBuilder(IPipelineBuilder inner, ChannelWriter<T> writer)
        : base(inner, writer)
    {
    }

    public ChannelWriter<T> Writer => Input;
}

