namespace DependencyInjection.Tests.Modules;

using DependencyInjection.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using Xunit;
using Xunit.Abstractions;
using Xunit.Categories;
using Xunit.Sdk;

[UnitTest]
public class ModuleRegistryTests
{
    private readonly ITestOutputHelper testOutputHelper;

    public ModuleRegistryTests(ITestOutputHelper testOutputHelper)
    {
        this.testOutputHelper = testOutputHelper;
    }

    private ModuleRegistry CreateTestSubject(Dictionary<string, string>? configurationData = null)
    {
        var services = new ServiceCollection();
        var configBuilder = new ConfigurationBuilder();
        if (configurationData is not null)
        {
            configBuilder.AddInMemoryCollection(configurationData!);
        }

        var config = configBuilder.Build();
        return new ModuleRegistry(services, config);
    }

    [Fact]
    public void Register_SimpleModule_ModuleIsRegisteredAndInvoked()
    {
        // Arrange
        var module = Substitute.For<IModule>();
        var sut = CreateTestSubject();

        // Act
        sut.Register(module);
        sut.Build(services => services.BuildServiceProvider());

        // Assert
        module.Received(1).Register(Arg.Any<IModuleRegistry>());
    }

    public class TestOptions
    {
        public string ConfigValue { get; set; }
    }


    private class TestModuleWithOptions : IModule<TestOptions>
    {
        public void Register(IModuleRegistry moduleRegistry, IOptionsMonitor<TestOptions> optionsMonitor) { }
        public string Name { get; set; }
    }

    // Helper class to simulate a concrete module type
    [ModuleOptionsBinding(DefaultConfigurationSectionKey)]
    private class TestModuleWithModuleConfigurationKeyAttribute : IModule<TestOptions>
    {
        public const string DefaultConfigurationSectionKey = "TestModule";
        public void Register(IModuleRegistry moduleRegistry, IOptionsMonitor<TestOptions> optionsMonitor) { }
        public string Name { get; set; }
    }

    // Helper class to simulate a concrete module type
    [ModuleOptionsBinding(DefaultConfigurationSectionKey)]
    private class TestModuleWithTemplatedConfigurationKeyAttribute : IModule<TestOptions>
    {
        public const string OptionsName = "CustomName";
        public const string DefaultConfigurationSectionKey = "TestModule:{OptionsName}";

        public void Register(IModuleRegistry moduleRegistry, IOptionsMonitor<TestOptions> optionsMonitor) { }
        public string Name { get; set; } = OptionsName;
    }

    private class TestModuleWithNamedOptions : IModule<TestOptions>
    {
        public const string OptionsName = "CustomName";
        public void Register(IModuleRegistry moduleRegistry, IOptionsMonitor<TestOptions> optionsMonitor) { }
        public string Name { get; set; } = OptionsName;
    }

    // Test for default options with explicit configuration key
    [Fact]
    public void Register_DefaultOptionsWithExplicitKey_ConfiguresCorrectly()
    {
        // Arrange
        var configKey = "FooModule:ConfigValue";
        var configValue = "TestValue";
        var module = new TestModuleWithOptions();
        var sut = CreateTestSubject(new Dictionary<string, string>
        {
            {
                configKey, configValue
            }
        });

        // Act
        sut.Register(() => module, null, "FooModule");

        sut.Build(services =>
        {
            // We intercept the internal SP, and check it has set the options up correctly.
            var sp = services.BuildServiceProvider();
            var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<TestOptions>>();
            // Assert
            testOutputHelper.WriteLine($"Configuration: {configKey} = {configValue}");
            testOutputHelper.WriteLine($"Actual Value: {optionsMonitor.CurrentValue.ConfigValue}");
            Assert.Equal(configValue, optionsMonitor.CurrentValue.ConfigValue);
            return sp;
        });



    }

    // Test for default options with implicit key using ModuleConfigurationKey attribute
    [Fact]
    public void Register_DefaultOptionsWithImplicitKey_ConfiguresCorrectly()
    {
        // Arrange
        var configKey = $"{TestModuleWithModuleConfigurationKeyAttribute.DefaultConfigurationSectionKey}:ConfigValue";
        var configValue = "TestValue";
        var module = new TestModuleWithModuleConfigurationKeyAttribute();
        var sut = CreateTestSubject(new Dictionary<string, string>
        {
            {
                configKey, configValue
            }
        });

        // Act
        sut.Register(() => module);
        sut.Build(services =>
        {
            // We intercept the internal SP, and check it has set the options up correctly.
            var sp = services.BuildServiceProvider();
            var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<TestOptions>>();
            // Assert
            testOutputHelper.WriteLine($"Configuration: {configKey} = {configValue}");
            testOutputHelper.WriteLine($"Actual Value: {optionsMonitor.CurrentValue.ConfigValue}");
            Assert.Equal(configValue, optionsMonitor.CurrentValue.ConfigValue);
            return sp;
        });

    }

    // Test for default options where explicit key overrides ModuleConfigurationKey attribute
    [Fact]
    public void Register_DefaultOptionsExplicitKeyOverridesAttribute_ConfiguresCorrectly()
    {
        // Arrange
        var configKey = "Overriden:ConfigValue";
        var configValue = "TestValue";
        var module = new TestModuleWithModuleConfigurationKeyAttribute();
        var sut = CreateTestSubject(new Dictionary<string, string>
        {
            {
                configKey, configValue
            }
        });

        // Act
        sut.Register(() => module, null, "Overriden");
        sut.Build(services =>
        {
            // We intercept the internal SP, and check it has set the options up correctly.
            var sp = services.BuildServiceProvider();
            var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<TestOptions>>();
            // Assert
            testOutputHelper.WriteLine($"Configuration: {configKey} = {configValue}");
            testOutputHelper.WriteLine($"Actual Value: {optionsMonitor.CurrentValue.ConfigValue}");
            Assert.Equal(configValue, optionsMonitor.CurrentValue.ConfigValue);
            return sp;
        });
    }

    // Test for named options with explicit configuration key and options name replacement
    [Fact]
    public void Register_NamedOptionsWithExplicitKeyAndReplacement_ConfiguresCorrectly()
    {
        // Arrange
        var configKey = $"TestModule:{TestModuleWithNamedOptions.OptionsName}:ConfigValue";
        var configValue = "TestValue";
        var module = new TestModuleWithNamedOptions();
        var sut = CreateTestSubject(new Dictionary<string, string>
        {
            {
                configKey, configValue
            }
        });

        // Act
        sut.Register(() => module, null, "TestModule:{OptionsName}", TestModuleWithNamedOptions.OptionsName);
        sut.Build(services =>
        {
            // We intercept the internal SP, and check it has set the options up correctly.
            var sp = services.BuildServiceProvider();
            var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<TestOptions>>();
            // Assert
            Assert.Equal(configValue, optionsMonitor.Get(TestModuleWithNamedOptions.OptionsName).ConfigValue);

            testOutputHelper.WriteLine($"Configuration: {configKey} = {configValue}");
            testOutputHelper.WriteLine($"Actual Value: {optionsMonitor.Get(TestModuleWithNamedOptions.OptionsName).ConfigValue}");
            return sp;
        });
    }

    // Test for named options with implicit key using ModuleConfigurationKey attribute
    [Fact]
    public void Register_NamedOptionsWithImplicitKey_ConfiguresCorrectly()
    {
        // Arrange
        var configKey = $"TestModule:{TestModuleWithTemplatedConfigurationKeyAttribute.OptionsName}:ConfigValue";
        var configValue = "TestValue";
        var module = new TestModuleWithTemplatedConfigurationKeyAttribute();
        var sut = CreateTestSubject(new Dictionary<string, string>
        {
            {
                configKey, configValue
            }
        });

        // Act
        sut.Register(() => module, moduleName: TestModuleWithTemplatedConfigurationKeyAttribute.OptionsName);
        sut.Build(services =>
        {
            // We intercept the internal SP, and check it has set the options up correctly.
            var sp = services.BuildServiceProvider();
            var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<TestOptions>>();
            // Assert
            Assert.Equal(configValue, optionsMonitor.Get(TestModuleWithTemplatedConfigurationKeyAttribute.OptionsName).ConfigValue);

            testOutputHelper.WriteLine($"Configuration: {configKey} = {configValue}");
            testOutputHelper.WriteLine($"Actual Value: {optionsMonitor.Get(TestModuleWithTemplatedConfigurationKeyAttribute.OptionsName).ConfigValue}");
            return sp;
        });
    }

    // Test for named options with explicit override key
    [Fact]
    public void Register_NamedOptionsWithExplicitOverrideKey_ConfiguresCorrectly()
    {
        // Arrange
        var configKey = "OverrideKey:ConfigValue";
        var configValue = "OverrideValue";
        var module = new TestModuleWithNamedOptions();
        var sut = CreateTestSubject(new Dictionary<string, string>
        {
            {
                configKey, configValue
            }
        });

        // Act
        sut.Register(() => module, null, "OverrideKey", TestModuleWithNamedOptions.OptionsName);
        sut.Build(services =>
        {
            // We intercept the internal SP, and check it has set the options up correctly.
            var sp = services.BuildServiceProvider();
            var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<TestOptions>>();
            // Assert
            Assert.Equal(configValue, optionsMonitor.Get(TestModuleWithNamedOptions.OptionsName).ConfigValue);

            testOutputHelper.WriteLine($"Configuration: {configKey} = {configValue}");
            testOutputHelper.WriteLine($"Actual Value: {optionsMonitor.Get(TestModuleWithNamedOptions.OptionsName).ConfigValue}");
            return sp;
        });
    }

    [Fact]
    public void Build_InvokesAllRegisteredModules()
    {
        // Arrange
        var module1 = Substitute.For<IModule>();
        var module2 = Substitute.For<IModule>();
        var sut = CreateTestSubject();
        sut.Register(module1);
        sut.Register(module2);

        // Act
        sut.Build(services => services.BuildServiceProvider());

        // Assert
        module1.Received(1).Register(Arg.Any<IModuleRegistry>());
        module2.Received(1).Register(Arg.Any<IModuleRegistry>());
    }

    [Fact]
    public void Build_CreatesNestedRegistryForModules()
    {
        // Arrange
        var outerModule = Substitute.For<IModule>();
        var innerModule = Substitute.For<IModule>();
        var sut = CreateTestSubject();

        outerModule.When(x => x.Register(Arg.Any<IModuleRegistry>()))
            .Do(x => ((IModuleRegistry)x[0]).Register(innerModule));

        sut.Register(outerModule);

        // Act
        sut.Build(services => services.BuildServiceProvider());

        // Assert
        outerModule.Received(1).Register(Arg.Any<IModuleRegistry>());
        innerModule.Received(1).Register(Arg.Any<IModuleRegistry>());
    }

    [Fact]
    public void Build_CreatesMultipleLevelsOfNestedRegistries()
    {
        // Arrange
        var outerModule = Substitute.For<IModule>();
        var middleModule = Substitute.For<IModule>();
        var innerModule = Substitute.For<IModule>();
        var sut = CreateTestSubject();

        outerModule.When(x => x.Register(Arg.Any<IModuleRegistry>()))
            .Do(x => ((IModuleRegistry)x[0]).Register(middleModule));
        middleModule.When(x => x.Register(Arg.Any<IModuleRegistry>()))
            .Do(x => ((IModuleRegistry)x[0]).Register(innerModule));

        sut.Register(outerModule);

        // Act
        sut.Build(services => services.BuildServiceProvider());

        // Assert
        outerModule.Received(1).Register(Arg.Any<IModuleRegistry>());
        middleModule.Received(1).Register(Arg.Any<IModuleRegistry>());
        innerModule.Received(1).Register(Arg.Any<IModuleRegistry>());
    }

    [Fact]
    public void Build_NestedModulesWithOptions_ConfiguresCorrectly()
    {
        // Arrange
        var outerModule = Substitute.For<IModule<TestOptions>>();
        var innerModule = Substitute.For<IModule<TestOptions>>();
        var configData = new Dictionary<string, string>
        {
            {
                "OuterModule:ConfigValue", "OuterValue"
            },
            {
                "InnerModule:ConfigValue", "InnerValue"
            }
        };
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        var sut = new ModuleRegistry(services, configuration);

        // outerModule.GetOptionsName().Returns("OuterModule");
        // innerModule.GetOptionsName().Returns("InnerModule");

        var outerOptionsCorrect = false;
        var innerOptionsCorrect = false;

        outerModule.When(x =>
                x.Register(Arg.Any<IModuleRegistry>(), Arg.Any<IOptionsMonitor<TestOptions>>()))
            .Do(x =>
            {
                var optionsMonitor = x.Arg<IOptionsMonitor<TestOptions>>();
                var options = optionsMonitor.Get("OuterModule");
                outerOptionsCorrect = options.ConfigValue == "OuterValue";
                testOutputHelper.WriteLine($"Outer module options: {options.ConfigValue}");

                // Register inner module
                ((IModuleRegistry)x[0]).Register(() => innerModule, null, "InnerModule", "InnerModule");
            });

        innerModule.When(x => x.Register(Arg.Any<IModuleRegistry>(), Arg.Any<IOptionsMonitor<TestOptions>>()))
            .Do(x =>
            {
                var optionsMonitor = x.Arg<IOptionsMonitor<TestOptions>>();
                var options = optionsMonitor.Get("InnerModule");
                innerOptionsCorrect = options.ConfigValue == "InnerValue";
                testOutputHelper.WriteLine($"Inner module options: {options.ConfigValue}");
            });

        // Act
        sut.Register(() => outerModule, null, "OuterModule", "OuterModule");
        sut.Build(s => s.BuildServiceProvider());

        // Assert
        Assert.True(outerOptionsCorrect, "Outer module options were not configured correctly");
        Assert.True(innerOptionsCorrect, "Inner module options were not configured correctly");

        outerModule.Received().Name = Arg.Is("OuterModule");
        innerModule.Received().Name = Arg.Is("InnerModule");
        outerModule.Received(1).Register(Arg.Any<IModuleRegistry>(), Arg.Any<IOptionsMonitor<TestOptions>>());
        innerModule.Received(1).Register(Arg.Any<IModuleRegistry>(), Arg.Any<IOptionsMonitor<TestOptions>>());
    }

    [Fact]
    public void Build_NestedModulesWithOverlappingKeys_UsesCorrectConfiguration()
    {
        // Arrange
        var outerModule = Substitute.For<IModule<TestOptions>>();
        var innerModule = Substitute.For<IModule<TestOptions>>();
        var sut = CreateTestSubject(new Dictionary<string, string>
        {
            {
                "Module:Outer:ConfigValue", "OuterValue"
            },
            {
                "Module:Inner:ConfigValue", "InnerValue"
            }
        });

        // outerModule.GetOptionsName().Returns("Outer");
        // innerModule.GetOptionsName().Returns("Inner");

        outerModule.When(x => x.Register(Arg.Any<IModuleRegistry>(), Arg.Any<IOptionsMonitor<TestOptions>>()))
            .Do(x => ((IModuleRegistry)x[0]).Register(() => innerModule, null, "Module:{OptionsName}", moduleName: "Inner"));

        sut.Register(() => outerModule, null, "Module:{OptionsName}", "Outer");

        // Act
        sut.Build(services => services.BuildServiceProvider());

        // Assert
        outerModule.Received(1).Register(Arg.Any<IModuleRegistry>(), Arg.Is<IOptionsMonitor<TestOptions>>(
            om => om.Get("Outer").ConfigValue == "OuterValue"));
        innerModule.Received(1).Register(Arg.Any<IModuleRegistry>(), Arg.Is<IOptionsMonitor<TestOptions>>(
            om => om.Get("Inner").ConfigValue == "InnerValue"));

        outerModule.Received().Name = Arg.Is("Outer");
        innerModule.Received().Name = Arg.Is("Inner");
    }

    [Fact]
    public void Build_ModuleRegisteringMultipleNestedModules_AllNestedModulesAreRegistered()
    {
        // Arrange
        var outerModule = Substitute.For<IModule>();
        var innerModule1 = Substitute.For<IModule>();
        var innerModule2 = Substitute.For<IModule>();
        var sut = CreateTestSubject();

        outerModule.When(x => x.Register(Arg.Any<IModuleRegistry>()))
            .Do(x =>
            {
                var registry = (IModuleRegistry)x[0];
                registry.Register(innerModule1);
                registry.Register(innerModule2);
            });

        sut.Register(outerModule);

        // Act
        sut.Build(services => services.BuildServiceProvider());

        // Assert
        outerModule.Received(1).Register(Arg.Any<IModuleRegistry>());
        innerModule1.Received(1).Register(Arg.Any<IModuleRegistry>());
        innerModule2.Received(1).Register(Arg.Any<IModuleRegistry>());
    }

    [Fact(Skip = "Stack overflow needs guarding against")]
    public void Build_CircularDependencyBetweenModules_ThrowsException()
    {
        // Arrange
        var moduleA = Substitute.For<IModule>();
        var moduleB = Substitute.For<IModule>();
        var sut = CreateTestSubject();

        moduleA.When(x => x.Register(Arg.Any<IModuleRegistry>()))
            .Do(x => ((IModuleRegistry)x[0]).Register(moduleB));
        moduleB.When(x => x.Register(Arg.Any<IModuleRegistry>()))
            .Do(x => ((IModuleRegistry)x[0]).Register(moduleA));

        sut.Register(moduleA);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => sut.Build(services => services.BuildServiceProvider()));
    }

    public interface ITestService
    {
    }

    public class TestService : ITestService
    {
    }

    public class TestModuleWithDependency : IModule
    {
        public ITestService TestService { get; }

        public TestModuleWithDependency(ITestService testService) => TestService = testService;

        public void Register(IModuleRegistry registry) => registry.Services.AddSingleton(this);
        public string Name { get; set; }
    }

    [Fact]
    public void Build_ModuleWithConstructorInjection_ReceivesDependencies()
    {
        // Arrange
        var sut = CreateTestSubject(null);

        // Act
        sut.Register<TestModuleWithDependency>();
        sut.Build(s =>
        {
            // Include services that can be injected into modules.
            // These aren't registered with the application services unless a module registers once injected.
            s.AddSingleton<ITestService, TestService>();
            return s.BuildServiceProvider();
        });

        // Assert
        var serviceProvider = sut.Services.BuildServiceProvider();
        var testModule = serviceProvider.GetRequiredService<TestModuleWithDependency>();
        Assert.NotNull(testModule.TestService);
    }
}
