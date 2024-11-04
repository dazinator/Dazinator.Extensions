namespace Dazinator.Extensions.Pipelines;
using System;
using System.Collections.Generic;
using Dazinator.Extensions.Pipelines.Features.Branching.PerItem;
using Dazinator.Extensions.Pipelines.Features.ProducerConsumer;

public static class ChannelPipelineExtensions
{
    /// <summary>
    /// Splits the execution pipeline first into a branch for readers and a branch for writers. Then splits each reader branch into the number of concurrent readers and the writer branch into the number of concurrent writers.
    /// Allows you to configure the branches used per reader and writer.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="configureConsumerBranch"></param>
    /// <param name="configureProducerBranch"></param>
    /// <param name="configureOptions"></param>
    /// <returns></returns>
    public static IPipelineBuilder UseProducerConsumer<T>(
     this IPipelineBuilder builder,
     Action<ChannelReaderBuilder<T>> configureConsumerBranch,
     Action<ChannelWriterBuilder<T>> configureProducerBranch,
     Action<ChannelPipelineOptions<T>>? configureOptions = null,
     string stepId = null)
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
            var isReader = branch.Input;
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
            var nestedStepId = $"{stepId ?? nameof(BranchPerInputExtensions.UseBranchPerInput)}_{(isReader ? "Reader" : "Writer")}";
            branch.UseBranchPerInput<int>(branch =>
            {
                // branch.UseNewScope(); // intelligent scope control. NAH - Leave it up to the user to manage scopes explicitly - this might break if using deps concurrently but it will make it clearer whats going on.

                Action<IPipelineBuilder> configureBranch;
                if (isReader) // is reader IS THIS SAGE OR ARE WE CAPTURING STALE VARIABLE
                {
                    // var readerBranch = new ItemBranchBuilder<(bool IsReader, int Index)> (branch, branch.Input); // Initial value doesn't matter
                    var readerBranch = new ChannelReaderBuilder<T>(branch, channel.Reader);
                    configureBranch = (builder) => configureConsumerBranch(readerBranch);                  
                    // configureReaderBranch(branch);
                }
                else
                {
                    var writerBranch = new ChannelWriterBuilder<T>(branch, channel.Writer);
                    configureBranch = (builder) => configureProducerBranch(writerBranch);
                }

                configureBranch(branch);

                //if (isReader)
                //{
                //    branch
                //        // .UseNewScope()
                //        .Use(next => async context =>
                //        {
                //            //       // Configure the reader branch once
                //            //       await context.ParentPipeline.RunBranch(
                //            //context,
                //            //configureBranch);

                //            try
                //            {
                //                await foreach (var item in channel.Reader.ReadAllAsync(context.CancellationToken))
                //                {
                //                    //context.SetStepState<T>(item); // Make current item available to downstream middleware
                //                    await next(context); // Execute the rest of the pipeline with this item
                //                }
                //            }
                //            catch (ChannelClosedException)
                //            {
                //                // Channel closed normally, exit gracefully
                //            }
                //        });
                //}
            }, nestedStepId)
              .WithInputs(indexes, opt =>
              {
                opt.MaxDegreeOfParallelism = indexes.Count();
              });

        }, stepId)
        .WithInputs(readerWriterFork, opt =>
        {
            opt.MaxDegreeOfParallelism = readerWriterFork.Count;
        });
    }
}

