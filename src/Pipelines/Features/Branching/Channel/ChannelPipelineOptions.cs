namespace Dazinator.Extensions.Pipelines.Features.Branching.Channel;
using System.Threading.Channels;

public class ChannelPipelineOptions<T>
{
    public int ReaderCount { get; set; } = 1;
    public int WriterCount { get; set; } = 1;
    public int? MaxCapacity { get; set; }

    public BoundedChannelFullMode FullMode { get; set; } = BoundedChannelFullMode.Wait;
    public bool? SingleWriter { get; set; } = false;
    public bool? SingleReader { get; set; } = false;

    internal Channel<T> CreateChannel()
    {       

        if(MaxCapacity is not  null)
        {
            var options = new BoundedChannelOptions(MaxCapacity ?? int.MaxValue)
            {
                SingleReader = SingleReader ?? ReaderCount == 1,
                SingleWriter = SingleWriter ?? WriterCount == 1,
                FullMode = FullMode // Or other strategies

            };

            return Channel.CreateBounded<T>(options);
        }

        var unboundedOptions = new UnboundedChannelOptions()
        {
            SingleReader = SingleReader ?? ReaderCount == 1,
            SingleWriter = SingleWriter ?? WriterCount == 1,      
        };
        return Channel.CreateUnbounded<T>(unboundedOptions);

    }
}

