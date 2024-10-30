namespace Dazinator.Extensions.Pipelines.Features.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dazinator.Extensions.Pipelines;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
public class ConcurrencyMonitorInspector : IPipelineInspector
{
    private readonly ILogger<ConcurrencyMonitorInspector>? _logger;
    private readonly ConcurrentDictionary<string, int> _activeStepCounts = new();
    private readonly ConcurrentDictionary<string, int> _maxConcurrencyPerStep = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<long, StepExecution>> _stepExecutions = new();
    private long _executionIdCounter;

    public ConcurrencyMonitorInspector(ILogger<ConcurrencyMonitorInspector>? logger = null)
    {
        _logger = logger;
    }

    public Task BeforeStepAsync(PipelineStepContext context)
    {
        var stepId = context.StepId;
        var threadId = Environment.CurrentManagedThreadId;
        var executionId = Interlocked.Increment(ref _executionIdCounter);

        // Track active count for this step
        var currentCount = _activeStepCounts.AddOrUpdate(
            stepId,
            1,
            (_, count) => count + 1);

        // Update max concurrency seen for this step
        _maxConcurrencyPerStep.AddOrUpdate(
            stepId,
            currentCount,
            (_, maxCount) => Math.Max(maxCount, currentCount));

        // Record execution start
        var execution = new StepExecution(
            executionId,
            stepId,
            threadId,
            DateTime.UtcNow,
            null, // EndTime not known yet
            currentCount,
            null); // Final count not known yet

        GetOrCreateExecutionDictionary(stepId)[executionId] = execution;

        _logger?.LogInformation(
            "Step {StepId} starting execution {ExecutionId} on thread {ThreadId}. Current concurrent executions: {Count}",
            stepId,
            executionId,
            threadId,
            currentCount);

        // Store the execution ID in context state for later retrieval
        context.PipelineContext.SetStepState(new ExecutionState(executionId));

        return Task.CompletedTask;
    }

    public Task AfterStepAsync(PipelineStepContext context)
    {
        CompleteExecution(context, null);
        return Task.CompletedTask;
    }

    public Task OnExceptionAsync(PipelineStepContext context)
    {
        CompleteExecution(context, context.Exception);
        return Task.CompletedTask;
    }

    private void CompleteExecution(PipelineStepContext context, Exception? exception)
    {
        var stepId = context.StepId;
        var threadId = Environment.CurrentManagedThreadId;

        // Retrieve the execution ID we stored earlier
        var executionState = context.PipelineContext.GetStepState<ExecutionState>();
        var executionId = executionState.ExecutionId;

        var currentCount = _activeStepCounts.AddOrUpdate(
            stepId,
            0,
            (_, count) => Math.Max(0, count - 1));

        // Update the execution record with end time and final count
        var executions = GetOrCreateExecutionDictionary(stepId);
        if (executions.TryGetValue(executionId, out var execution))
        {
            executions[executionId] = execution with
            {
                EndTime = DateTime.UtcNow,
                FinalConcurrentCount = currentCount
            };
        }

        if (exception != null)
        {
            _logger?.LogError(
                exception,
                "Step {StepId} execution {ExecutionId} failed after {Duration}ms. Remaining concurrent executions: {Count}",
                stepId,
                executionId,
                context.Duration.TotalMilliseconds,
                currentCount);
        }
        else
        {
            _logger?.LogInformation(
                "Step {StepId} execution {ExecutionId} completed after {Duration}ms. Remaining concurrent executions: {Count}",
                stepId,
                executionId,
                context.Duration.TotalMilliseconds,
                currentCount);
        }
    }

    public ConcurrencyReport GenerateReport()
    {
        var report = new ConcurrencyReport();

        foreach (var step in _maxConcurrencyPerStep)
        {
            var executions = GetOrCreateExecutionDictionary(step.Key).Values.ToList();
            var analysis = new StepConcurrencyAnalysis
            {
                StepId = step.Key,
                MaxConcurrentExecutions = step.Value,
                CurrentActiveExecutions = _activeStepCounts.GetValueOrDefault(step.Key),
                TotalExecutions = executions.Count,
                ExecutionTimeline = executions
                    .OrderBy(e => e.StartTime)
                    .Select(e => new ExecutionTimelineEntry(
                        e.ThreadId,
                        e.StartTime,
                        e.EndTime,
                        e.ConcurrentExecutionsAtStart,
                        e.FinalConcurrentCount))
                    .ToList()
            };

            report.StepAnalysis.Add(analysis);
        }

        return report;
    }

    private ConcurrentDictionary<long, StepExecution> GetOrCreateExecutionDictionary(string stepId)
    {
        return _stepExecutions.GetOrAdd(stepId, _ => new ConcurrentDictionary<long, StepExecution>());
    }

    private record StepExecution(
         long ExecutionId,
         string StepId,
         int ThreadId,
         DateTime StartTime,
         DateTime? EndTime,
         int ConcurrentExecutionsAtStart,
         int? FinalConcurrentCount);

    public class ConcurrencyReport
    {
        public List<StepConcurrencyAnalysis> StepAnalysis { get; } = new();

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Concurrency Analysis Report");
            sb.AppendLine("==========================");

            foreach (var analysis in StepAnalysis.OrderBy(a => a.StepId))
            {
                sb.AppendLine($"\nStep: {analysis.StepId}");
                sb.AppendLine($"Max Concurrent Executions: {analysis.MaxConcurrentExecutions}");
                sb.AppendLine($"Current Active Executions: {analysis.CurrentActiveExecutions}");
                sb.AppendLine($"Total Executions: {analysis.TotalExecutions}");

                sb.AppendLine("\nExecution Timeline:");
                foreach (var entry in analysis.ExecutionTimeline)
                {
                    sb.AppendLine(
                        $"  Thread {entry.ThreadId}: " +
                        $"Start={entry.StartTime:HH:mm:ss.fff} " +
                        $"End={entry.EndTime?.ToString("HH:mm:ss.fff") ?? "PENDING"} " +
                        $"Concurrent=(Start: {entry.ConcurrentExecutionsAtStart}, End: {entry.FinalConcurrentCount ?? 0})");
                }
            }

            return sb.ToString();
        }
    }

    public class StepConcurrencyAnalysis
    {
        public string StepId { get; init; } = string.Empty;
        public int MaxConcurrentExecutions { get; init; }
        public int CurrentActiveExecutions { get; init; }
        public int TotalExecutions { get; init; }
        public List<ExecutionTimelineEntry> ExecutionTimeline { get; init; } = new();
    }

    public record ExecutionTimelineEntry(
        int ThreadId,
        DateTime StartTime,
        DateTime? EndTime,
        int ConcurrentExecutionsAtStart,
        int? FinalConcurrentCount);

    private sealed class ExecutionState
    {
        public ExecutionState(long executionId)
        {
            ExecutionId = executionId;
        }

        public long ExecutionId { get; }
    }

}
