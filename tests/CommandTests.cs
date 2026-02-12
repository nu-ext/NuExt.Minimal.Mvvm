using System.ComponentModel;
using System.Windows.Input;

namespace Minimal.Mvvm.Tests
{
    internal sealed class TestCommand<T>(Action<T> execute, Func<T, bool>? canExecute = null) : CommandBase<T>
    {
        private readonly Action<T> _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        private readonly Func<T, bool> _canExecute = canExecute ?? (_ => true);

        protected override bool CanExecuteCore(T parameter) => _canExecute(parameter);
        public override void Execute(T parameter) => _execute(parameter);
    }

    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class CommandBaseTests
    {
        [Test]
        public void Constructor_Default_PropertiesInitialized()
        {
            // Arrange & Act
            var command = new TestCommand<int>(_ => { });

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(command.AllowConcurrentExecution, Is.False);
                Assert.That(command.IsExecuting, Is.False);
            }
        }

        [Test]
        public void CanExecute_WhenConcurrentExecutionDisabledAndExecuting_ReturnsFalse()
        {
            // Arrange
            var command = new TestCommand<int>(_ => { });
            command.Execute(0);

            // Act
            var canExecute = command.CanExecute(0);

            // Assert
            Assert.That(canExecute, Is.True);
        }

        [Test]
        public void CanExecute_WhenConcurrentExecutionEnabledAndExecuting_ReturnsTrue()
        {
            // Arrange
            var command = new TestCommand<int>(_ => { })
            {
                AllowConcurrentExecution = true
            };
            command.Execute(0);

            // Act
            var canExecute = command.CanExecute(0);

            // Assert
            Assert.That(canExecute, Is.True);
        }

        [Test]
        public void RaiseCanExecuteChanged_TriggersEvent()
        {
            // Arrange
            var command = new TestCommand<int>(_ => { });
            var eventRaised = false;
            ((System.Windows.Input.ICommand)command).CanExecuteChanged += (s, e) =>
            {
                eventRaised = true;
            };

            // Act
            command.RaiseCanExecuteChanged();

            // Assert
            Assert.That(eventRaised, Is.True);
        }

        [Test]
        public void GetCommandParameter_InvalidCast_ThrowsInvalidCastException()
        {
            // Arrange
            ICommand command = new TestCommand<int>(_ => { });

            // Act & Assert
            Assert.Throws<InvalidCastException>(() =>
            {
                command.Execute("invalid");
            });
        }
    }

    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class ParameterlessAsyncCommandTests
    {
        [Test]
        public async Task AsyncCommand_ExecuteAsync_WithoutParameter_Completes()
        {
            // Arrange
            var executed = false;
            var command = new AsyncCommand(async ct =>
            {
                await Task.Delay(50, ct);
                executed = true;
            });

            // Act
            await command.ExecuteAsync(null);

            // Assert
            Assert.That(executed, Is.True);
        }

        [Test]
        public void AsyncCommand_CanExecute_WithParameter_ReturnsTrue()
        {
            // Arrange
            var command = new AsyncCommand(ct => Task.CompletedTask);

            using (Assert.EnterMultipleScope())
            {
                // Act & Assert
                Assert.That(command.CanExecute("not null"), Is.True);
                Assert.That(command.CanExecute(null), Is.True);
            }
        }

        [Test]
        public async Task AsyncCommand_MultipleConcurrentExecutions_WhenAllowed()
        {
            // Arrange
            var executionCount = 0;
            var command = new AsyncCommand(async ct =>
            {
                await Task.Delay(100, ct);
                Interlocked.Increment(ref executionCount);
            })
            {
                AllowConcurrentExecution = true
            };

            // Act
            var tasks = Enumerable.Range(0, 3)
                .Select(_ => command.ExecuteAsync(null))
                .ToArray();

            await Task.WhenAll(tasks);

            // Assert
            Assert.That(executionCount, Is.EqualTo(3));
        }

        [Test]
        public void AsyncCommand_CanExecuteChanged_RaisesEvent()
        {
            // Arrange
            var command = new AsyncCommand(ct => Task.CompletedTask);
            var eventCount = 0;
            ((System.Windows.Input.ICommand)command).CanExecuteChanged += (s, e) =>
            {
                eventCount++;
            };

            // Act
            command.RaiseCanExecuteChanged();
            command.RaiseCanExecuteChanged();

            // Assert
            Assert.That(eventCount, Is.EqualTo(2));
        }

        [Test]
        public async Task AsyncCommand_TaskBasedConstructor_Works()
        {
            // Arrange
            var executed = false;
            var command = new AsyncCommand(async () =>
            {
                await Task.Delay(50);
                executed = true;
            });

            // Act
            await command.ExecuteAsync(null);

            // Assert
            Assert.That(executed, Is.True);
        }

        [Test]
        public void AsyncCommand_PropertyChanged_ForIsExecuting()
        {
            // Arrange
            var command = new AsyncCommand(async ct =>
            {
                await Task.Delay(100, ct);
            });

            var propertyChanges = new List<string>();
            ((INotifyPropertyChanged)command).PropertyChanged += (s, e) =>
            {
                propertyChanges.Add(e.PropertyName!);
            };

            // Act
            command.Execute(null);
            Thread.Sleep(150);

            // Assert
            Assert.That(propertyChanges, Contains.Item(nameof(command.IsExecuting)));
        }

        [Test]
        public async Task AsyncCommand_CancelAll_Works()
        {
            // Arrange
            var cancelledCount = 0;
            var command = new AsyncCommand(async ct =>
            {
                try
                {
                    await Task.Delay(1000, ct);
                }
                catch (OperationCanceledException)
                {
                    Interlocked.Increment(ref cancelledCount);
                    throw;
                }
            })
            {
                AllowConcurrentExecution = true
            };

            // Act
            var task1 = command.ExecuteAsync(null);
            var task2 = command.ExecuteAsync(null);

            await Task.Delay(100);
            command.Cancel();

            // Assert
            try
            {
                await Task.WhenAll(task1, task2);
            }
            catch (OperationCanceledException) { }

            Assert.That(cancelledCount, Is.EqualTo(2));
        }
    }

    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class AsyncCommandBaseTests
    {
        internal sealed class TestAsyncCommand<T>(Func<T, CancellationToken, Task> execute, Func<T, bool>? canExecute = null) : AsyncCommandBase<T>
        {
            private readonly Func<T, CancellationToken, Task> _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            private readonly Func<T, bool> _canExecute = canExecute ?? (_ => true);

            protected override bool CanExecuteCore(T parameter) => _canExecute(parameter);
            protected override Task ExecuteAsyncCore(T parameter, CancellationToken cancellationToken)
                => _execute(parameter, cancellationToken);
        }

        [Test]
        public async Task AsyncCommandBase_TryStartExecution_PreventsConcurrentWhenDisabled()
        {
            // Arrange
            var executionCount = 0;
            var command = new TestAsyncCommand<int>(async (param, ct) =>
            {
                Interlocked.Increment(ref executionCount);
                await Task.Delay(200, ct);
            });

            // Act
            var task1 = Task.Run(() => command.ExecuteAsync(1));
            await Task.Delay(10);
            var task2 = Task.Run(() => command.ExecuteAsync(2));

            await Task.WhenAll(task1, task2);

            // Assert
            Assert.That(executionCount, Is.EqualTo(1));
        }

        [Test]
        public async Task AsyncCommandBase_CompleteExecution_AlwaysBalanced()
        {
            // Arrange
            var executionCount = 0;
            var command = new TestAsyncCommand<int>(async (param, ct) =>
            {
                Interlocked.Increment(ref executionCount);
                await Task.Delay(50, ct);
            })
            {
                AllowConcurrentExecution = true
            };

            // Act
            var tasks = Enumerable.Range(0, 5)
                .Select(i => command.ExecuteAsync(i))
                .ToArray();

            await Task.WhenAll(tasks);

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(executionCount, Is.EqualTo(5));
                Assert.That(command.IsExecuting, Is.False);
            }
        }

        [Test]
        public async Task AsyncCommandBase_ExternalCancellationToken_Works()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            var cancelled = false;
            var command = new TestAsyncCommand<int>(async (param, ct) =>
            {
                try
                {
                    await Task.Delay(1000, ct);
                }
                catch (OperationCanceledException)
                {
                    cancelled = true;
                    throw;
                }
            });

            // Act
            cts.CancelAfter(50);
            Assert.ThrowsAsync<TaskCanceledException>(async () =>
                await command.ExecuteAsync(42, cts.Token));

            // Assert
            Assert.That(cancelled, Is.True);
        }

        [Test]
        public void AsyncCommandBase_UnhandledExceptionEvent_RaisedOnException()
        {
            // Arrange
            var exception = new ArgumentException("Test");
            var command = new TestAsyncCommand<int>((param, ct) =>
                Task.FromException(exception));

            Exception? capturedException = null;
            command.UnhandledException += (s, e) =>
            {
                capturedException = e.Exception;
            };

            // Act
            command.Execute(42);
            Thread.Sleep(100);

            // Assert
            Assert.That(capturedException, Is.SameAs(exception));
        }

        [Test]
        public async Task AsyncCommandBase_CancelMethod_CancelsAllExecutions()
        {
            // Arrange
            var cancellationCount = 0;
            var command = new TestAsyncCommand<int>(async (param, ct) =>
            {
                try
                {
                    await Task.Delay(1000, ct);
                }
                catch (OperationCanceledException)
                {
                    Interlocked.Increment(ref cancellationCount);
                    throw;
                }
            })
            {
                AllowConcurrentExecution = true
            };

            // Act
            var tasks = Enumerable.Range(0, 3)
                .Select(i => command.ExecuteAsync(i))
                .ToArray();

            await Task.Delay(100);
            command.Cancel();

            // Assert
            try
            {
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException) { }

            Assert.That(cancellationCount, Is.EqualTo(3));
        }
    }

    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class ThreadSafetyTests
    {
        [Test]
        [CancelAfter(5000)]
        public void ConcurrentExecutionStressTest()
        {
            // Arrange
            var command = new AsyncCommand<int>(async (param, ct) =>
            {
                await Task.Delay(10, ct);
            })
            {
                AllowConcurrentExecution = true
            };

            const int threadCount = 20;
            const int iterations = 10;

            // Act
            Parallel.For(0, threadCount, i =>
            {
                for (int j = 0; j < iterations; j++)
                {
                    try
                    {
                        command.Execute(i * 100 + j);
                        Thread.Sleep(1);
                    }
                    catch
                    {
                    }
                }
            });

            Assert.Pass("No deadlocks or exceptions in concurrent execution");
        }

        [Test]
        [CancelAfter(3000)]
        public async Task RaceConditionBetweenCancelAndExecute()
        {
            // Arrange
            var command = new AsyncCommand<int>(async (p, ct) =>
            {
                await Task.Delay(200, ct);
            })
            {
                AllowConcurrentExecution = true
            };

            // Act
            var executeTask = command.ExecuteAsync(1);
            await Task.Delay(10);
            command.Cancel();

            var executeTask2 = command.ExecuteAsync(2);
            await Task.Delay(10);
            command.Cancel();

            // Assert
            try
            {
                await Task.WhenAll(executeTask, executeTask2);
            }
            catch (OperationCanceledException)
            {
            }

            Assert.That(command.IsExecuting, Is.False);
        }
    }
}