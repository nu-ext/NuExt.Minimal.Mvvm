using Minimal.Mvvm;

namespace Minimal.Mvvm.Tests
{
    internal class ViewModelBaseTests
    {
        [Test]
        public async Task ServicesTestAsync()
        {
            ServiceProvider.Default.RegisterService(new MyServiceBase("Default"));

            var parentVm = new MyParentViewModel();
            parentVm.Services.RegisterService(new MyServiceBase("Parent"));
            await parentVm.InitializeAsync();

            var vm = new MyViewModel
            {
                ParentViewModel = parentVm
            };
            vm.Services.RegisterService(new MyServiceBase("Current"));
            vm.Services.RegisterService(new MyDerivedService("Derived"));
            await vm.InitializeAsync();

            var services = vm.Services.GetServices(typeof(MyServiceBase));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(services, Is.Not.Null);
                Assert.That(services.Count(), Is.EqualTo(3));
            }

            var service = vm.Services.GetService<MyServiceBase>();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(service, Is.Not.Null);
                Assert.That(service!.Data, Is.EqualTo("Current"));
            }
            service = vm.GetService<MyServiceBase>();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(service, Is.Not.Null);
                Assert.That(service!.Data, Is.EqualTo("Current"));
            }

            vm.Services.UnregisterService<MyServiceBase>();

            service = vm.Services.GetService<MyServiceBase>();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(service, Is.Not.Null);
                Assert.That(service!.Data, Is.EqualTo("Derived"));
            }
            service = vm.GetService<MyServiceBase>();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(service, Is.Not.Null);
                Assert.That(service!.Data, Is.EqualTo("Derived"));
            }
            vm.Services.UnregisterService<MyDerivedService>();

            service = vm.Services.GetService<MyServiceBase>();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(service, Is.Not.Null);
                Assert.That(service!.Data, Is.EqualTo("Parent"));
            }
            service = vm.GetService<MyServiceBase>();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(service, Is.Not.Null);
                Assert.That(service!.Data, Is.EqualTo("Parent"));
            }

            parentVm.Services.UnregisterService<MyServiceBase>();

            service = vm.Services.GetService<MyServiceBase>();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(service, Is.Not.Null);
                Assert.That(service!.Data, Is.EqualTo("Default"));
            }

            service = vm.GetService<MyServiceBase>();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(service, Is.Not.Null);
                Assert.That(service!.Data, Is.EqualTo("Default"));
            }

            await vm.UninitializeAsync();
            await parentVm.UninitializeAsync();
        }

        internal class MyViewModel : ViewModelBase
        {

        }

        internal class MyParentViewModel : ViewModelBase
        {

        }

        internal class MyServiceBase(string data)
        {
            public string Data { get; set; } = data;
        }

        internal class MyDerivedService(string data) : MyServiceBase(data)
        {
        }
    }
}
