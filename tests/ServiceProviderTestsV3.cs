using System.Collections.Concurrent;

namespace Minimal.Mvvm.Tests
{
	[TestFixture]
	public class ServiceProviderTestsV3
	{
		#region Helpers

		private sealed class RecordingServiceProvider : ServiceProvider
		{
			public readonly ConcurrentQueue<string> Events = new();

			protected override void OnServiceAdded(object service)
				=> Events.Enqueue($"Added:{service.GetType().Name}");

			protected override void OnServiceActivated(object service)
				=> Events.Enqueue($"Activated:{service.GetType().Name}");

			protected override void OnServiceActivationFailed(object service)
				=> Events.Enqueue($"ActivationFailed:{service.GetType().Name}");

			protected override void OnServiceRemoved(object service)
				=> Events.Enqueue($"Removed:{service.GetType().Name}");
		}

		private sealed class ParentLoopbackProvider(ServiceProvider child) : INamedServiceProvider
        {
            public object? GetService(Type serviceType) => ((IServiceProvider)child).GetService(serviceType);

			public object? GetService(Type serviceType, string? name)
				=> child.GetService(serviceType, name);
		}

		private interface IFoo { }
		private sealed class Foo : IFoo { }
		private sealed class Foo2 : IFoo { }

		private sealed class Bar(int x = 0)
        { 
			public int X = x;
        }

		private sealed class ValueEq(int v) : IFoo
        {
			public int V = v;
            public override bool Equals(object? obj) => obj is ValueEq other && other.V == V;
			public override int GetHashCode() => V;
		}

		#endregion

		#region Resolution priorities

		[Test]
		public void GetService_ExactMatch_Wins()
		{
			var sp = new ServiceProvider();
			var exact = new Foo();
			var other = new Foo2();

			sp.RegisterService(typeof(IFoo), exact, name: "A");
			sp.RegisterService(typeof(IFoo), other, name: "B");

			var resolved = sp.GetService(typeof(IFoo), "A");
			Assert.That(resolved, Is.SameAs(exact));
		}

		[Test]
		public void GetService_NamedFallback_ByAssignableType()
		{
			var sp = new ServiceProvider();
			// No exact key (IFoo,"X"), but there is (Foo,"X")
			sp.RegisterService(typeof(Foo), name: "X", throwIfExists: false);

			var resolved = sp.GetService(typeof(IFoo), "X");
			Assert.That(resolved, Is.InstanceOf<Foo>());
		}

		[Test]
		public void GetService_Unnamed_FallbackSearchesNullNameThenNamed()
		{
			var sp = new ServiceProvider();

			// Unnamed implementation of Foo
			sp.RegisterService(typeof(Foo));
			// Named implementation (should be consulted only after unnamed path)
			sp.RegisterService(typeof(Foo2), name: "N");

			var resolved = sp.GetService(typeof(IFoo), name: null);
			Assert.That(resolved, Is.InstanceOf<Foo>()); // prefers unnamed assignable first
		}

		#endregion

		#region Lazy init / exceptions / single-publication

		[Test]
		public void Factory_ExceptionIsNotCached_RetrySucceeds()
		{
			var sp = new ServiceProvider();
			int attempts = 0;

			sp.RegisterService(typeof(Bar), callback: () =>
			{
				attempts++;
				if (attempts == 1) throw new InvalidOperationException("boom");
				return new Bar(42);
			});

			Assert.Throws<InvalidOperationException>(() => sp.GetService(typeof(Bar)));
			var resolved = sp.GetService(typeof(Bar));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(resolved, Is.InstanceOf<Bar>());
                Assert.That(((Bar)resolved!).X, Is.EqualTo(42));
                Assert.That(attempts, Is.EqualTo(2));
            }
        }

		[Test]
		public async Task Concurrent_GetService_SingleActivation()
		{
			var sp = new ServiceProvider();
			int created = 0;

			sp.RegisterService(typeof(Foo), callback: () =>
			{
				Interlocked.Increment(ref created);
				// A small delay to increase race window
				Thread.Sleep(10);
				return new Foo();
			});

			const int N = 16;
			var tasks = new Task<object?>[N];
			for (int i = 0; i < N; i++)
			{
				tasks[i] = Task.Run(() => sp.GetService(typeof(Foo)));
			}

			var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(created, Is.EqualTo(1), "Factory must run once");
                Assert.That(results, Has.All.Not.Null);
            }
            // All references must be the same
            var first = results[0];
			foreach (var r in results)
			{
				Assert.That(r, Is.SameAs(first));
			}
		}

		#endregion

		#region Circular dependency detection

		[Test]
		public void CircularDependency_Throws_WithReadableChain()
		{
			var sp = new ServiceProvider(); // ThrowOnCircularDependency = true by default

			sp.RegisterService(typeof(string), callback: () =>
			{
				// string depends on int
				return (string)sp.GetService(typeof(int))!;
			});

			sp.RegisterService(typeof(int), callback: () =>
			{
				// int depends on string
				return ((string)sp.GetService(typeof(string))!).Length;
			});

			var ex = Assert.Throws<InvalidOperationException>(() => sp.GetService(typeof(string)));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ex!.Message, Does.Contain("Circular dependency detected"));
                Assert.That(ex.Message, Does.Contain(typeof(string).FullName));
            }
            Assert.That(ex.Message, Does.Contain(typeof(int).FullName));
		}

		#endregion

		#region Parent provider delegation and reentrancy guard

		[Test]
		public void ParentReentrancyGuard_PreventsPingPong()
		{
			var child = new ServiceProvider();
			var parent = new ParentLoopbackProvider(child);
			var childWithParent = new ServiceProvider(parent);

			// Ask childWithParent for an unregistered service: it will delegate to parent,
			// parent calls back into childWithParent (loop). Guard must break the ping-pong and return null.
			var result = childWithParent.GetService(typeof(Foo));
			Assert.That(result, Is.Null);
		}

		[Test]
		public void GetServices_IncludesParent_AndIsDistinctByReference()
		{
			var parent = new ServiceProvider();
			var child = new ServiceProvider(parent);

			var shared = new Foo();
			parent.RegisterService(typeof(IFoo), shared);
			child.RegisterService(typeof(IFoo), shared); // same reference

			var all = new List<object>(child.GetServices(typeof(IFoo)));
			Assert.That(all, Has.Count.EqualTo(1), "Must be distinct by reference (same instance deduped)");

			// Now register two different instances with value equality
			var p2 = new ServiceProvider();
			var c2 = new ServiceProvider(p2);
			var a = new ValueEq(7);
			var b = new ValueEq(7);
			p2.RegisterService(typeof(IFoo), a);
			c2.RegisterService(typeof(IFoo), b);

			var all2 = new List<object>(c2.GetServices(typeof(IFoo)));
			Assert.That(all2, Has.Count.EqualTo(2), "Different references must not be merged even if Equals==true");
		}

		#endregion

		#region throwIfExists / replacement and hooks

		[Test]
		public void RegisterService_Instance_Replaces_WhenThrowIfExistsFalse()
		{
			var sp = new RecordingServiceProvider();

			var a = new Foo();
			var b = new Foo2();

			sp.RegisterService(typeof(IFoo), a, throwIfExists: false);
			sp.RegisterService(typeof(IFoo), b, throwIfExists: false);

			var resolved = sp.GetService(typeof(IFoo));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(resolved, Is.SameAs(b));

                // Instance registration calls OnServiceAdded immediately.
                // Replacement triggers OnServiceRemoved(old) and OnServiceAdded(new).
                Assert.That(sp.Events, Contains.Item("Added:Foo"));
                Assert.That(sp.Events, Contains.Item("Removed:Foo"));
                Assert.That(sp.Events, Contains.Item("Added:Foo2"));
            }
		}

		[Test]
		public void ActivationFailed_Hook_IsCalled_OnTypeMismatch()
		{
			var sp = new RecordingServiceProvider();

			sp.RegisterService(typeof(IFoo), implementationType: typeof(Foo), callback: () => new Bar());

			// On first resolution, a Bar is created but fails compatibility check for IFoo → ActivationFailed hook.
			Assert.Throws<ArgumentException>(() => sp.GetService(typeof(IFoo)));
			bool seen = false;
			while (sp.Events.TryDequeue(out var evt))
			{
				if (evt.StartsWith("ActivationFailed:", StringComparison.Ordinal))
				{
					seen = true; break;
				}
			}
			Assert.That(seen, Is.True, "OnServiceActivationFailed must be raised on post-creation validation failure.");
		}

		#endregion

		#region Unregister / Clear

		[Test]
		public void Unregister_ByInstance_RemovesMatchingKey()
		{
			var sp = new ServiceProvider();
			var inst = new Foo();
			sp.RegisterService(typeof(IFoo), inst);

			var ok = sp.UnregisterService(inst);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ok, Is.True);
                Assert.That(sp.GetService(typeof(IFoo)), Is.Null);
            }
        }

		[Test]
		public void Clear_RemovesAll_AndRaisesRemovedForCreated()
		{
			var sp = new RecordingServiceProvider();

			// Instance registration → created immediately
			var inst = new Foo();
			sp.RegisterService(typeof(IFoo), inst);

			// Factory registration → will create at first resolution
			sp.RegisterService(typeof(Bar), () => new Bar(1));
			_ = sp.GetService(typeof(Bar)); // force creation so that removal hook will fire

			sp.Clear();

			// At least these two should be removed
			int removed = 0;
			foreach (var e in sp.Events)
			{
				if (e.StartsWith("Removed:", StringComparison.Ordinal)) removed++;
			}

            using (Assert.EnterMultipleScope())
            {
                Assert.That(removed, Is.GreaterThanOrEqualTo(2));
                Assert.That(sp.GetService(typeof(IFoo)), Is.Null);
                Assert.That(sp.GetService(typeof(Bar)), Is.Null);
            }
        }

		#endregion

		#region Open generics and compatibility

		[Test]
		public void OpenGeneric_ServiceType_Throws_OnTypeRegistration()
		{
			var sp = new ServiceProvider();
			var ex = Assert.Throws<ArgumentException>(() => sp.RegisterService(typeof(List<>)));
			Assert.That(ex!.ParamName, Is.EqualTo("serviceType"));
		}

		[Test]
		public void OpenGeneric_ServiceType_Throws_OnFactoryRegistration()
		{
			var sp = new ServiceProvider();
			var ex = Assert.Throws<ArgumentException>(() => sp.RegisterService(typeof(List<>), () => new List<int>()));
			Assert.That(ex!.ParamName, Is.EqualTo("serviceType"));
		}

		[Test]
		public void ImplementationType_NotAssignable_Throws_WithParamName()
		{
			var sp = new ServiceProvider();
			var ex = Assert.Throws<ArgumentException>(() => sp.RegisterService(typeof(IFoo), implementationType: typeof(Bar), throwIfExists: false));
			Assert.That(ex!.ParamName, Is.EqualTo("implementationType"));
		}

		[Test]
		public void Instance_NotAssignable_Throws_WithParamName()
		{
			var sp = new ServiceProvider();
			var ex = Assert.Throws<ArgumentException>(() => sp.RegisterService(typeof(IFoo), new Bar()));
			Assert.That(ex!.ParamName, Is.EqualTo("service"));
		}

		#endregion

		#region Dispose idempotency

		[Test]
		public void Dispose_CanBeCalledTwice_WithoutThrowing()
		{
			var sp = new ServiceProvider();
			sp.Dispose();
			Assert.DoesNotThrow(() => sp.Dispose());
		}

        #endregion


        [Test]
        public void GetService_Generic_Unnamed_Works()
        {
            var sp = new ServiceProvider();
            sp.RegisterService(typeof(IFoo), new Foo());

            var foo = sp.GetService<IFoo>();
            Assert.That(foo, Is.Not.Null);
        }

        [Test]
        public void GetService_Generic_Named_Works()
        {
            var sp = new ServiceProvider();
            sp.RegisterService(typeof(IFoo), new Foo(), name: "X");

            var foo = sp.GetService<IFoo>("X");
            Assert.That(foo, Is.Not.Null);
        }

        [Test]
        public void GetRequiredService_Throws_WhenMissing()
        {
            var sp = new ServiceProvider();
            Assert.Throws<InvalidOperationException>(() =>
                sp.GetRequiredService<IFoo>());
        }

        [Test]
        public void GetServices_Generic_Yields_All()
        {
            var sp = new ServiceProvider();
            sp.RegisterService(typeof(IFoo), new Foo());
            sp.RegisterService(typeof(IFoo), new Foo(), name: "A");

            var all = new List<IFoo>(sp.GetServices<IFoo>());

            Assert.That(all, Has.Count.EqualTo(2));
        }

    }
}
