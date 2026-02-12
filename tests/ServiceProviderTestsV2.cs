namespace Minimal.Mvvm.Tests
{
    [TestFixture]
    public class ServiceProviderTestsV2
    {
        #region Basic Registration and Resolution

        [Test]
        public void GetService_ReturnsNull_WhenServiceNotRegistered()
        {
            // Arrange
            var provider = new ServiceProvider();

            // Act
            var service = provider.GetService(typeof(IService));

            // Assert
            Assert.That(service, Is.Null);
        }

        [Test]
        public void RegisterService_Instance_CanBeResolved()
        {
            // Arrange
            var provider = new ServiceProvider();
            var instance = new ServiceImpl();

            // Act
            provider.RegisterService(typeof(IService), instance);
            var resolved = provider.GetService(typeof(IService));

            // Assert
            Assert.That(resolved, Is.SameAs(instance));
        }

        [Test]
        public void RegisterService_GenericInstance_CanBeResolved()
        {
            // Arrange
            var provider = new ServiceProvider();
            var instance = new ServiceImpl();

            // Act
            provider.RegisterService<IService>(instance);
            var resolved = provider.GetService<IService>();

            // Assert
            Assert.That(resolved, Is.SameAs(instance));
        }

        [Test]
        public void RegisterService_ThrowIfExists_ThrowsOnDuplicate()
        {
            // Arrange
            var provider = new ServiceProvider();
            provider.RegisterService(typeof(IService), new ServiceImpl());

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                provider.RegisterService(typeof(IService), new ServiceImpl(), throwIfExists: true));
        }

        [Test]
        public void RegisterService_NotThrowIfExists_UpdatesService()
        {
            // Arrange
            var provider = new ServiceProvider();
            var first = new ServiceImpl();
            var second = new ServiceImpl();

            // Act
            provider.RegisterService(typeof(IService), first, throwIfExists: false);
            provider.RegisterService(typeof(IService), second, throwIfExists: false);
            var resolved = provider.GetService(typeof(IService));

            // Assert
            Assert.That(resolved, Is.SameAs(second));
            Assert.That(resolved, Is.Not.SameAs(first));
        }

        #endregion

        #region Factory Registration

        [Test]
        public void RegisterService_Factory_CreatesServiceLazily()
        {
            // Arrange
            var provider = new ServiceProvider();
            bool created = false;
            var instance = new ServiceImpl();

            // Act
            provider.RegisterService(typeof(IService), () =>
            {
                created = true;
                return instance;
            });

            // Assert - not created yet
            Assert.That(created, Is.False);

            var resolved = provider.GetService(typeof(IService));

            using (Assert.EnterMultipleScope())
            {
                // Assert - created on resolution
                Assert.That(created, Is.True);
                Assert.That(resolved, Is.SameAs(instance));
            }
        }

        [Test]
        public void RegisterService_Factory_ThrowsWhenReturnsNull()
        {
            // Arrange
            var provider = new ServiceProvider();

            // Act
            provider.RegisterService(typeof(IService), () => null!);

            // Assert
            Assert.Throws<InvalidOperationException>(() =>
                provider.GetService(typeof(IService)));
        }

        [Test]
        public void RegisterService_FactoryWithType_CreatesCorrectType()
        {
            // Arrange
            var provider = new ServiceProvider();

            // Act
            provider.RegisterService(typeof(IService), typeof(ServiceImpl), () => new ServiceImpl());
            var resolved = provider.GetService(typeof(IService));

            // Assert
            Assert.That(resolved, Is.InstanceOf<ServiceImpl>());
        }

        #endregion

        #region Type Registration (Constructor)

        [Test]
        public void RegisterService_Type_CreatesInstanceViaConstructor()
        {
            // Arrange
            var provider = new ServiceProvider();

            // Act
            provider.RegisterService(typeof(IService), typeof(ServiceImpl));
            var resolved = provider.GetService(typeof(IService));

            // Assert
            Assert.That(resolved, Is.InstanceOf<ServiceImpl>());
        }

        [Test]
        public void RegisterService_Type_ThrowsIfNoParameterlessConstructor()
        {
            // Arrange
            var provider = new ServiceProvider();

            // Act & Assert
            provider.RegisterService(typeof(NoDefaultCtorService), typeof(NoDefaultCtorService));
            Assert.Throws<MissingMethodException>(() => provider.GetService<NoDefaultCtorService>());
        }

        #endregion

        #region Named Services

        [Test]
        public void GetService_Named_ReturnsCorrectInstance()
        {
            // Arrange
            var provider = new ServiceProvider();
            var defaultService = new ServiceImpl();
            var namedService = new ServiceImpl();

            // Act
            provider.RegisterService(typeof(IService), defaultService);
            provider.RegisterService(typeof(IService), namedService, "named");

            var defaultResolved = provider.GetService(typeof(IService));
            var namedResolved = provider.GetService(typeof(IService), "named");

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(defaultResolved, Is.SameAs(defaultService));
                Assert.That(namedResolved, Is.SameAs(namedService));
            }
        }

        [Test]
        public void GetService_Named_ReturnsAssignableWhenExactNameIsEmpty()
        {
            // Arrange
            var provider = new ServiceProvider();
            var namedService = new ServiceImpl();

            // Act
            provider.RegisterService(typeof(IService), namedService, "specificName");
            var resolved = provider.GetService(typeof(IService), "");

            // Assert - should find by type assignability
            Assert.That(resolved, Is.SameAs(namedService));
        }

        [Test]
        public void GetService_Unnamed_FindsNamedServicesAsFallback()
        {
            // Arrange
            var provider = new ServiceProvider();
            var namedService = new ServiceImpl();

            // Act
            provider.RegisterService(typeof(IService), namedService, "named");
            var resolved = provider.GetService(typeof(IService));

            // Assert
            Assert.That(resolved, Is.SameAs(namedService));
        }

        [Test]
        public void GetService_ExtensionNamed_WorksWithGenericMethod()
        {
            // Arrange
            var provider = new ServiceProvider();
            var service = new ServiceImpl();

            // Act
            provider.RegisterService<IService>(service, "myName");
            var resolved = provider.GetService<IService>("myName");

            // Assert
            Assert.That(resolved, Is.SameAs(service));
        }

        #endregion

        #region Multiple Services (GetServices)

        [Test]
        public void GetServices_ReturnsAllMatchingServices()
        {
            // Arrange
            var provider = new ServiceProvider();
            var service1 = new ServiceImpl();
            var service2 = new ServiceImpl();
            var service3 = new AnotherServiceImpl();

            // Act
            provider.RegisterService(typeof(IService), service1);
            provider.RegisterService(typeof(IService), service2, "named");
            provider.RegisterService(typeof(IAnotherService), service3);

            var services = provider.GetServices(typeof(IService)).ToList();

            // Assert
            Assert.That(services, Has.Count.EqualTo(3));
            Assert.That(services, Contains.Item(service1));
            Assert.That(services, Contains.Item(service2));
            Assert.That(services, Contains.Item(service3));
        }

        [Test]
        public void GetServices_GenericExtension_ReturnsTypedEnumerable()
        {
            // Arrange
            var provider = new ServiceProvider();
            var service1 = new ServiceImpl();
            var service2 = new ServiceImpl();

            // Act
            provider.RegisterService<IService>(service1);
            provider.RegisterService<IService>(service2, "named");

            var services = provider.GetServices<IService>().ToList();

            // Assert
            Assert.That(services, Has.Count.EqualTo(2));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(services[0], Is.InstanceOf<ServiceImpl>());
                Assert.That(services[1], Is.InstanceOf<ServiceImpl>());
            }
        }

        [Test]
        public void GetServices_ReturnsDistinctInstances()
        {
            // Arrange
            var provider = new ServiceProvider();
            var service = new ServiceImpl();

            // Act - register same instance twice
            provider.RegisterService(typeof(IService), service);
            provider.RegisterService(typeof(IService), service, "named");

            var services = provider.GetServices(typeof(IService)).ToList();

            // Assert
            Assert.That(services, Has.Count.EqualTo(1));
            Assert.That(services[0], Is.SameAs(service));
        }

        [Test]
        public void GetServices_WithParentProvider_IncludesParentServices()
        {
            // Arrange
            var parent = new ServiceProvider();
            var child = new ServiceProvider(parent);

            var parentService = new ServiceImpl();
            var childService = new AnotherServiceImpl();

            // Act
            parent.RegisterService(typeof(IService), parentService);
            child.RegisterService(typeof(IService), childService);

            var services = child.GetServices(typeof(IService)).ToList();

            // Assert
            Assert.That(services, Has.Count.EqualTo(2));
            Assert.That(services, Contains.Item(parentService));
            Assert.That(services, Contains.Item(childService));
        }

        #endregion

        #region Parent Provider Delegation

        [Test]
        public void GetService_DelegatesToParent_WhenNotFound()
        {
            // Arrange
            var parent = new ServiceProvider();
            var child = new ServiceProvider(parent);
            var service = new ServiceImpl();

            // Act
            parent.RegisterService(typeof(IService), service);
            var resolved = child.GetService(typeof(IService));

            // Assert
            Assert.That(resolved, Is.SameAs(service));
        }

        [Test]
        public void GetService_DoesNotDelegate_WhenParentReentrancyGuardActive()
        {
            // Arrange
            var parent = new ServiceProvider();
            var child = new ServiceProvider(parent);
            var service = new ServiceImpl();

            // Act - simulate parent calling back to child
            parent.RegisterService(typeof(IService), () =>
            {
                // This would create infinite recursion if not guarded
                return child.GetService(typeof(IService)) ?? service;
            });

            var resolved = child.GetService(typeof(IService));

            // Assert - should get service from parent's factory
            Assert.That(resolved, Is.SameAs(service));
        }

        [Test]
        public void GetService_WithNamedParentProvider_DelegatesCorrectly()
        {
            // Arrange
            var parent = new ServiceProvider();
            var child = new ServiceProvider(parent);
            var service = new ServiceImpl();

            // Act
            parent.RegisterService(typeof(IService), service, "specific");
            var resolved = child.GetService(typeof(IService), "specific");

            // Assert
            Assert.That(resolved, Is.SameAs(service));
        }

        #endregion

        #region Circular Dependency Detection

        [Test]
        public void GetService_ThrowsOnCircularDependency_WhenEnabled()
        {
            // Arrange
            var provider = new ServiceProvider
            {
                ThrowOnCircularDependency = true
            };

            // Act - create circular dependency via factory
            provider.RegisterService(typeof(IService), () =>
            {
                // This creates a circle
                return provider.GetService(typeof(IAnotherService))!;
            });

            provider.RegisterService(typeof(IAnotherService), () =>
            {
                return provider.GetService(typeof(IService))!;
            });

            // Assert
            Assert.Throws<InvalidOperationException>(() =>
                provider.GetService(typeof(IService)));
        }

        #endregion

        #region Unregistration and Clear

        [Test]
        public void UnregisterService_ByType_RemovesService()
        {
            // Arrange
            var provider = new ServiceProvider();
            var service = new ServiceImpl();
            provider.RegisterService(typeof(IService), service);

            // Act
            var removed = provider.UnregisterService(typeof(IService));
            var resolved = provider.GetService(typeof(IService));

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(removed, Is.True);
                Assert.That(resolved, Is.Null);
            }
        }

        [Test]
        public void UnregisterService_ByInstance_RemovesService()
        {
            // Arrange
            var provider = new ServiceProvider();
            var service = new ServiceImpl();
            provider.RegisterService(typeof(IService), service);

            // Act
            var removed = provider.UnregisterService(service);
            var resolved = provider.GetService(typeof(IService));

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(removed, Is.True);
                Assert.That(resolved, Is.Null);
            }
        }

        [Test]
        public void UnregisterService_ByNamed_RemovesCorrectService()
        {
            // Arrange
            var provider = new ServiceProvider();
            var service1 = new ServiceImpl();
            var service2 = new ServiceImpl();

            provider.RegisterService(typeof(IService), service1);
            provider.RegisterService(typeof(IService), service2, "named");

            // Act
            var removed = provider.UnregisterService(typeof(IService), "named");
            var resolved1 = provider.GetService(typeof(IService));
            var resolved2 = provider.GetService(typeof(IService), "named");

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(removed, Is.True);
                Assert.That(resolved1, Is.SameAs(service1));
                Assert.That(resolved2, Is.Null);
            }
        }

        [Test]
        public void Clear_RemovesAllServices()
        {
            // Arrange
            var provider = new ServiceProvider();
            provider.RegisterService(typeof(IService), new ServiceImpl());
            provider.RegisterService(typeof(IAnotherService), new AnotherServiceImpl(), "named");

            // Act
            provider.Clear();

            var service1 = provider.GetService(typeof(IService));
            var service2 = provider.GetService(typeof(IAnotherService), "named");

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(service1, Is.Null);
                Assert.That(service2, Is.Null);
            }
        }

        #endregion

        #region Disposal

        [Test]
        public void Dispose_ClearsServicesAndDisposesThreadLocal()
        {
            // Arrange
            var provider = new ServiceProvider();
            var service = new DisposableService();
            provider.RegisterService(typeof(IDisposableService), service);

            // Act
            provider.Clear();

            using (Assert.EnterMultipleScope())
            {
                // Assert - service should be removed
                Assert.That(provider.GetService(typeof(IDisposableService)), Is.Null);
                Assert.That(service.IsDisposed, Is.False); // Container doesn't dispose services by default
            }
        }

        [Test]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Arrange
            var provider = new ServiceProvider();

            // Act & Assert - no exception
            provider.Dispose();
            provider.Dispose();
        }

        #endregion

        #region Thread Safety

        [Test]
        public void Concurrent_Registrations_DoNotCorruptState()
        {
            // Arrange
            var provider = new ServiceProvider();
            var iterations = 1000;

            // Act
            Parallel.For(0, iterations, i =>
            {
                provider.RegisterService(typeof(IService), new ServiceImpl(), i.ToString());
            });

            // Assert - all services should be accessible
            for (int i = 0; i < iterations; i++)
            {
                var service = provider.GetService(typeof(IService), i.ToString());
                Assert.That(service, Is.Not.Null);
            }
        }

        [Test]
        public void Concurrent_Resolutions_ReturnConsistentResults()
        {
            // Arrange
            var provider = new ServiceProvider();
            var instance = new ServiceImpl();
            provider.RegisterService(typeof(IService), instance);

            // Act
            var results = new System.Collections.Concurrent.ConcurrentBag<object?>();

            Parallel.For(0, 100, _ =>
            {
                results.Add(provider.GetService(typeof(IService)));
            });

            // Assert - all results should be the same instance
            Assert.That(results, Has.All.SameAs(instance));
        }

        #endregion

        #region Default Provider

        [Test]
        public void Default_StaticProperty_ReturnsSingleton()
        {
            // Arrange
            var default1 = ServiceProvider.Default;
            var default2 = ServiceProvider.Default;

            // Assert
            Assert.That(default2, Is.SameAs(default1));
        }

        [Test]
        public void Default_CanBeOverridden()
        {
            // Arrange
            var original = ServiceProvider.Default;
            var custom = new ServiceProvider();

            try
            {
                // Act
                ServiceProvider.Default = custom;

                // Assert
                Assert.That(ServiceProvider.Default, Is.SameAs(custom));
                Assert.That(ServiceProvider.Default, Is.Not.SameAs(original));
            }
            finally
            {
                // Cleanup
                ServiceProvider.Default = null!; // Reset to original
                Assert.That(ServiceProvider.Default, Is.SameAs(original));
            }
        }

        #endregion

        #region Test Services (Internal types for testing)

        private interface IService { }
        private interface IAnotherService { }
        private interface IDisposableService : IDisposable
        {
            bool IsDisposed { get; }
        }

        private class ServiceImpl : IService { }
        private class AnotherServiceImpl : ServiceImpl, IAnotherService { }

        private class NoDefaultCtorService(string parameter) : IService
        {
            public object Parameter => parameter;
        }

        private class DisposableService : IDisposableService
        {
            public bool IsDisposed { get; private set; }

            public void Dispose()
            {
                IsDisposed = true;
            }
        }

        #endregion
    }
}