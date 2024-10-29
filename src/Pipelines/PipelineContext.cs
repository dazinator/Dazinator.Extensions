namespace Dazinator.Extensions.Pipelines;

public class PipelineContext
{ 

    private readonly Dictionary<int, Dictionary<Type, object>> _stepState = new();
    internal int CurrentStepIndex { get; set; }

    internal string CurrentStepId { get; set; }

    internal void SetStepState<T>(T state) where T : class
    {
        if (!_stepState.TryGetValue(CurrentStepIndex, out var stepState))
        {
            stepState = new Dictionary<Type, object>();
            _stepState[CurrentStepIndex] = stepState;
        }
        stepState[typeof(T)] = state;
    }

    internal T? GetStepState<T>() where T : class
    {
        if (_stepState.TryGetValue(CurrentStepIndex, out var stepState) &&
            stepState.TryGetValue(typeof(T), out var state))
        {
            return state as T;
        }
        return null;
    }

    internal IStepStateAccessor StepState => new StepStateAccessor(this);

    internal T? GetExtensionState<T>() where T : class
    {
        if (ParentPipeline.ExtensionState.TryGetValue(typeof(T), out var state))
        {
            return state as T;
        }
        return null;
    }

    public PipelineContext(IServiceProvider serviceProvider, CancellationToken cancellationToken, Pipeline parentPipeline)
    {
        ServiceProvider = serviceProvider;
        CancellationToken = cancellationToken;
        ParentPipeline = parentPipeline;
    }

    public IServiceProvider ServiceProvider { get; set; }
    public CancellationToken CancellationToken { get; set; }
    internal Pipeline ParentPipeline { get; }

    // Create a new context with clean options for parallel branches
    internal PipelineContext CreateBranchContext(Pipeline branch)
    {
        return new PipelineContext(ServiceProvider, CancellationToken, branch);
    }

    internal class StepStateAccessor : IStepStateAccessor
    {
        private readonly PipelineContext _context;

        public StepStateAccessor(PipelineContext context)
        {
            _context = context;
        }

        public void Set<T>(T state) where T : class => _context.SetStepState(state);
        public T? Get<T>() where T : class => _context.GetStepState<T>();
    }
}
