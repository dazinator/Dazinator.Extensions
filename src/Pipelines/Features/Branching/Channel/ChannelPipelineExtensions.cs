namespace Dazinator.Extensions.Pipelines;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Threading.Channels;
using Dazinator.Extensions.Pipelines.Features.Branching;
using Dazinator.Extensions.Pipelines.Features.Branching.Channel;
using Dazinator.Extensions.Pipelines.Features.Branching.PerItem;

public static class ChannelPipelineExtensions
{
    public static IPipelineBuilder UseChannel<T>(
     this IPipelineBuilder builder,
     Action<IPipelineBuilder> configureReaderBranch,
     Action<ChannelWriterBuilder<T>> configureWriterBranch,
     Action<ChannelPipelineOptions<T>>? configureOptions = null)
    {
        var options = new ChannelPipelineOptions<T>();
        configureOptions?.Invoke(options);

        var channel = options.CreateChannel();

        return builder.UseBranchPerInput<(bool IsReader, int Index)>(branch =>
        {
            branch.UseNewScope(); // intelligent scope control.

            Action<IPipelineBuilder> configureBranch;
            if (branch.Input.IsReader)
            {
                // var readerBranch = new ItemBranchBuilder<(bool IsReader, int Index)> (branch, branch.Input); // Initial value doesn't matter
                configureBranch = configureReaderBranch;
                // configureReaderBranch(branch);
            }
            else
            {
                var writerBranch = new ChannelWriterBuilder<T>(branch, channel.Writer);
                configureBranch = (builder) => configureWriterBranch(new ChannelWriterBuilder<T>(builder, channel.Writer));
            }

            if (branch.Input.IsReader)
            {
                branch
                    // .UseNewScope()
                    .Use(next => async context =>
                    {
                        //       // Configure the reader branch once
                        //       await context.ParentPipeline.RunBranch(
                        //context,
                        //configureBranch);

                        try
                        {
                            await foreach (var item in channel.Reader.ReadAllAsync(context.CancellationToken))
                            {
                                // context.SetStepState<T>(item); // Make current item available to downstream middleware
                                await next(context); // Execute the rest of the pipeline with this item
                            }
                        }
                        catch (ChannelClosedException)
                        {
                            // Channel closed normally, exit gracefully
                        }
                    });
            }

        })
        .WithInputs(GenerateBranchInputs(options), opt =>
        {
            opt.MaxDegreeOfParallelism = options.ReaderCount + options.WriterCount;
        });
    }


    private static IEnumerable<(bool IsReader, int Index)> GenerateBranchInputs<T>(ChannelPipelineOptions<T> options)
    {
        // Generate reader branches
        for (int i = 0; i < options.ReaderCount; i++)
        {
            yield return (true, i);
        }

        // Generate writer branches
        for (int i = 0; i < options.WriterCount; i++)
        {
            yield return (false, i);
        }
    }
}

