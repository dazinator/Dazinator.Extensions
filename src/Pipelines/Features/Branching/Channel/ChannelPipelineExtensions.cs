namespace Dazinator.Extensions.Pipelines;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics.SymbolStore;
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

        if (options.ReaderCount == 0 && options.WriterCount == 0)
        {
            return builder; // No readers or writers, nothing to do.
        }

        var channel = options.CreateChannel();

        // var readerBranches = new List<ItemBranchBuilder<(bool IsReader, int Index)>>();      
        //var writerBranchIndexes = (options.WriterCount == 0 ? Enumerable.Empty<int>() : Enumerable.Range(0, options.WriterCount)).ToArray();

        var readerWriterFork = new List<bool>(2);
        if (options.ReaderCount > 0)
        {
            readerWriterFork.Add(true); // bool indicates reader 
        }
        if (options.WriterCount > 0)
        {
            readerWriterFork.Add(false); // bool indicates reader 
        }

        // We fork the pipeline into a branch for readers and a branch for writers.
        // this makes it easier to know when all of the writers have finished writing, in order to complete the channel.
        return builder.UseBranchPerInput<bool>(branch =>
        {
            // We now need to split the pipeline into the concurrent readers or writers.
            bool isReader = branch.Input;
            var branchCount = isReader ? options.ReaderCount : options.WriterCount;
            var indexes = (branchCount == 0 ? Enumerable.Empty<int>() : Enumerable.Range(0, branchCount)).ToArray();


            // We need to surround the writer branch with a middleware that will complete the channel when all writers have finished writing.

            if (!branch.Input && options.AutoCompleteChannel)// writer branch
            {
                branch.Use(next => async context =>
                {
                    try
                    {
                        await next(context);
                    }
                    finally
                    {
                        channel.Writer.Complete();
                    }
                });
            }


            // We now need to split the pipeline into the concurrent readers or writers.
            branch.UseBranchPerInput<int>(branch =>
            {
                branch.UseNewScope(); // intelligent scope control.

                Action<IPipelineBuilder> configureBranch;
                if (isReader) // is reader IS THIS SAGE OR ARE WE CAPTURING STALE VARIABLE
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

                if (isReader)
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

            }).WithInputs(indexes, opt =>
            {
                opt.MaxDegreeOfParallelism = indexes.Count();
            });

        }).WithInputs(readerWriterFork, opt =>
        {
            opt.MaxDegreeOfParallelism = readerWriterFork.Count;
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

