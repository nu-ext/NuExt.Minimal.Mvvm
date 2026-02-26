namespace Minimal.Mvvm.Tests
{
    public sealed class ServiceProviderTestsV4
    {
        private sealed class Foo { }
        private sealed class Bar { }

        [SetUp]
        public void SetUp()
        {
            // Ensure Default is isolated per test.
            ServiceProvider.Default = new ServiceProvider();
        }

        [TearDown]
        public void TearDown()
        {
            // Reset to built-in default (null setter resets to built-in per our provider semantics).
            ServiceProvider.Default = null!;
        }

        [Test]
        public void GetService_LocalOnly_DoesNotCallUpstreamOrParent()
        {
            // Arrange
            int upstreamResolveCalls = 0;
            int upstreamEnumerateCalls = 0;

            object? Resolve(Type serviceType, string? name)
            {
                upstreamResolveCalls++;
                return null;
            };
            IEnumerable<object> Enumerate(Type serviceType)
            {
                upstreamEnumerateCalls++;
                yield break;
            };

            var provider = new ServiceProvider(Resolve, Enumerate);
            provider.RegisterService(typeof(Foo), new Foo());

            // Act
            var local = provider.GetService(typeof(Foo), name: null, localOnly: true);
            var list = provider.GetServices(typeof(Foo), localOnly: true, includeTransient: true).ToArray();

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(local, Is.InstanceOf<Foo>());
                Assert.That(list, Has.Length.EqualTo(1));
                Assert.That(upstreamResolveCalls, Is.Zero);
                Assert.That(upstreamEnumerateCalls, Is.Zero);
            }
        }

        [Test]
        public void GetService_UsesUpstreamResolve_WithReentrancyGuard()
        {
            // Arrange
            ServiceProvider? provider = null;
            bool innerReturnedNullDueToGuard = false;

            var result = new Foo();

            object? Resolve(Type t, string? n)
            {
                // Attempt to re-enter the same provider: guard should prevent recursion.
                var inner = provider!.GetService(t, n, localOnly: false);
                innerReturnedNullDueToGuard = inner is null;
                return result;
            }

            IEnumerable<object> Enumerate(Type t) => Array.Empty<object>();

            provider = new ServiceProvider(Resolve, Enumerate);

            // Act
            var service = provider.GetService(typeof(Foo));

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(innerReturnedNullDueToGuard, Is.True, "Reentrancy guard must return null to avoid recursion.");
                Assert.That(service, Is.SameAs(result));
            }
        }

        private sealed class ParentContainer(object obj) : IServiceContainer
        {
            public object? GetService(Type serviceType) => obj;
            public object? GetService(Type serviceType, string? name) => obj;
            public object? GetService(Type serviceType, string? name, bool localOnly) => obj;

            public IEnumerable<object> GetServices(Type serviceType) => new[] { obj };
            public IEnumerable<object> GetServices(Type serviceType, bool localOnly) => new[] { obj };
            public IEnumerable<object> GetServices(Type serviceType, bool localOnly, bool includeTransient) => new[] { obj };

            public void RegisterService(Type serviceType, object service, bool throwIfExists = false) => throw new NotSupportedException();
            public void RegisterService(Type serviceType, object service, string? name, bool throwIfExists = false) => throw new NotSupportedException();

            public void RegisterService(Type serviceType, bool throwIfExists = false) => throw new NotSupportedException();
            public void RegisterService(Type serviceType, string? name, bool throwIfExists = false) => throw new NotSupportedException();

            public void RegisterService(Type serviceType, Func<object> callback, bool throwIfExists = false) => throw new NotSupportedException();
            public void RegisterService(Type serviceType, Func<object> callback, string? name, bool throwIfExists = false) => throw new NotSupportedException();

            public void RegisterService(Type serviceType, Type implementationType, bool throwIfExists = false) => throw new NotSupportedException();
            public void RegisterService(Type serviceType, Type implementationType, string? name, bool throwIfExists = false) => throw new NotSupportedException();

            public void RegisterService(Type serviceType, Type implementationType, Func<object> callback, bool throwIfExists = false) => throw new NotSupportedException();
            public void RegisterService(Type serviceType, Type implementationType, Func<object> callback, string? name, bool throwIfExists = false) => throw new NotSupportedException();

            public void RegisterTransient(Type serviceType, Func<object> callback, bool throwIfExists = false) => throw new NotSupportedException();
            public void RegisterTransient(Type serviceType, Func<object> callback, string? name, bool throwIfExists = false) => throw new NotSupportedException();
            public void RegisterTransient(Type serviceType, Type implementationType, Func<object> callback, bool throwIfExists = false) => throw new NotSupportedException();

            public void RegisterTransient(Type serviceType, Type implementationType, Func<object> callback, string? name, bool throwIfExists = false) => throw new NotSupportedException();

            public void RegisterTransient(Type serviceType, bool throwIfExists = false) => throw new NotSupportedException();
            public void RegisterTransient(Type serviceType, string? name, bool throwIfExists = false) => throw new NotSupportedException();
            public void RegisterTransient(Type serviceType, Type implementationType, bool throwIfExists = false) => throw new NotSupportedException();
            public void RegisterTransient(Type serviceType, Type implementationType, string? name, bool throwIfExists = false) => throw new NotSupportedException();

            public bool UnregisterService(object service) => false;
            public bool UnregisterService(Type serviceType) => false;
            public bool UnregisterService(Type serviceType, string? name) => false;

            public IServiceContainer CreateScope() => this;
            public void Clear() { }
            public Task CleanupAsync(Func<object, Task> cleanupOperation, bool continueOnCapturedContext) => Task.CompletedTask;
        }

        [Test]
        public void GetService_UsesParentProvider_WhenNoDelegates()
        {
            // Arrange
            var parentObj = new Bar();
            var parent = new ParentContainer(parentObj);
            var provider = new ServiceProvider(parent);

            // Act
            var resolved = provider.GetService(typeof(Bar));
            var list = provider.GetServices(typeof(Bar)).ToArray();

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(resolved, Is.SameAs(parentObj));
                Assert.That(list, Has.Length.EqualTo(1));
            }
            Assert.That(list[0], Is.SameAs(parentObj));
        }

        [Test]
        public void RegisterTransient_ProvidesNewInstanceEachTime()
        {
            // Arrange
            var provider = new ServiceProvider();
            provider.RegisterTransient(typeof(Foo), () => new Foo());

            // Act
            var a = provider.GetService(typeof(Foo));
            var b = provider.GetService(typeof(Foo));

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(a, Is.InstanceOf<Foo>());
                Assert.That(b, Is.InstanceOf<Foo>());
            }
            Assert.That(a, Is.Not.SameAs(b));
        }

        [Test]
        public void GetServices_LocalOnly_IgnoresUpstreamEnumerate()
        {
            // Arrange
            int upstreamEnumerateCalls = 0;
            var local1 = new Foo();

            IEnumerable<object> Enumerate(Type t)
            {
                upstreamEnumerateCalls++;
                return new object[] { new Foo() };
            }

            var provider = new ServiceProvider(
                upstreamResolve: (t, n) => null,
                upstreamEnumerate: Enumerate);

            provider.RegisterService(typeof(Foo), local1);

            // Act
            var items = provider.GetServices(typeof(Foo), localOnly: true, includeTransient: true).ToArray();

            // Assert
            Assert.That(items, Has.Length.EqualTo(1));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(items[0], Is.SameAs(local1));
                Assert.That(upstreamEnumerateCalls, Is.Zero);
            }
        }
    }
}
