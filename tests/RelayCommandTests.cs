using System.ComponentModel;

using static NUnit.Framework.TestContext;

namespace Minimal.Mvvm.Tests
{
    public class RelayCommandTests
    {
        [Test]
        public async Task MultipleExecuteTestAsync()
        {
            int executedCount = 0;
            RelayCommand command = null!;
            command = new RelayCommand(Execute) { AllowConcurrentExecution = true };

            (command as INotifyPropertyChanged).PropertyChanged += OnPropertyChanged;
           
            for (int i = 0; i < 100; i++)
            {
                executedCount = 0;
                Assert.That(command.IsExecuting, Is.False);
                var tasks = new List<Task>();
                int num = 5;
                for (int j = 0; j < num; j++)
                {
                    tasks.Add(Task.Run(() => ExecuteCommand(null)));
                }
                await Task.WhenAll([.. tasks]).ConfigureAwait(false);
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(command.IsExecuting, Is.False);
                    Assert.That(executedCount, Is.EqualTo(num));
                }
            }

            Assert.Pass();

            void Execute()
            {
                Interlocked.Increment(ref executedCount);
                Assert.That(command.IsExecuting, Is.True);
                Progress.WriteLine($"[{command.GetType().Name}] Thread={Environment.CurrentManagedThreadId, -2}, ExecutingCount={command.ExecutingCount}");
            }

            void ExecuteCommand(object? obj)
            {
                command.Execute(obj);
            }

            void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(IRelayCommand.IsExecuting))
                {
                    Progress.WriteLine($"[{command.GetType().Name}] Thread={Environment.CurrentManagedThreadId, -2}, ExecutingCount={command.ExecutingCount}, IsExecuting={command.IsExecuting}");
                }
            }
        }

        [Test]
        public void Constructor_WithExecuteAndCanExecute_InitializesCorrectly()
        {
            var command = new RelayCommand(
                () => { },
                () => true);

            Assert.That(command, Is.Not.Null);
            Assert.That(command.AllowConcurrentExecution, Is.False);
        }

        [Test]
        public void Constructor_WithExecuteOnly_AlwaysCanExecute()
        {
            var command = new RelayCommand(() => { });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(command.CanExecute(null), Is.True);
                Assert.That(command.CanExecute("not null"), Is.True);
            }
        }

        [Test]
        public void Constructor_NullExecute_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new RelayCommand(null!));
            Assert.Throws<ArgumentNullException>(() => new RelayCommand(null!, null));
        }

        [Test]
        public void Execute_InvokesProvidedAction()
        {
            bool executed = false;
            var command = new RelayCommand(() => executed = true);

            command.Execute(null);

            Assert.That(executed, Is.True);
        }

        [Test]
        public void Execute_WhenCanExecuteReturnsFalse_DoesNotInvokeAction()
        {
            bool actionInvoked = false;
            var command = new RelayCommand(
                () => actionInvoked = true,
                () => false);

            command.Execute(null);

            Assert.That(actionInvoked, Is.False);
        }

        [Test]
        public void CanExecute_WithCustomDelegate_RespectsDelegate()
        {
            bool canExecute = false;
            var command = new RelayCommand(
                () => { },
                () => canExecute);

            Assert.That(command.CanExecute(null), Is.False);

            canExecute = true;
            command.RaiseCanExecuteChanged();

            Assert.That(command.CanExecute(null), Is.True);
        }

        [Test]
        public void CanExecute_WithParameter_AlwaysRespectsDelegate()
        {
            var command = new RelayCommand(() => { }, () => true);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(command.CanExecute(null), Is.True);
                Assert.That(command.CanExecute("not null"), Is.True);
                Assert.That(command.CanExecute(42), Is.True);
            }
        }

        [Test]
        public void Execute_WithAllowConcurrentExecutionFalse_PreventsOverlap()
        {
            int executionCount = 0;
            var command = new RelayCommand(() =>
            {
                Interlocked.Increment(ref executionCount);
                Thread.Sleep(500);
            });

            var task1 = Task.Run(() => command.Execute(null));
            Thread.Sleep(10);
            var task2 = Task.Run(() => command.Execute(null));

            Task.WaitAll(task1, task2);

            Assert.That(executionCount, Is.EqualTo(1));
        }

        [Test]
        public void Execute_WithAllowConcurrentExecutionTrue_AllowsOverlap()
        {
            int executionCount = 0;
            var command = new RelayCommand(() =>
            {
                Interlocked.Increment(ref executionCount);
                Thread.Sleep(100);
            })
            {
                AllowConcurrentExecution = true
            };

            var task1 = Task.Run(() => command.Execute(null));
            var task2 = Task.Run(() => command.Execute(null));

            Task.WaitAll(task1, task2);

            Assert.That(executionCount, Is.EqualTo(2));
        }

        [Test]
        public void PropertyChanged_IsExecuting_RaisesEvent()
        {
            var command = new RelayCommand(() => Thread.Sleep(50));

            var propertyChanges = new List<string>();
            ((INotifyPropertyChanged)command).PropertyChanged += (s, e) =>
            {
                propertyChanges.Add(e.PropertyName!);
            };

            command.Execute(null);

            Assert.That(propertyChanges, Contains.Item(nameof(command.IsExecuting)));
        }

        [Test]
        public void RaiseCanExecuteChanged_RaisesEvent()
        {
            var command = new RelayCommand(() => { });
            var eventCount = 0;
            ((System.Windows.Input.ICommand)command).CanExecuteChanged += (s, e) =>
            {
                eventCount++;
            };

            command.RaiseCanExecuteChanged();
            command.RaiseCanExecuteChanged();

            Assert.That(eventCount, Is.EqualTo(2));
        }

        [Test]
        public void Execute_ExceptionInAction_PropagatesToCaller()
        {
            var exception = new InvalidOperationException("Test");
            var command = new RelayCommand(() => throw exception);

            var thrownException = Assert.Throws<InvalidOperationException>(() =>
            {
                command.Execute(null);
            });

            Assert.That(thrownException, Is.SameAs(exception));
        }

        [Test]
        public void ThreadSafety_ConcurrentAccessToCanExecute()
        {
            var command = new RelayCommand(() => { });
            var canExecuteResults = new List<bool>();
            var exceptions = new List<Exception>();

            Parallel.For(0, 1000, i =>
            {
                try
                {
                    var result = command.CanExecute(i % 2 == 0 ? null : "parameter");
                    lock (canExecuteResults)
                    {
                        canExecuteResults.Add(result);
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });

            Assert.That(exceptions, Is.Empty);
        }

        [Test]
        public void CanExecute_WhenConcurrentExecutionDisabledAndExecuting_ReturnsFalse()
        {
            var command = new RelayCommand(() => Thread.Sleep(1000));
            var canExecuteResult = false;

            var executionTask = Task.Run(() => command.Execute(null));

            // Check during execution
            Thread.Sleep(100);
            canExecuteResult = command.CanExecute(null);

            executionTask.Wait();

            Assert.That(canExecuteResult, Is.False);
        }

        [Test]
        [CancelAfter(3000)]
        public void MultipleThreads_StressTest()
        {
            var command = new RelayCommand(() =>
            {
                Thread.Sleep(10);
            })
            {
                AllowConcurrentExecution = true
            };

            var exceptions = new List<Exception>();

            Parallel.For(0, 100, i =>
            {
                try
                {
                    command.Execute(null);
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(exceptions, Is.Empty);
                Assert.That(command.IsExecuting, Is.False);
            }
        }

    }
}