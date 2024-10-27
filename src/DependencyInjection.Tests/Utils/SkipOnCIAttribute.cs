namespace DependencyInjection.Tests.Utils
{
    using System;
    using Xunit;

    // First create this attribute
    public sealed class SkipOnCIAttribute : FactAttribute
    {
        public SkipOnCIAttribute()
        {
            if (Environment.GetEnvironmentVariable("CI") != null ||
                Environment.GetEnvironmentVariable("TF_BUILD") != null ||  // Azure DevOps specific
                Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != null) // GitHub Actions
            {
                Skip = "Test skipped on CI environment";
            }
        }
    }
}
