namespace Minimal.Mvvm.Tests
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class AsyncCommandTTests
    {
        [Test]
        public void Constructor_WithExecuteAndCanExecute_InitializesCorrectly()
        {
            // Arrange & Act
            var command = new AsyncCommand<int>(
                (param, ct) => Task.CompletedTask,
                param => param > 0);

            // Assert
            Assert.That(command, Is.Not.Null);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(command.AllowConcurrentExecution, Is.False);
                Assert.That(command.ContinueOnCapturedContext, Is.True);
            }
        }

        [Test]
        public void Constructor_WithExecuteOnly_AlwaysCanExecute()
        {
            // Arrange & Act
            var command = new AsyncCommand<int>((param, ct) => Task.CompletedTask);

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(command.CanExecute(42), Is.True);
                Assert.That(command.CanExecute(-1), Is.True);
                Assert.That(command.CanExecute(0), Is.True);
            }
        }

        [Test]
        public void Constructor_WithFuncTaskOnly_CanExecuteAlwaysTrue()
        {
            // Arrange & Act
            var command = new AsyncCommand<int>(param => Task.CompletedTask);

            // Assert
            Assert.That(command.CanExecute(42), Is.True);
        }

        [Test]
        public void Constructor_NullExecute_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AsyncCommand<int>((Func<int, CancellationToken, Task>)null!));
            Assert.Throws<ArgumentNullException>(() => new AsyncCommand<int>((Func<int, CancellationToken, Task>)null!, null));
            Assert.Throws<ArgumentNullException>(() => new AsyncCommand<int>((Func<int, Task>)null!));
            Assert.Throws<ArgumentNullException>(() => new AsyncCommand<int>((Func<int, Task>)null!, null));
        }

        [Test]
        public async Task ExecuteAsync_CompletesSuccessfully()
        {
            // Arrange
            var executed = false;
            var command = new AsyncCommand<int>(async (param, ct) =>
            {
                await Task.Delay(50, ct);
                executed = true;
            });

            // Act
            await command.ExecuteAsync(42);

            // Assert
            Assert.That(executed, Is.True);
        }

        [Test]
        public void Execute_AsyncVoid_RaisesUnhandledExceptionOnError()
        {
            // Arrange
            var exception = new InvalidOperationException("Test");
            var command = new AsyncCommand<int>((param, ct) => Task.FromException(exception));

            Exception? capturedException = null;
            command.UnhandledException += (s, e) =>
            {
                capturedException = e.Exception;
            };

            // Act
            command.Execute(42);
            Thread.Sleep(100); // Allow time for async void to process

            // Assert
            Assert.That(capturedException, Is.SameAs(exception));
        }

        [Test]
        public async Task ExecuteAsync_WithCancellationToken_PropagatesCancellation()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            var wasCancelled = false;
            var command = new AsyncCommand<int>(async (param, ct) =>
            {
                try
                {
                    await Task.Delay(1000, ct);
                }
                catch (OperationCanceledException)
                {
                    wasCancelled = true;
                    throw;
                }
            });

            // Act
            var executeTask = command.ExecuteAsync(42, cts.Token);
            await Task.Delay(50);
            cts.Cancel();

            // Assert
            Assert.ThrowsAsync<TaskCanceledException>(async () => await executeTask);
            Assert.That(wasCancelled, Is.True);
        }

        [Test]
        public async Task Cancel_CancelsAllExecutingTasks()
        {
            // Arrange
            var cancelledCount = 0;
            var command = new AsyncCommand<int>(async (param, ct) =>
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
            var task1 = command.ExecuteAsync(1);
            var task2 = command.ExecuteAsync(2);

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

        [Test]
        public void CanExecute_WithCustomDelegate_RespectsDelegate()
        {
            // Arrange
            var canExecute = false;
            var command = new AsyncCommand<int>(
                (param, ct) => Task.CompletedTask,
                param => canExecute);

            // Act & Assert
            Assert.That(command.CanExecute(42), Is.False);

            canExecute = true;
            command.RaiseCanExecuteChanged();
            Assert.That(command.CanExecute(42), Is.True);
        }

        [Test]
        public void CanExecute_WhenConcurrentExecutionDisabledAndExecuting_ReturnsFalse()
        {
            // Arrange
            var command = new AsyncCommand<int>(async (param, ct) =>
            {
                await Task.Delay(100, ct);
            });

            // Act
            command.Execute(42);
            var canExecute = command.CanExecute(42);

            // Assert
            Assert.That(canExecute, Is.False);
        }

        [Test]
        public void CanExecute_WhenConcurrentExecutionEnabledAndExecuting_ReturnsTrue()
        {
            // Arrange
            var command = new AsyncCommand<int>(async (param, ct) =>
            {
                await Task.Delay(100, ct);
            })
            {
                AllowConcurrentExecution = true
            };

            // Act
            command.Execute(42);
            var canExecute = command.CanExecute(42);

            // Assert
            Assert.That(canExecute, Is.True);
        }

        [Test]
        public async Task ExecuteAsync_WithoutCancellationToken_Works()
        {
            // Arrange
            var executed = false;
            var command = new AsyncCommand<int>(param =>
            {
                executed = true;
                return Task.CompletedTask;
            });

            // Act
            await command.ExecuteAsync(42);

            // Assert
            Assert.That(executed, Is.True);
        }

        [Test]
        public async Task ContinueOnCapturedContext_False_DoesNotCaptureContext()
        {
            SynchronizationContext? context = null;
            // Arrange
            var command = new AsyncCommand<int>(async (param, ct) =>
            {
                await Task.Delay(1, ct);
                context = SynchronizationContext.Current;
            })
            {
                ContinueOnCapturedContext = false
            };

            // Act
            await command.ExecuteAsync(42);

            // Assert
            Assert.That(context, Is.Null);
        }

        [Test]
        public async Task ContinueOnCapturedContext_True_CapturesContextWhenAvailable()
        {
            // Arrange
            var context = SynchronizationContext.Current;
            SynchronizationContext? result = null;
            var command = new AsyncCommand<int>(async (param, ct) =>
            {
                await Task.Delay(1, ct);
                result = SynchronizationContext.Current;
            })
            {
                ContinueOnCapturedContext = true
            };

            // Act
            await command.ExecuteAsync(42);

            // Assert
            Assert.That(result, Is.EqualTo(context));
        }

        [Test]
        public async Task PropertyChanged_IsExecuting_RaisesEvents()
        {
            // Arrange
            var command = new AsyncCommand<int>(async (param, ct) =>
            {
                await Task.Delay(200, ct);
            });

            var propertyChanges = new List<string>();
            ((System.ComponentModel.INotifyPropertyChanged)command).PropertyChanged += (s, e) =>
            {
                propertyChanges.Add(e.PropertyName!);
            };

            // Act
            var executeTask = command.ExecuteAsync(42);
            await Task.Delay(50); // Allow time to start execution

            var isExecutingDuring = command.IsExecuting;

            await executeTask;
            var isExecutingAfter = command.IsExecuting;

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(isExecutingDuring, Is.True);
                Assert.That(isExecutingAfter, Is.False);
                Assert.That(propertyChanges, Contains.Item(nameof(command.IsExecuting)));
            }
        }

        [Test]
        public void CanExecuteChanged_RaisesEvent()
        {
            // Arrange
            var command = new AsyncCommand<int>((param, ct) => Task.CompletedTask);
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
        [CancelAfter(3000)]
        public async Task MultipleConcurrentExecutions_WhenAllowed()
        {
            // Arrange
            var executionCount = 0;
            var maxConcurrent = 0;
            var currentConcurrent = 0;

            var command = new AsyncCommand<int>(async (param, ct) =>
            {
                var current = Interlocked.Increment(ref currentConcurrent);
                var oldMax = maxConcurrent;
                while (current > oldMax)
                {
                    Interlocked.CompareExchange(ref maxConcurrent, current, oldMax);
                    oldMax = maxConcurrent;
                }

                await Task.Delay(100, ct);

                Interlocked.Decrement(ref currentConcurrent);
                Interlocked.Increment(ref executionCount);
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
                Assert.That(maxConcurrent, Is.GreaterThan(1));
                Assert.That(command.IsExecuting, Is.False);
            }
        }

        [Test]
        public async Task ExecuteAsyncCore_UsesConfigureAwaitFalse()
        {
            // Arrange
            SynchronizationContext? capturedContext = null;
            var command = new AsyncCommand<int>(async (param, ct) =>
            {
                await Task.Delay(1, ct);
                capturedContext = SynchronizationContext.Current;
            });

            // Act
            await command.ExecuteAsync(42);

            // Assert
            Assert.That(capturedContext, Is.Null);
        }

        [Test]
        public void GetCommandParameter_Cast_IntToString()
        {
            // Arrange
            var command = new AsyncCommand<string>((param, ct) => Task.CompletedTask);

            // Act & Assert
            var result = command.GetCommandParameter(42);
            Assert.That(result, Is.EqualTo("42"));
        }

        [Test]
        public void GetCommandParameter_ValidCast_ReturnsValue()
        {
            // Arrange
            var command = new AsyncCommand<int>((param, ct) => Task.CompletedTask);

            // Act
            var result = command.GetCommandParameter((object)42);

            // Assert
            Assert.That(result, Is.EqualTo(42));
        }
    }
}