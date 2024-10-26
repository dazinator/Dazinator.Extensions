namespace Dazinator.Extensions.DependencyInjection.Tests.ChildServiceProvider
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Threading;
    using Dazinator.AspNet.Extensions.FileProviders;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    public class ConfigurationScenatioTests
    {

        #region Configuration

        [Theory]
        [Description("Tests creating child containers with configuration registered in parent should work")]
        [InlineData("")]
        public void Can_Use_Parent_IConfiguration_In_ChildContainer(params string[] args)
        {

            // Arrange
            var testFileProvider = new InMemoryFileProvider();
            testFileProvider.Directory.AddFile("", new StringFileInfo("{ \"Foo\": true }", "appsettings.json"));


            var services = new ServiceCollection();

            var configBuilder = new ConfigurationBuilder();
            configBuilder.SetBasePath(Directory.GetCurrentDirectory());
            configBuilder.SetFileProvider(testFileProvider);

            configBuilder.AddCommandLine(args);
            configBuilder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            IConfiguration config = configBuilder.Build();
            services.AddSingleton(config);

            var serviceProvider = services.BuildServiceProvider();
            var childServiceProvider = services.CreateChildServiceProvider(serviceProvider, (childServices) =>
             {
                 // don't add any additional child registrations. so IConfiguration is purely in parent scope.
             }, s => s.BuildServiceProvider());


            var configInstance = childServiceProvider.GetRequiredService<IConfiguration>();
            var reloadToken = configInstance.GetReloadToken();


            var waitHandle = new ManualResetEvent(false);
            reloadToken.RegisterChangeCallback((state) =>
            {
                waitHandle.Set();
            }, null);


            var configValue = configInstance["Foo"];
            Assert.Equal("True", configValue);


            // trigger reload token
            testFileProvider.Directory.AddOrUpdateFile("", new StringFileInfo("{ \"Foo\": false }", "appsettings.json"));
            Assert.True(waitHandle.WaitOne(TimeSpan.FromSeconds(5)));
        }


        [Theory]
        [Description("Tests scenarios around IConfiguration in the child container, and adding IConfiguration from the parent container as a configuration source.")]
        [InlineData("")]
        public void Can_Extend_Parent_IConfiguration_In_ChildContainers(params string[] args)
        {
            AssertConfigCanBeUsedFromChildContainer(args);

            // Force multiple GC collections across different generations
            for (int i = 0; i < 3; i++)
            {
                GC.Collect(2, GCCollectionMode.Forced, true, true);
                GC.WaitForPendingFinalizers();
            }

            // Give a small delay to allow for any async operations to complete
            Thread.Sleep(100);

            // One final collection
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Assert.False(ChildConfigWeakReference.IsAlive, "Child configuration was not garbage collected as expected");

        }

        /// <summary>
        /// Verifies the scenario that a new IConfiguration can be built and consumed from the child container:
        ///   - that adds the parent IConfiguration as a source, and
        ///       - overrides some of its values
        ///       - inherits other values unmodified
        ///       - adds in new values just at the child level
        ///   - and when the child container is disposed, and the child IConfiguration goes out of scope, it should be GC'd.
        ///   - and when the parent IConfiguration is modified (reload token fires)
        ///       - the parent is reloaded
        ///       - the child is reloaded
        ///           - the new values inherited from the reloaded parent (unmodified) are available to the child
        ///       - the previous values added at child level are still available to the child.
        /// </summary>
        /// <param name="args"></param>
        private void AssertConfigCanBeUsedFromChildContainer(string[] args)
        {
            // Arrange
            var testFileProvider = new InMemoryFileProvider();
            testFileProvider.Directory.AddFile("/", new StringFileInfo("{ \"Level\": \"Parent\", \"InheritedFromParent\": true }", "appsettings.json"));

            var services = new ServiceCollection();
            IConfiguration parentConfig = null;
            IConfiguration childConfig = null;
            IServiceProvider parentServiceProvider = null;
            IServiceProvider childServiceProvider = null;

            try
            {
                var configBuilder = new ConfigurationBuilder();
                configBuilder.SetBasePath(Directory.GetCurrentDirectory())
                             .SetFileProvider(testFileProvider)
                             .AddCommandLine(args)
                             .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                IConfiguration config = configBuilder.Build();
                services.AddSingleton(config);

                parentServiceProvider = services.BuildServiceProvider();
                childServiceProvider = services.CreateChildServiceProvider(parentServiceProvider, (childServices) =>
                {
                    // inherit but extend the IConfiguration for this child container.
                    // when the child container is disposed, our inhertied IConfiguration should be intact.

                    testFileProvider.Directory.AddFile("/", new StringFileInfo("{ \"Level\": \"Child\", \"ChildOnly\": true  }", "childsettings.json"));

                    var parentIConfig = parentServiceProvider.GetRequiredService<IConfiguration>();
                    IConfiguration childConfig = new ConfigurationBuilder()
                                          .SetFileProvider(testFileProvider)
                                          .AddConfiguration(parentIConfig)
                                          .AddJsonFile("childsettings.json", optional: true, reloadOnChange: true)
                                          .Build();

                    childServices.AddSingleton(childConfig);

                }, s => s.BuildServiceProvider());


                parentConfig = parentServiceProvider.GetRequiredService<IConfiguration>();
                childConfig = childServiceProvider.GetRequiredService<IConfiguration>();

                Assert.Equal("Parent", parentConfig["Level"]);
                Assert.Equal("Child", childConfig["Level"]);

                Assert.Null(parentConfig["ChildOnly"]);
                Assert.Equal("True", childConfig["ChildOnly"]);

                Assert.NotNull(childConfig["InheritedFromParent"]);
                Assert.Equal("True", childConfig["InheritedFromParent"]);


                // now change the parent and should be able to get the newly changed inherited values.
                var parentReloadToken = parentConfig.GetReloadToken();
                var childReloadToken = childConfig.GetReloadToken();

                var waitHandle = new CountdownEvent(2);
                using var registration1 = parentReloadToken.RegisterChangeCallback((state) =>
                {
                    waitHandle.Signal();
                }, null);


                using var registration2 = childReloadToken.RegisterChangeCallback((state) =>
                {
                    waitHandle.Signal();
                }, null);


                // change the parent config file, which should trigger the parent reload token (and the child?)
                testFileProvider.Directory.AddOrUpdateFile("/", new StringFileInfo("{ \"Level\": \"Parent\", \"InheritedFromParent\": \"changed\" }", "appsettings.json"));
                Assert.True(waitHandle.Wait(TimeSpan.FromSeconds(3)));

                Assert.Equal("Parent", parentConfig["Level"]);
                Assert.Equal("Child", childConfig["Level"]);

                Assert.Null(parentConfig["ChildOnly"]);
                Assert.Equal("True", childConfig["ChildOnly"]);

                Assert.NotNull(childConfig["InheritedFromParent"]);
                Assert.Equal("changed", childConfig["InheritedFromParent"]);

                // verify when disposing child containers, parent config still works
                if (childServiceProvider is IDisposable disposable)
                {
                    disposable.Dispose();
                }


                Assert.Equal("Parent", parentConfig["Level"]); // can still use.

                // https://stackoverflow.com/questions/15460362/how-to-tell-if-an-object-has-been-garbage-collected
                // read the comments.
                ChildConfigWeakReference = new WeakReference(childConfig);
            }
            finally
            {
                // Explicit cleanup
                if (childServiceProvider is IDisposable disposable)
                {
                    disposable.Dispose();                   
                }
                childServiceProvider = null;
                if (parentServiceProvider is IDisposable pdisposable)
                {
                    pdisposable.Dispose();                    
                }
                parentServiceProvider = null;

                // Clear references explicitly
                parentConfig = null;
                childConfig = null;
                childServiceProvider = null;
            }

        }


        #endregion

        public WeakReference ChildConfigWeakReference { get; set; }


    }
}
