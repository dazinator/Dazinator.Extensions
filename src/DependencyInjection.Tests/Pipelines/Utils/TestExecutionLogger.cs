namespace Tests.Pipelines.Utils;
using Xunit.Abstractions;

public class TestExecutionLogger
{

    public TestExecutionLogger(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }
   
    private int _currentId = 0; 

    private readonly List<string> _executionLog = new();
    private readonly ITestOutputHelper _testOutputHelper;  
  

    public int GetNextId()
    {
        return Interlocked.Increment(ref _currentId);
    }

    public void WriteNextIdToLog()
    {
        _executionLog.Add(GetNextId().ToString());       
    }

    public Task WriteNextIdToLogAsync()
    {
        _executionLog.Add(GetNextId().ToString());
        return Task.CompletedTask;
    }

    public Task WriteToLogAsync(string message)
    {
        _executionLog.Add(message);
        return Task.CompletedTask;

    }

    public void WriteToLog(string message)
    {
        _executionLog.Add(message);
    }


    public bool LogContains(string message)
    {
        return _executionLog.Contains(message);
    }

    public void AssertWasLogged(string message)
    {
        Assert.Contains(message, _executionLog);
    }
}
