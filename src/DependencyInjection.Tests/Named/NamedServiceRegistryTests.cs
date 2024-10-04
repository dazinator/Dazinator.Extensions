namespace Dazinator.Extensions.DependencyInjection.Tests.Named
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Dazinator.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection;  
    using Xunit;
    using ServiceCollection = ServiceCollection;

    public class NamedServiceRegistryTests
    {

        #region Singleton
        [Fact]
        public void Can_Add_Singleton_Instance_Registrations()
        {
            var namedRegistrations = new NamedServiceRegistry<AnimalService>();
            namedRegistrations.AddSingleton("A", new AnimalService() { SomeProperty = "A" });
            namedRegistrations.AddSingleton("B", new AnimalService() { SomeProperty = "B" });
            namedRegistrations.AddSingleton("C", new BearService() { SomeProperty = "C" });
            namedRegistrations.AddSingleton("D", new LionService() { SomeProperty = "D", SomeOtherProperty = true });
            namedRegistrations.AddSingleton("E", new LionService() { SomeProperty = "E" }, registrationOwnsInstance: true);
            namedRegistrations.AddSingleton(new AnimalService());

            var registeredA = namedRegistrations["A"];
            var registeredB = namedRegistrations["B"];

            Assert.NotNull(registeredA);
            Assert.Equal(ServiceLifetime.Singleton, registeredA.Lifetime);
            Assert.False(registeredA.RegistrationOwnsInstance);
            Assert.Null(registeredA.ImplementationType);
            Assert.NotNull(registeredA.InstanceFactory);

            Assert.NotNull(registeredB);
            Assert.Equal(ServiceLifetime.Singleton, registeredB.Lifetime);
            Assert.False(registeredB.RegistrationOwnsInstance);
            Assert.Null(registeredB.ImplementationType);
            Assert.NotNull(registeredB.InstanceFactory);

            var registeredC = namedRegistrations["C"];
            var registeredD = namedRegistrations["D"];

            Assert.NotNull(registeredC);
            Assert.Equal(ServiceLifetime.Singleton, registeredC.Lifetime);
            Assert.False(registeredC.RegistrationOwnsInstance);
            Assert.Null(registeredC.ImplementationType);
            Assert.NotNull(registeredC.InstanceFactory);

            Assert.NotNull(registeredD);
            Assert.Equal(ServiceLifetime.Singleton, registeredD.Lifetime);
            Assert.False(registeredD.RegistrationOwnsInstance);
            Assert.Null(registeredD.ImplementationType);
            Assert.NotNull(registeredD.InstanceFactory);

            var registeredE = namedRegistrations["E"];
            Assert.True(registeredE.RegistrationOwnsInstance);
            Assert.Equal(ServiceLifetime.Singleton, registeredE.Lifetime);
            Assert.Null(registeredE.ImplementationType);
            Assert.NotNull(registeredE.InstanceFactory);

            var registeredDefault = namedRegistrations[string.Empty];
            Assert.False(registeredDefault.RegistrationOwnsInstance);
            Assert.Equal(ServiceLifetime.Singleton, registeredDefault.Lifetime);
            Assert.Null(registeredDefault.ImplementationType);
            Assert.NotNull(registeredDefault.InstanceFactory);

            // deault registration should be resolvable via service provider also.

        }

        [Fact]
        public void Can_Add_Singleton_Type_Registrations()
        {
            var namedRegistrations = new NamedServiceRegistry<AnimalService>();
            namedRegistrations.AddSingleton("A");
            namedRegistrations.AddSingleton<BearService>("B");
            namedRegistrations.Add(ServiceLifetime.Singleton, "C");
            namedRegistrations.Add<BearService>(ServiceLifetime.Singleton, "D");
            namedRegistrations.Add(ServiceLifetime.Singleton);

            AssertRegistration<AnimalService, AnimalService>(ServiceLifetime.Singleton, namedRegistrations, "A");
            AssertRegistration<AnimalService, BearService>(ServiceLifetime.Singleton, namedRegistrations, "B");
            AssertRegistration<AnimalService, AnimalService>(ServiceLifetime.Singleton, namedRegistrations, "C");
            AssertRegistration<AnimalService, BearService>(ServiceLifetime.Singleton, namedRegistrations, "D");
            AssertRegistration<AnimalService, AnimalService>(ServiceLifetime.Singleton, namedRegistrations, "");

        }

        [Fact]
        public void Can_Add_Singleton_FactoryFunc_Registrations()
        {
            var namedRegistrations = new NamedServiceRegistry<AnimalService>();
            namedRegistrations.AddSingleton("A", sp => new AnimalService());
            namedRegistrations.AddSingleton("B", sp => new BearService());
            namedRegistrations.Add(ServiceLifetime.Singleton, "C", (sp) => new AnimalService());
            namedRegistrations.Add<BearService>(ServiceLifetime.Singleton, "D", (sp) => new BearService());
            namedRegistrations.Add(ServiceLifetime.Singleton, (sp) => new AnimalService());

            AssertRegistration<AnimalService, AnimalService>(ServiceLifetime.Singleton, namedRegistrations, "A", true);
            AssertRegistration<AnimalService, BearService>(ServiceLifetime.Singleton, namedRegistrations, "B", true);
            AssertRegistration<AnimalService, AnimalService>(ServiceLifetime.Singleton, namedRegistrations, "C", true);
            AssertRegistration<AnimalService, BearService>(ServiceLifetime.Singleton, namedRegistrations, "D", true);
            AssertRegistration<AnimalService, AnimalService>(ServiceLifetime.Singleton, namedRegistrations, "", true);

        }

        [Fact]
        public void Can_Add_MixtureOf_Registrations()
        {
            var namedRegistrations = new NamedServiceRegistry<AnimalService>();
            namedRegistrations.Add(ServiceLifetime.Singleton); // same as .AddSingleton(string.Empty); 
            namedRegistrations.AddSingleton("A", new AnimalService() { SomeProperty = "A" });
            namedRegistrations.AddSingleton("B", new LionService() { SomeProperty = "B", SomeOtherProperty = true });
            namedRegistrations.AddSingleton<TigerService>("C");
            namedRegistrations.AddSingleton("D");
            namedRegistrations.AddSingleton("E", sp => new BearService());

            Assert.NotNull(namedRegistrations["A"]);
            Assert.NotNull(namedRegistrations["B"]);
            Assert.NotNull(namedRegistrations["C"]);
            Assert.NotNull(namedRegistrations["D"]);
            Assert.NotNull(namedRegistrations["E"]);

            Assert.NotNull(namedRegistrations[""]);
        }

        [Fact]
        public void Disposes_Of_Registrations_OnDispose()
        {
            var disposable = new DisposableTigerService() { SomeProperty = "Should be disposed." };
            var disposableShouldNotBeDisposed = new DisposableTigerService() { SomeProperty = "Should NOT be disposed." };
            var anotherDisposable = new DisposableTigerService() { SomeProperty = "Should be disposed." };

            using (var namedRegistrations = new NamedServiceRegistry<AnimalService>())
            {
                namedRegistrations.AddSingleton("A", disposableShouldNotBeDisposed);
                namedRegistrations.AddSingleton("B", disposable, registrationOwnsInstance: true); // This param defaults to false, must specifiy true if you want instance to be disposed by service provider dispose().
                namedRegistrations.AddSingleton(anotherDisposable, registrationOwnsInstance: true);
            }

            Assert.False(disposableShouldNotBeDisposed.WasDisposed);
            Assert.True(disposable.WasDisposed);
            Assert.True(anotherDisposable.WasDisposed);
        }

        [Fact]
        public void Owned_Singleton_Instances_Are_Disposed_When_ServiceProvider_Is_Disposed()
        {
            var notOwnedDisposable = new DisposableTigerService() { SomeProperty = "A" };
            var ownedDisposable = new DisposableTigerService() { SomeProperty = "B" };

            DisposableTigerService ownedDisposableFromFactoryFunc = null;
            DisposableTigerService ownedDisposableFromNamelessFactoryFunc = null;


            var services = new ServiceCollection();
            services.AddNamed<AnimalService>(registry =>
            {
                registry.AddSingleton("A", notOwnedDisposable);
                registry.AddSingleton("B", ownedDisposable, registrationOwnsInstance: true);
                registry.AddSingleton("C", (sp) =>
                {
                    ownedDisposableFromFactoryFunc = new DisposableTigerService();
                    return ownedDisposableFromFactoryFunc;
                });
                registry.AddSingleton((sp) =>
                {
                    ownedDisposableFromNamelessFactoryFunc = new DisposableTigerService();
                    return ownedDisposableFromNamelessFactoryFunc;
                }); // nameless
            });

            var sp = services.BuildServiceProvider();
            // before the container will dispose the registry (and thus the instances registered with the registry)
            // it must have first resolved an instance of it atleast once.
            var registry = sp.GetRequiredService<NamedServiceRegistry<AnimalService>>();
            var namelessInstanceRegistration = registry[""];
            var namelessInstance = namelessInstanceRegistration.InstanceFactory(sp);
            sp.Dispose();

            Assert.False(notOwnedDisposable.WasDisposed);
            Assert.True(ownedDisposable.WasDisposed);
            Assert.Null(ownedDisposableFromFactoryFunc); // As this wasn't resolved yet it shouldnt have been created, so obvioulsy not disposed either.
            Assert.True(ownedDisposableFromNamelessFactoryFunc.WasDisposed); // as this was resolved, an instance was created, therefore it should be disposed.

        }

        [Fact]
        public void Adding_A_Nameless_Singleton_Populates_ServiceCollection()
        {

            var instance = new AnimalService();

            // instance added with no name
            AssertServiceDescriptor<AnimalService>((namedRegistrations) => namedRegistrations.AddSingleton(instance), (descriptor) =>
            {
                Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
                Assert.Null(descriptor.ImplementationType);
                Assert.Equal(descriptor.ImplementationInstance, instance);
                Assert.Equal(typeof(AnimalService), descriptor.ServiceType);
                Assert.Null(descriptor.ImplementationFactory);
            });

            // instance added with empty name
            AssertServiceDescriptor<AnimalService>((namedRegistrations) => namedRegistrations.AddSingleton("", instance), (descriptor) =>
            {
                Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
                Assert.Null(descriptor.ImplementationType);
                Assert.Equal(descriptor.ImplementationInstance, instance);
                Assert.Equal(typeof(AnimalService), descriptor.ServiceType);
                Assert.Null(descriptor.ImplementationFactory);
            });

            // type added with no name
            AssertServiceDescriptor<AnimalService>((namedRegistrations) => namedRegistrations.AddSingleton<AnimalService>(), (descriptor) =>
            {
                Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
                Assert.NotNull(descriptor.ImplementationType);
                Assert.Null(descriptor.ImplementationInstance);
                Assert.Equal(typeof(AnimalService), descriptor.ServiceType);
                Assert.Null(descriptor.ImplementationFactory);
            });

            // type added with empty name
            AssertServiceDescriptor<AnimalService>((namedRegistrations) => namedRegistrations.AddSingleton<AnimalService>(""), (descriptor) =>
            {
                Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
                Assert.NotNull(descriptor.ImplementationType);
                Assert.Null(descriptor.ImplementationInstance);
                Assert.Equal(typeof(AnimalService), descriptor.ServiceType);
                Assert.Null(descriptor.ImplementationFactory);
            });


            // func added with no name
            AssertServiceDescriptor<AnimalService>((namedRegistrations) => namedRegistrations.AddSingleton<AnimalService>((s) => new AnimalService()), (descriptor) =>
              {
                  Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
                  Assert.Null(descriptor.ImplementationType);
                  Assert.Null(descriptor.ImplementationInstance);
                  Assert.Equal(typeof(AnimalService), descriptor.ServiceType);
                  Assert.NotNull(descriptor.ImplementationFactory);
              });

            // func added with empty name
            AssertServiceDescriptor<AnimalService>((namedRegistrations) => namedRegistrations.AddSingleton<AnimalService>("", (s) => new AnimalService()), (descriptor) =>
            {
                Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
                Assert.Null(descriptor.ImplementationType);
                Assert.Null(descriptor.ImplementationInstance);
                Assert.Equal(typeof(AnimalService), descriptor.ServiceType);
                Assert.NotNull(descriptor.ImplementationFactory);
            });

        }


        #endregion

        #region Transient

        [Fact]
        public void Can_Add_Transient_Type_Registrations()
        {
            var namedRegistrations = new NamedServiceRegistry<AnimalService>();
            namedRegistrations.AddTransient("A");
            namedRegistrations.AddTransient<BearService>("B");
            namedRegistrations.Add(ServiceLifetime.Transient, "C");
            namedRegistrations.Add<BearService>(ServiceLifetime.Transient, "D");
            namedRegistrations.Add(ServiceLifetime.Transient);

            AssertRegistration<AnimalService, AnimalService>(ServiceLifetime.Transient, namedRegistrations, "A");
            AssertRegistration<AnimalService, BearService>(ServiceLifetime.Transient, namedRegistrations, "B");
            AssertRegistration<AnimalService, AnimalService>(ServiceLifetime.Transient, namedRegistrations, "C");
            AssertRegistration<AnimalService, BearService>(ServiceLifetime.Transient, namedRegistrations, "D");
            AssertRegistration<AnimalService, AnimalService>(ServiceLifetime.Transient, namedRegistrations, "");

        }

        [Fact]
        public void Can_Add_Transient_FactoryFunc_Registrations()
        {
            var namedRegistrations = new NamedServiceRegistry<AnimalService>();
            namedRegistrations.AddTransient("A", (sp) => new AnimalService());
            namedRegistrations.AddTransient("B", (sp) => new BearService());
            namedRegistrations.Add(ServiceLifetime.Transient, "C", (sp) => new AnimalService());
            namedRegistrations.Add<BearService>(ServiceLifetime.Transient, "D", (sp) => new BearService());
            namedRegistrations.Add(ServiceLifetime.Transient, (sp) => new AnimalService());

            AssertRegistration<AnimalService, AnimalService>(ServiceLifetime.Transient, namedRegistrations, "A", true);
            AssertRegistration<AnimalService, BearService>(ServiceLifetime.Transient, namedRegistrations, "B", true);
            AssertRegistration<AnimalService, AnimalService>(ServiceLifetime.Transient, namedRegistrations, "C", true);
            AssertRegistration<AnimalService, BearService>(ServiceLifetime.Transient, namedRegistrations, "D", true);
            AssertRegistration<AnimalService, AnimalService>(ServiceLifetime.Transient, namedRegistrations, "", true);


        }

        [Fact]
        public void Adding_A_Nameless_Transient_Populates_ServiceCollection()
        {

            var instance = new AnimalService();

            // type added with no name
            AssertServiceDescriptor<AnimalService>((namedRegistrations) => namedRegistrations.AddTransient<AnimalService>(), (descriptor) =>
            {
                Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
                Assert.NotNull(descriptor.ImplementationType);
                Assert.Null(descriptor.ImplementationInstance);
                Assert.Equal(typeof(AnimalService), descriptor.ServiceType);
                Assert.Null(descriptor.ImplementationFactory);
            });

            // type added with empty name
            AssertServiceDescriptor<AnimalService>((namedRegistrations) => namedRegistrations.AddTransient<AnimalService>(""), (descriptor) =>
            {
                Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
                Assert.NotNull(descriptor.ImplementationType);
                Assert.Null(descriptor.ImplementationInstance);
                Assert.Equal(typeof(AnimalService), descriptor.ServiceType);
                Assert.Null(descriptor.ImplementationFactory);
            });


            // func added with no name
            AssertServiceDescriptor<AnimalService>((namedRegistrations) => namedRegistrations.AddTransient<AnimalService>((s) => new AnimalService()), (descriptor) =>
            {
                Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
                Assert.Null(descriptor.ImplementationType);
                Assert.Null(descriptor.ImplementationInstance);
                Assert.Equal(typeof(AnimalService), descriptor.ServiceType);
                Assert.NotNull(descriptor.ImplementationFactory);
            });

            // func added with empty name
            AssertServiceDescriptor<AnimalService>((namedRegistrations) => namedRegistrations.AddTransient<AnimalService>("", (s) => new AnimalService()), (descriptor) =>
            {
                Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
                Assert.Null(descriptor.ImplementationType);
                Assert.Null(descriptor.ImplementationInstance);
                Assert.Equal(typeof(AnimalService), descriptor.ServiceType);
                Assert.NotNull(descriptor.ImplementationFactory);
            });

        }


        #endregion

        #region Scoped

        [Fact]
        public void Can_Add_Scoped_Type_Registrations()
        {
            var namedRegistrations = new NamedServiceRegistry<AnimalService>();
            namedRegistrations.AddScoped("A");
            namedRegistrations.AddScoped<BearService>("B");
            namedRegistrations.Add(ServiceLifetime.Scoped, "C");
            namedRegistrations.Add<BearService>(ServiceLifetime.Scoped, "D");
            namedRegistrations.Add(ServiceLifetime.Scoped);

            AssertRegistration<AnimalService, AnimalService>(ServiceLifetime.Scoped, namedRegistrations, "A");
            AssertRegistration<AnimalService, BearService>(ServiceLifetime.Scoped, namedRegistrations, "B");
            AssertRegistration<AnimalService, AnimalService>(ServiceLifetime.Scoped, namedRegistrations, "C");
            AssertRegistration<AnimalService, BearService>(ServiceLifetime.Scoped, namedRegistrations, "D");
            AssertRegistration<AnimalService, AnimalService>(ServiceLifetime.Scoped, namedRegistrations, "");
        }

        [Fact]
        public void Can_Add_Scoped_FactoryFunc_Registrations()
        {
            var namedRegistrations = new NamedServiceRegistry<AnimalService>();
            namedRegistrations.AddScoped("A", sp => new AnimalService());
            namedRegistrations.AddScoped("B", sp => new BearService());
            namedRegistrations.Add(ServiceLifetime.Scoped, "C", (sp) => new AnimalService());
            namedRegistrations.Add<BearService>(ServiceLifetime.Scoped, "D", (sp) => new BearService());
            namedRegistrations.Add(ServiceLifetime.Scoped, (sp) => new AnimalService());

            AssertRegistration<AnimalService, AnimalService>(ServiceLifetime.Scoped, namedRegistrations, "A", true);
            AssertRegistration<AnimalService, BearService>(ServiceLifetime.Scoped, namedRegistrations, "B", true);
            AssertRegistration<AnimalService, AnimalService>(ServiceLifetime.Scoped, namedRegistrations, "C", true);
            AssertRegistration<AnimalService, BearService>(ServiceLifetime.Scoped, namedRegistrations, "D", true);
            AssertRegistration<AnimalService, AnimalService>(ServiceLifetime.Scoped, namedRegistrations, "", true);

        }

        [Fact]
        public void Adding_A_Nameless_Scoped_Populates_ServiceCollection()
        {

            var instance = new AnimalService();

            // type added with no name
            AssertServiceDescriptor<AnimalService>((namedRegistrations) => namedRegistrations.AddScoped<AnimalService>(), (descriptor) =>
            {
                Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
                Assert.NotNull(descriptor.ImplementationType);
                Assert.Null(descriptor.ImplementationInstance);
                Assert.Equal(typeof(AnimalService), descriptor.ServiceType);
                Assert.Null(descriptor.ImplementationFactory);
            });

            // type added with empty name
            AssertServiceDescriptor<AnimalService>((namedRegistrations) => namedRegistrations.AddScoped<AnimalService>(""), (descriptor) =>
            {
                Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
                Assert.NotNull(descriptor.ImplementationType);
                Assert.Null(descriptor.ImplementationInstance);
                Assert.Equal(typeof(AnimalService), descriptor.ServiceType);
                Assert.Null(descriptor.ImplementationFactory);
            });


            // func added with no name
            AssertServiceDescriptor<AnimalService>((namedRegistrations) => namedRegistrations.AddScoped<AnimalService>((s) => new AnimalService()), (descriptor) =>
            {
                Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
                Assert.Null(descriptor.ImplementationType);
                Assert.Null(descriptor.ImplementationInstance);
                Assert.Equal(typeof(AnimalService), descriptor.ServiceType);
                Assert.NotNull(descriptor.ImplementationFactory);
            });

            // func added with empty name
            AssertServiceDescriptor<AnimalService>((namedRegistrations) => namedRegistrations.AddScoped<AnimalService>("", (s) => new AnimalService()), (descriptor) =>
            {
                Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
                Assert.Null(descriptor.ImplementationType);
                Assert.Null(descriptor.ImplementationInstance);
                Assert.Equal(typeof(AnimalService), descriptor.ServiceType);
                Assert.NotNull(descriptor.ImplementationFactory);
            });

        }

        #endregion

        #region ForwardedNames

        [Fact]
        public void Can_Forward_Names()
        {
            var namedRegistrations = new NamedServiceRegistry<AnimalService>();
            namedRegistrations.AddSingleton("A", new AnimalService() { SomeProperty = "A" });
            namedRegistrations.AddSingleton("B", new AnimalService() { SomeProperty = "B" });

            namedRegistrations.ForwardName("FOO", "A");
            namedRegistrations.ForwardName("BAR", "B");

            var registeredA = namedRegistrations["A"];
            var registeredB = namedRegistrations["B"];

            var registeredForwardedToA = namedRegistrations["FOO"];
            var registeredForwardedToB = namedRegistrations["BAR"];

            Assert.Throws<KeyNotFoundException>(() => namedRegistrations["FoO"]); // case sensitive.
            Assert.Throws<KeyNotFoundException>(() => namedRegistrations["bar"]); // case sensitive.

            Assert.Same(registeredA, registeredForwardedToA);
            Assert.Same(registeredB, registeredForwardedToB);
        }

        #endregion

        #region DynamicLookup

        [Fact]
        public void Can_Use_Dynamic_Lookup()
        {
            var namedRegistrations = new NamedServiceRegistry<AnimalService>();
            namedRegistrations.AddSingleton("A", new AnimalService() { SomeProperty = "A" });
            namedRegistrations.AddSingleton("B", new AnimalService() { SomeProperty = "B" });

            var requestsMade = new List<string>();

            namedRegistrations.AddLateRegistration((name, factory) =>
            {
                // Capturing the name that was requested for test assertions..
                requestsMade.Add(name);

                if (name.StartsWith("Transient"))
                {
                    return factory.Result((a) => a.Create(ServiceLifetime.Transient));
                };

                if (name.StartsWith("ScopedWithFactory"))
                {
                    return factory.Result((a) => a.Create((sp) => new AnimalService(), ServiceLifetime.Scoped));
                }

                if (name.StartsWith("SingletonWithImplementationType"))
                {
                    return factory.Result(a => a.Create<LionService>(ServiceLifetime.Singleton));
                }
                // satisfy any other unrecognised names?
                return null; //nah
            });

            // make sure it works with the forwarded names feature.
            namedRegistrations.ForwardName("S", "ScopedWithFactory");

            var scoped = namedRegistrations["ScopedWithFactory"];

            Assert.Single(requestsMade);
            Assert.Contains("ScopedWithFactory", requestsMade);

            var scopedForwarded = namedRegistrations["S"];
            Assert.Single(requestsMade); // shouldn't have resulted in any more calls as mapped to "ScopedWithFactory"

            Assert.Same(scoped, scopedForwarded);

            var transientA = namedRegistrations["Transient"];
            Assert.Contains("Transient", requestsMade);
            Assert.Equal(2, requestsMade.Count);

            var transientB = namedRegistrations["Transient"];
            Assert.Equal(2, requestsMade.Count); // no more calls made
            Assert.Same(transientA, transientB); // its the same registration that provides the transient instance - don't get confused

            var singletonA = namedRegistrations["SingletonWithImplementationType"];
            Assert.Contains("SingletonWithImplementationType", requestsMade);
            Assert.Equal(3, requestsMade.Count);

            var singletonB = namedRegistrations["SingletonWithImplementationType"];
            Assert.Equal(3, requestsMade.Count); // no more calls made
            Assert.Same(singletonA, singletonB); // same singleton isntance.           
        }

        #endregion


        private void AssertServiceDescriptor<TService>(Action<NamedServiceRegistry<TService>> addRegistrations, Action<ServiceDescriptor> assertDescriptor)
            where TService : AnimalService
        {
            var services = new ServiceCollection();
            var namedRegistrations = new NamedServiceRegistry<TService>(services);
            addRegistrations(namedRegistrations);
            var descriptor = services.FirstOrDefault();
            assertDescriptor(descriptor);

        }

        private void AssertRegistration<TService, TImplementationType>(ServiceLifetime lifetime, NamedServiceRegistry<TService> registry, string name, bool hasFactoryFunc = false)
        {
            var registration = registry[name];
            Assert.NotNull(registration);
            Assert.Equal(lifetime, registration.Lifetime);

            var shouldRegistrationOwnInstance = name == "" ? false : (lifetime == ServiceLifetime.Singleton); // nameless registrations by type and factory func are owned by the underlying IServiceProvider
            Assert.Equal(shouldRegistrationOwnInstance, registration.RegistrationOwnsInstance);

            if (hasFactoryFunc)
            {
                Assert.Null(registration.ImplementationType);
            }
            else
            {
                if (name == "")
                {
                    // we don't need to know about the implementation type as we pass the resolve through the IServiceProvider.
                    Assert.Null(registration.ImplementationType);
                }
                else
                {
                    Assert.Equal(typeof(TImplementationType), registration.ImplementationType);
                }

                //if ( registration.TrackScopedLifetime)
                //{
                //}
                //else
                //{
                //    Assert.Null(registration.ImplementationType);
                //}
            }
            Assert.NotNull(registration.InstanceFactory);
        }

        [Fact]
        public void Cannot_Add_Duplicate_Keys()
        {
            var namedRegistrations = new NamedServiceRegistry<AnimalService>();
            namedRegistrations.AddTransient("ABC");
            AssertThrowsDuplicateKey(namedRegistrations, "ABC");

            namedRegistrations.AddScoped("FOO");
            AssertThrowsDuplicateKey(namedRegistrations, "FOO");

            namedRegistrations.AddSingleton("BAR");
            AssertThrowsDuplicateKey(namedRegistrations, "BAR");

            // Test registering default name.
            namedRegistrations = new NamedServiceRegistry<AnimalService>();
            namedRegistrations.AddTransient(); // defaultname = string.Empty
            AssertThrowsDuplicateKey(namedRegistrations, string.Empty);

            namedRegistrations = new NamedServiceRegistry<AnimalService>();
            namedRegistrations.AddScoped(); // defaultname = string.Empty
            AssertThrowsDuplicateKey(namedRegistrations, string.Empty);

            namedRegistrations = new NamedServiceRegistry<AnimalService>();
            namedRegistrations.AddSingleton(); // defaultname = string.Empty
            AssertThrowsDuplicateKey(namedRegistrations, string.Empty);

            // Test registering default name with implementation type
            namedRegistrations = new NamedServiceRegistry<AnimalService>();
            namedRegistrations.AddTransient<BearService>(); // defaultname = string.Empty
            AssertThrowsDuplicateKey(namedRegistrations, string.Empty);

            namedRegistrations = new NamedServiceRegistry<AnimalService>();
            namedRegistrations.AddScoped(); // defaultname = string.Empty
            AssertThrowsDuplicateKey(namedRegistrations, string.Empty);

            namedRegistrations = new NamedServiceRegistry<AnimalService>();
            namedRegistrations.AddSingleton(); // defaultname = string.Empty
            AssertThrowsDuplicateKey(namedRegistrations, string.Empty);


        }

        private void AssertThrowsDuplicateKey(NamedServiceRegistry<AnimalService> namedRegistrations, string key)
        {
            Assert.Throws<ArgumentException>(() => namedRegistrations.AddTransient(key));
            Assert.Throws<ArgumentException>(() => namedRegistrations.AddTransient(key, (sp) => new AnimalService()));
            Assert.Throws<ArgumentException>(() => namedRegistrations.Add(ServiceLifetime.Transient, key));

            Assert.Throws<ArgumentException>(() => namedRegistrations.AddScoped(key));
            Assert.Throws<ArgumentException>(() => namedRegistrations.AddScoped(key, (sp) => new AnimalService()));
            Assert.Throws<ArgumentException>(() => namedRegistrations.Add(ServiceLifetime.Scoped, key));

            Assert.Throws<ArgumentException>(() => namedRegistrations.AddSingleton(key));
            Assert.Throws<ArgumentException>(() => namedRegistrations.AddSingleton(key, (sp) => new AnimalService()));
            Assert.Throws<ArgumentException>(() => namedRegistrations.Add(ServiceLifetime.Singleton, key));

            if (key == string.Empty)
            {
                // transient
                Assert.Throws<ArgumentException>(() => namedRegistrations.AddTransient());
                Assert.Throws<ArgumentException>(() => namedRegistrations.AddTransient((sp) => new AnimalService()));
                Assert.Throws<ArgumentException>(() => namedRegistrations.AddTransient<BearService>());
                Assert.Throws<ArgumentException>(() => namedRegistrations.AddTransient<BearService>((sp) => new BearService()));

                Assert.Throws<ArgumentException>(() => namedRegistrations.Add(ServiceLifetime.Transient));
                Assert.Throws<ArgumentException>(() => namedRegistrations.Add(ServiceLifetime.Transient, sp => new AnimalService()));
                Assert.Throws<ArgumentException>(() => namedRegistrations.Add<BearService>(ServiceLifetime.Transient));
                Assert.Throws<ArgumentException>(() => namedRegistrations.Add<BearService>(ServiceLifetime.Transient, sp => new BearService()));

                // scoped
                Assert.Throws<ArgumentException>(() => namedRegistrations.AddScoped());
                Assert.Throws<ArgumentException>(() => namedRegistrations.AddScoped((sp) => new AnimalService()));
                Assert.Throws<ArgumentException>(() => namedRegistrations.AddScoped<BearService>());
                Assert.Throws<ArgumentException>(() => namedRegistrations.AddScoped<BearService>((sp) => new BearService()));

                Assert.Throws<ArgumentException>(() => namedRegistrations.Add(ServiceLifetime.Scoped));
                Assert.Throws<ArgumentException>(() => namedRegistrations.Add(ServiceLifetime.Scoped, sp => new AnimalService()));
                Assert.Throws<ArgumentException>(() => namedRegistrations.Add<BearService>(ServiceLifetime.Scoped));
                Assert.Throws<ArgumentException>(() => namedRegistrations.Add<BearService>(ServiceLifetime.Scoped, sp => new BearService()));

                // singletin
                Assert.Throws<ArgumentException>(() => namedRegistrations.AddSingleton());
                Assert.Throws<ArgumentException>(() => namedRegistrations.AddSingleton((sp) => new AnimalService()));
                Assert.Throws<ArgumentException>(() => namedRegistrations.AddSingleton<BearService>());
                Assert.Throws<ArgumentException>(() => namedRegistrations.AddSingleton<BearService>((sp) => new BearService()));

                Assert.Throws<ArgumentException>(() => namedRegistrations.Add(ServiceLifetime.Singleton));
                Assert.Throws<ArgumentException>(() => namedRegistrations.Add(ServiceLifetime.Singleton, sp => new AnimalService()));
                Assert.Throws<ArgumentException>(() => namedRegistrations.Add<BearService>(ServiceLifetime.Singleton));
                Assert.Throws<ArgumentException>(() => namedRegistrations.Add<BearService>(ServiceLifetime.Singleton, sp => new BearService()));

            }
        }

        private IServiceProvider GetDefaultServiceProvider()
        {
            var services = new ServiceCollection();
            return services.BuildServiceProvider();
        }

    }

    public class AnimalService
    {
        public string SomeProperty { get; set; }
    }

    public class LionService : AnimalService
    {
        public bool SomeOtherProperty { get; set; }
    }

    public class TigerService : AnimalService
    {
        public bool SomeOtherProperty { get; set; }
    }

    public class BearService : AnimalService
    {
        public bool SomeOtherProperty { get; set; }
    }

    public class DisposableTigerService : AnimalService, IDisposable
    {
        public bool WasDisposed { get; set; } = false;
        public void Dispose()
        {
            WasDisposed = true;
        }
    }


}
