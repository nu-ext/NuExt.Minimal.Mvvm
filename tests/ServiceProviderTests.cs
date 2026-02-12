namespace Minimal.Mvvm.Tests
{
    internal partial class ServiceProviderTests
    {

        [Test]
        public void ServiceContainerTest()
        {
            var provider = new ServiceProvider();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(typeof(StubServiceBase).IsAssignableFrom(typeof(StubService)), Is.True);
                Assert.That(typeof(StubService).IsAssignableFrom(typeof(StubServiceBase)), Is.False);
                Assert.That(typeof(StubService).IsAssignableFrom(typeof(IStubService)), Is.False);
                Assert.That(typeof(IStubService).IsAssignableFrom(typeof(StubService)), Is.True);
            }

            provider.RegisterService(new StubService("A"));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(provider.GetService<StubServiceBase>(), Is.Not.Null);
                Assert.That(provider.GetService<StubServiceBase>()!.Name, Is.EqualTo("A"));
                Assert.That(provider.GetService<IStubService>(), Is.Not.Null);
                Assert.That(provider.GetService<IStubService>()!.Name, Is.EqualTo("A"));
            }
            provider.RegisterService<IStubService>(new StubService("B"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(provider.GetService<StubService>(), Is.Not.Null);
                Assert.That(provider.GetService<StubService>()!.Name, Is.EqualTo("A"));
                Assert.That(provider.GetService<IStubService>(), Is.Not.Null);
                Assert.That(provider.GetService<IStubService>()!.Name, Is.EqualTo("B"));
                Assert.That(provider.GetServices<StubServiceBase>(), Is.Not.Null);
                Assert.That(provider.GetServices<StubServiceBase>().Count(), Is.EqualTo(2));
                Assert.That(provider.GetServices<StubService>(), Is.Not.Null);
                Assert.That(provider.GetServices<StubService>().Count(), Is.EqualTo(2));
                Assert.That(provider.GetServices<IStubService>(), Is.Not.Null);
                Assert.That(provider.GetServices<IStubService>().Count(), Is.EqualTo(2));
            }

            provider.RegisterService(new AnotherStubService("C"));
            provider.RegisterService<IStubService>(new AnotherStubService("D"));
            AssertMultiple();
            Cleanup();

            provider.RegisterService(typeof(AnotherStubService), () => new AnotherStubService("C"));
            provider.RegisterService<IStubService>(() => new AnotherStubService("D"));
            AssertMultiple();
            Cleanup();

            provider.RegisterService<AnotherStubService>();
            provider.RegisterService<IStubService>(() => new AnotherStubService("D"));
            AssertMultiple();
            Cleanup();

            provider.RegisterService<AnotherStubService>();
            provider.RegisterService<IStubService>(() => new AnotherStubService("D"));
            AssertMultiple();
            Cleanup();

            Assert.Pass();

            void AssertMultiple()
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(provider.GetService<StubService>(), Is.Not.Null);
                    Assert.That(provider.GetService<StubService>()!.Name, Is.EqualTo("A"));
                    Assert.That(provider.GetService<AnotherStubService>(), Is.Not.Null);
                    Assert.That(provider.GetService<AnotherStubService>()!.Name, Is.EqualTo("C"));
                    Assert.That(provider.GetService<IStubService>(), Is.Not.Null);
                    Assert.That(provider.GetService<IStubService>()!.Name, Is.EqualTo("D"));
                    Assert.That(provider.GetServices<StubServiceBase>(), Is.Not.Null);
                    Assert.That(provider.GetServices<StubServiceBase>().Count(), Is.EqualTo(3));
                    Assert.That(provider.GetServices<StubService>(), Is.Not.Null);
                    Assert.That(provider.GetServices<StubService>().Count(), Is.EqualTo(1));
                    Assert.That(provider.GetServices<AnotherStubService>(), Is.Not.Null);
                    Assert.That(provider.GetServices<AnotherStubService>().Count(), Is.EqualTo(2));
                    Assert.That(provider.GetServices<IStubService>(), Is.Not.Null);
                    Assert.That(provider.GetServices<IStubService>().Count(), Is.EqualTo(3));
                }
            }

            void Cleanup()
            {
                bool res = provider.UnregisterService<AnotherStubService>();
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(res, Is.True);
                    Assert.That(provider.GetService(typeof(AnotherStubService)), Is.Not.Null);
                    Assert.That(((AnotherStubService?)provider.GetService(typeof(AnotherStubService)))!.Name, Is.EqualTo("D"));
                }
                res = provider.UnregisterService<IStubService>();
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(res, Is.True);
                    Assert.That(provider.GetService<IStubService>(), Is.Not.Null);
                    Assert.That(provider.GetService<IStubService>()!.Name, Is.EqualTo("A"));
                }
            }
        }


        private interface IStubService
        {
            string Name { get; }
        }

        private abstract class StubServiceBase(string name)
        {
            public string Name { get; } = name;
        }

        private class StubService(string name) : StubServiceBase(name), IStubService
        {
        }

        private class AnotherStubService(string name) : StubServiceBase(name), IStubService
        {
            public AnotherStubService() : this("C")
            {

            }
        }

        public interface ITestService { string Name { get; } }
        public interface IOtherService { int Value { get; } }

        public class TestServiceImpl : ITestService
        {
            public string Name => "TestService";
            public int Id { get; } = new Random().Next();
        }

        public class AnotherServiceImpl : ITestService
        {
            public string Name => "AnotherService";
        }

        public class OtherServiceImpl : IOtherService
        {
            public int Value => 42;
        }

        private ServiceProvider _provider = null!;

        [SetUp]
        public void SetUp() => _provider = new ServiceProvider();

        [TearDown]
        public void TearDown()
        {
            _provider?.Dispose();
            ServiceProvider.Default = null!;
        }

        #region Basic Registration

        [Test]
        public void RegisterService_ShouldRegisterAndRetrieveInstance()
        {
            var service = new TestServiceImpl();
            _provider.RegisterService<ITestService>(service);
            var result = _provider.GetService<ITestService>();
            Assert.That(result, Is.SameAs(service));
        }

        [Test]
        public void RegisterService_WithName_ShouldRegisterAndRetrieveNamedService()
        {
            var service = new TestServiceImpl();
            _provider.RegisterService<ITestService>(service, "MyService");
            var result = _provider.GetService<ITestService>("MyService");
            Assert.That(result, Is.SameAs(service));
        }

        [Test]
        public void RegisterService_WithFactory_ShouldCreateServiceLazily()
        {
            bool created = false;
            _provider.RegisterService<ITestService>(() =>
            {
                created = true;
                return new TestServiceImpl();
            });

            var before = created;
            var result = _provider.GetService<ITestService>();
            var after = created;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<TestServiceImpl>());
                Assert.That(before, Is.False);
                Assert.That(after, Is.True);
            }
        }

        [Test]
        public void RegisterService_WithType_ShouldCreateInstanceOnDemand()
        {
            _provider.RegisterService<TestServiceImpl>();
            var result = _provider.GetService<TestServiceImpl>();
            Assert.That(result, Is.InstanceOf<TestServiceImpl>());
        }

        [Test]
        public void RegisterService_ConcreteType_CanBeResolvedViaInterface()
        {
            _provider.RegisterService<TestServiceImpl>();
            var result = _provider.GetService<ITestService>();
            Assert.That(result, Is.InstanceOf<TestServiceImpl>());
        }

        [Test]
        public void RegisterService_WithNamedType_ShouldCreateNamedInstance()
        {
            _provider.RegisterService<TestServiceImpl>("MyTestService");
            var result = _provider.GetService<TestServiceImpl>("MyTestService");
            Assert.That(result, Is.InstanceOf<TestServiceImpl>());
        }

        #endregion

        #region Exact Match Priority

        [Test]
        public void GetService_ExactMatch_ShouldHaveHighestPriority()
        {
            var exactService = new TestServiceImpl();
            var baseService = new AnotherServiceImpl();

            _provider.RegisterService<TestServiceImpl>(exactService);
            _provider.RegisterService<ITestService>(baseService);

            var result = _provider.GetService<TestServiceImpl>();
            Assert.That(result, Is.SameAs(exactService));
        }

        [Test]
        public void GetService_ExactMatchWithName_ShouldHaveHighestPriority()
        {
            var namedService = new TestServiceImpl();
            var unnamedService = new AnotherServiceImpl();

            _provider.RegisterService<ITestService>(namedService, "Specific");
            _provider.RegisterService<ITestService>(unnamedService);

            var result = _provider.GetService<ITestService>("Specific");
            Assert.That(result, Is.SameAs(namedService));
        }

        #endregion

        #region Named Search

        [Test]
        public void GetService_WithName_ShouldFindByNameAndTypeAssignment()
        {
            var service = new TestServiceImpl();
            _provider.RegisterService<TestServiceImpl>(service, "MyService");
            var result = _provider.GetService<ITestService>("MyService");
            Assert.That(result, Is.SameAs(service));
        }

        [Test]
        public void GetService_WithName_ShouldReturnNullIfNotFound()
        {
            _provider.RegisterService<ITestService>(new TestServiceImpl(), "OtherName");
            var result = _provider.GetService<ITestService>("NonExistent");
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetService_WithName_ShouldNotFallbackToUnnamed()
        {
            var unnamedService = new TestServiceImpl();
            _provider.RegisterService<ITestService>(unnamedService);
            var result = _provider.GetService<ITestService>("SpecificName");
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetService_EmptyStringName_ShouldBeTreatedAsNull()
        {
            var service1 = new TestServiceImpl();
            var service2 = new AnotherServiceImpl();

            _provider.RegisterService<ITestService>(service1);
            _provider.RegisterService<ITestService>(service2, "");

            var result = _provider.GetService<ITestService>("");
            Assert.That(result, Is.SameAs(service2));
        }

        #endregion

        #region Unnamed Search

        [Test]
        public void GetService_Unnamed_ShouldFindUnnamedServicesFirst()
        {
            var unnamedService = new TestServiceImpl();
            var namedService = new AnotherServiceImpl();

            _provider.RegisterService<ITestService>(unnamedService);
            _provider.RegisterService<ITestService>(namedService, "Named");

            var result = _provider.GetService<ITestService>();
            Assert.That(result, Is.SameAs(unnamedService));
        }

        [Test]
        public void GetService_Unnamed_ShouldFallbackToNamedServices()
        {
            var namedService = new TestServiceImpl();
            _provider.RegisterService<ITestService>(namedService, "OnlyNamed");
            var result = _provider.GetService<ITestService>();
            Assert.That(result, Is.SameAs(namedService));
        }

        [Test]
        public void GetService_Unnamed_ShouldFindByTypeAssignment()
        {
            var service = new TestServiceImpl();
            _provider.RegisterService<TestServiceImpl>(service);
            var result = _provider.GetService<ITestService>();
            Assert.That(result, Is.SameAs(service));
        }

        #endregion

        #region Parent Container

        [Test]
        public void GetService_ShouldFindInParentViewModel()
        {
            var parentViewModel = new TestViewModel();
            parentViewModel.Services.RegisterService<ITestService>(new TestServiceImpl());
            var childProvider = new ServiceProvider(parentViewModel);
            var result = childProvider.GetService<ITestService>();
            Assert.That(result, Is.InstanceOf<TestServiceImpl>());
        }

        [Test]
        public void GetService_WithName_ShouldFindInParentViewModel()
        {
            var parentViewModel = new TestViewModel();
            parentViewModel.Services.RegisterService<ITestService>(new TestServiceImpl(), "ParentService");
            var childProvider = new ServiceProvider(parentViewModel);
            var result = childProvider.GetService<ITestService>("ParentService");
            Assert.That(result, Is.InstanceOf<TestServiceImpl>());
        }

        [Test]
        public void GetService_ShouldPreferLocalOverParent()
        {
            var parentViewModel = new TestViewModel();
            parentViewModel.Services.RegisterService<ITestService>(new AnotherServiceImpl());
            var childProvider = new ServiceProvider(parentViewModel);
            childProvider.RegisterService<ITestService>(new TestServiceImpl());
            var result = childProvider.GetService<ITestService>();
            Assert.That(result, Is.InstanceOf<TestServiceImpl>());
        }

        [Test]
        public void GetService_Unnamed_ShouldSearchParentWhenNotFoundLocally()
        {
            var parentViewModel = new TestViewModel();
            var parentService = new TestServiceImpl();
            parentViewModel.Services.RegisterService<ITestService>(parentService);
            var childProvider = new ServiceProvider(parentViewModel);
            var result = childProvider.GetService<ITestService>();
            Assert.That(result, Is.SameAs(parentService));
        }

        #endregion

        #region GetServices

        [Test]
        public void GetServices_ShouldReturnAllMatchingServices()
        {
            var service1 = new TestServiceImpl();
            var service2 = new AnotherServiceImpl();

            _provider.RegisterService<ITestService>(service1);
            _provider.RegisterService<ITestService>(service2, "Named");

            var results = _provider.GetServices<ITestService>().ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(results, Has.Count.EqualTo(2));
                Assert.That(results, Contains.Item(service1));
                Assert.That(results, Contains.Item(service2));
            }
        }

        [Test]
        public void GetServices_ShouldReturnUniqueServices()
        {
            var service = new TestServiceImpl();
            _provider.RegisterService<ITestService>(service);
            _provider.RegisterService<TestServiceImpl>(service);
            var results = _provider.GetServices<ITestService>().ToList();
            Assert.That(results, Has.Count.EqualTo(1));
        }

        [Test]
        public void GetServices_ShouldIncludeParentServices()
        {
            var parentViewModel = new TestViewModel();
            parentViewModel.Services.RegisterService<ITestService>(new TestServiceImpl());
            var childProvider = new ServiceProvider(parentViewModel);
            childProvider.RegisterService<ITestService>(new AnotherServiceImpl(), "ChildService");
            var results = childProvider.GetServices<ITestService>().ToList();
            Assert.That(results, Has.Count.EqualTo(2));
        }

        [Test]
        public void GetServices_ShouldNotIncludeDuplicatesFromParent()
        {
            var service = new TestServiceImpl();
            var parentViewModel = new TestViewModel();
            parentViewModel.Services.RegisterService<ITestService>(service);
            var childProvider = new ServiceProvider(parentViewModel);
            childProvider.RegisterService<ITestService>(service);
            var results = childProvider.GetServices<ITestService>().ToList();
            Assert.That(results, Has.Count.EqualTo(1));
        }

        #endregion

        #region Unregistration

        [Test]
        public void UnregisterService_ShouldRemoveService()
        {
            var service = new TestServiceImpl();
            _provider.RegisterService<ITestService>(service);

            var before = _provider.GetService<ITestService>();
            var removed = _provider.UnregisterService<ITestService>();
            var after = _provider.GetService<ITestService>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(before, Is.SameAs(service));
                Assert.That(removed, Is.True);
                Assert.That(after, Is.Null);
            }
        }

        [Test]
        public void UnregisterService_WithName_ShouldRemoveNamedService()
        {
            var service = new TestServiceImpl();
            _provider.RegisterService<ITestService>(service, "MyService");

            var removed = _provider.UnregisterService<ITestService>("MyService");
            var result = _provider.GetService<ITestService>("MyService");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(removed, Is.True);
                Assert.That(result, Is.Null);
            }
        }

        [Test]
        public void UnregisterService_ByInstance_ShouldRemoveService()
        {
            var service = new TestServiceImpl();
            _provider.RegisterService<ITestService>(service);

            var before = _provider.GetService<ITestService>();
            var removed = _provider.UnregisterService(service);
            var after = _provider.GetService<ITestService>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(before, Is.SameAs(service));
                Assert.That(removed, Is.True);
                Assert.That(after, Is.Null);
            }
        }

        [Test]
        public void UnregisterService_ByInstance_FactoryRegistration_ShouldNotFind()
        {
            var service = new TestServiceImpl();
            _provider.RegisterService<ITestService>(() => service);
            var removed = _provider.UnregisterService(service);
            Assert.That(removed, Is.False);
        }

        [Test]
        public void UnregisterService_ByName_ShouldRemoveNamedService()
        {
            var service1 = new TestServiceImpl();
            var service2 = new AnotherServiceImpl();

            _provider.RegisterService<ITestService>(service1, "Service1");
            _provider.RegisterService<ITestService>(service2, "Service2");

            var removed = _provider.UnregisterService<ITestService>("Service1");
            var remaining1 = _provider.GetService<ITestService>("Service1");
            var remaining2 = _provider.GetService<ITestService>("Service2");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(removed, Is.True);
                Assert.That(remaining1, Is.Null);
                Assert.That(remaining2, Is.SameAs(service2));
            }
        }

        #endregion

        #region Multiple Services

        [Test]
        public void GetService_WhenMultipleUnnamed_ShouldReturnLastRegistered()
        {
            var first = new TestServiceImpl();
            var second = new AnotherServiceImpl();

            _provider.RegisterService<ITestService>(first);
            _provider.RegisterService<ITestService>(second);

            var result = _provider.GetService<ITestService>();
            Assert.That(result, Is.SameAs(second));
        }

        [Test]
        public void GetServices_ShouldReturnAllNamedServices()
        {
            var services = new ITestService[]
            {
                new TestServiceImpl(),
                new AnotherServiceImpl(),
                new TestServiceImpl()
            };

            _provider.RegisterService(services[0], "Service1");
            _provider.RegisterService(services[1], "Service2");
            _provider.RegisterService(services[2], "Service3");

            var results = _provider.GetServices<ITestService>().ToList();

            Assert.That(results, Has.Count.EqualTo(3));
            Assert.That(results, Is.EquivalentTo(services));
        }

        #endregion

        #region Default Provider

        [Test]
        public void Default_ShouldBeAccessibleGlobally()
        {
            var service = new TestServiceImpl();
            ServiceProvider.Default.RegisterService<ITestService>(service);
            var result = ServiceProvider.Default.GetService<ITestService>();
            Assert.That(result, Is.SameAs(service));
        }

        [Test]
        public void Default_CanBeOverridden()
        {
            var customProvider = new ServiceProvider();
            var service = new TestServiceImpl();
            customProvider.RegisterService<ITestService>(service);

            ServiceProvider.Default = customProvider;
            var result = ServiceProvider.Default.GetService<ITestService>();

            Assert.That(result, Is.SameAs(service));
        }

        #endregion

        #region Edge Cases

        [Test]
        public void GetService_NullServiceType_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _provider.GetService(null!, null));
        }

        [Test]
        public void RegisterService_DuplicateWithThrowIfExists_ShouldThrow()
        {
            _provider.RegisterService<ITestService>(new TestServiceImpl());
            Assert.Throws<InvalidOperationException>(() =>
                _provider.RegisterService<ITestService>(new AnotherServiceImpl(), throwIfExists: true));
        }

        [Test]
        public void RegisterService_DuplicateWithoutThrowIfExists_ShouldReplace()
        {
            var first = new TestServiceImpl();
            var second = new AnotherServiceImpl();

            _provider.RegisterService<ITestService>(first);
            _provider.RegisterService<ITestService>(second, throwIfExists: false);
            var result = _provider.GetService<ITestService>();

            Assert.That(result, Is.SameAs(second));
        }

        [Test]
        public void Clear_ShouldRemoveAllServices()
        {
            _provider.RegisterService<ITestService>(new TestServiceImpl());
            _provider.RegisterService<IOtherService>(new OtherServiceImpl(), "Named");
            _provider.Clear();

            var testService = _provider.GetService<ITestService>();
            var otherService = _provider.GetService<IOtherService>("Named");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(testService, Is.Null);
                Assert.That(otherService, Is.Null);
            }
        }

        #endregion
    }

    internal class TestViewModel : ViewModelBase
    {
        public TestViewModel() { }
    }
}
