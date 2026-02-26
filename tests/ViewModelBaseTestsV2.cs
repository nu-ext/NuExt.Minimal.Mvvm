namespace Minimal.Mvvm.Tests
{
    public sealed class ViewModelBaseTestsV2
    {
        private sealed class Marker { }
        private interface ICommon { }
        private sealed class CommonA : ICommon { }
        private sealed class CommonB : ICommon { }

        private sealed class TestViewModel(IServiceContainer? fallback = null) : ViewModelBase(fallback)
        {
            public void RegisterLocal(Type t, object instance, string? name = null)
            {
                Services.RegisterService(t, instance, name);
            }
        }

        [SetUp]
        public void SetUp()
        {
            // Isolate Default per test
            ServiceProvider.Default = new ServiceProvider();
        }

        [TearDown]
        public void TearDown()
        {
            ServiceProvider.Default = null!;
        }

        [Test]
        public void SingleResolve_Order_Local_Fallback_Parent_Default()
        {
            // Arrange: Default provides Marker
            var def = new ServiceProvider();
            def.RegisterService(typeof(Marker), new Marker());
            ServiceProvider.Default = def;

            // Parent VM provides CommonA
            var parentVm = new TestViewModel();
            var parentA = new CommonA();
            parentVm.RegisterLocal(typeof(ICommon), parentA);

            // Fallback provides CommonB
            var fallback = new ServiceProvider();
            var fbB = new CommonB();
            fallback.RegisterService(typeof(ICommon), fbB);

            // Child VM: local empty, fallback set, parent set
            var childVm = new TestViewModel(fallback)
            {
                ParentViewModel = parentVm
            };

            // Local registration for something else
            var localObj = new object();
            childVm.RegisterLocal(typeof(object), localObj);

            // Act & Assert:
            // 1) Local wins for 'object'
            Assert.That(childVm.GetService<object>(), Is.SameAs(localObj));

            // 2) Fallback wins for ICommon (no local), before parent
            var s1 = childVm.GetService<ICommon>();
            Assert.That(s1, Is.SameAs(fbB));

            // 3) If fallback misses, parent supplies
            //    (unregister fbB and try again)
            fallback.UnregisterService(typeof(ICommon));
            var s2 = childVm.GetService<ICommon>();
            Assert.That(s2, Is.SameAs(parentA));

            // 4) If all miss, Default supplies Marker
            var m = childVm.GetService<Marker>();
            Assert.That(m, Is.InstanceOf<Marker>());
        }

        [Test]
        public void Enumeration_DoesNotConsult_Default_And_Deduplicates_ByReference()
        {
            // Arrange
            var shared = new CommonA();

            var fallback = new ServiceProvider();
            var parentVm = new TestViewModel();

            // Put the same instance reference into all three scopes to test dedup
            var vm = new TestViewModel(fallback);
            vm.RegisterLocal(typeof(ICommon), shared);
            fallback.RegisterService(typeof(ICommon), shared);
            parentVm.RegisterLocal(typeof(ICommon), shared);
            vm.ParentViewModel = parentVm;

            // Default also has a different object, but must NOT be included in enumeration
            var def = new ServiceProvider();
            def.RegisterService(typeof(ICommon), new CommonB());
            ServiceProvider.Default = def;

            // Act
            var all = vm.GetServices<ICommon>().ToArray();

            // Assert
            Assert.That(all, Has.Length.EqualTo(1));
            Assert.That(all[0], Is.SameAs(shared), "Enumeration must deduplicate by reference and must not include Default.");
        }

        [Test]
        public void Services_LocalOnly_Enumerates_Only_Local()
        {
            // Arrange: child with fallback and parent—both holding items
            var fallback = new ServiceProvider();
            fallback.RegisterService(typeof(ICommon), new CommonA());

            var parent = new TestViewModel();
            parent.RegisterLocal(typeof(ICommon), new CommonB());

            var child = new TestViewModel(fallback)
            {
                ParentViewModel = parent
            };

            // Also local
            var local = new CommonA();
            child.RegisterLocal(typeof(ICommon), local);

            // Act: go directly through IServiceContainer (localOnly: true)
            var items = child.Services.GetServices(typeof(ICommon), localOnly: true).ToArray();

            // Assert
            Assert.That(items, Has.Length.EqualTo(1));
            Assert.That(items[0], Is.SameAs(local));
        }

        [Test]
        public void Enumeration_Order_Local_Then_Upstream()
        {
            // Arrange
            var fallback = new ServiceProvider();
            var parent = new TestViewModel();

            var local1 = new CommonA();
            var upstream1 = new CommonB();

            var child = new TestViewModel(fallback);
            child.RegisterLocal(typeof(ICommon), local1);
            fallback.RegisterService(typeof(ICommon), upstream1);
            child.ParentViewModel = parent;

            // Act
            var list = child.GetServices<ICommon>().ToList();

            // Assert
            // We only assert that all upstream instances appear after at least one local instance.
            var localIndices = list
                .Select((v, i) => (v, i))
                .Where(x => ReferenceEquals(x.v, local1))
                .Select(x => x.i)
                .ToArray();

            var upstreamIndices = list
                .Select((v, i) => (v, i))
                .Where(x => ReferenceEquals(x.v, upstream1))
                .Select(x => x.i)
                .ToArray();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(localIndices, Is.Not.Empty);
                Assert.That(upstreamIndices, Is.Not.Empty);
            }
            Assert.That(upstreamIndices.Min(), Is.GreaterThan(localIndices.Max()),
                "Upstream items must be yielded after local items.");
        }
    }
}
