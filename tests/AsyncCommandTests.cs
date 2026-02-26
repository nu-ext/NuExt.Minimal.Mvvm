using System.ComponentModel;
using static NUnit.Framework.TestContext;

namespace Minimal.Mvvm.Tests
{
    internal class AsyncCommandTests
    {
        [Test]
        public async Task MultipleExecuteTestAsync()
        {
            int executedCount = 0;
            AsyncCommand command = null!;
            command = new AsyncCommand(ExecuteAsync) { AllowConcurrentExecution = true };

            (command as INotifyPropertyChanged).PropertyChanged += OnPropertyChanged;

            for (int i = 0; i < 100; i++)
            {
                executedCount = 0;
                Assert.That(command.IsExecuting, Is.False);
                int num = 5;
                for (int j = 0; j < num; j++)
                {
                    _ = Task.Run(() => command.Execute(null));
                }
                while (Interlocked.CompareExchange(ref executedCount, 0, 0) != num)
                {
                    await Task.Delay(10).ConfigureAwait(false);
                }
                while (!command.ExecutingTasks.IsEmpty)
                {
                    await Task.Delay(10).ConfigureAwait(false);
                }
                await command.WaitAsync();
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(command.IsExecuting, Is.False);
                    Assert.That(executedCount, Is.EqualTo(num));
                }
            }

            Assert.Pass();

            async Task ExecuteAsync()
            {
                Interlocked.Increment(ref executedCount);
                Assert.That(command.IsExecuting, Is.True);
                await Progress.WriteLineAsync($"[{command.GetType().Name}] Thread={Environment.CurrentManagedThreadId,-2}, ExecutingCount={command.ExecutingCount}");
            }

            void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(IRelayCommand.IsExecuting))
                {
                    Progress.WriteLine($"[{command.GetType().Name}] Thread={Environment.CurrentManagedThreadId,-2}, ExecutingCount={command.ExecutingCount}, IsExecuting={command.IsExecuting}");
                }
            }
        }

        [Test]
        public async Task CancelMultipleExecuteTestAsync()
        {
            int executedCount = 0;
            AsyncCommand command = null!;
            command = new AsyncCommand(ExecuteAsync) { AllowConcurrentExecution = true };

            (command as INotifyPropertyChanged).PropertyChanged += OnPropertyChanged;

            for (int i = 0; i < 100; i++)
            {
                await Progress.WriteLineAsync($"[{command.GetType().Name}, {i}] Thread={Environment.CurrentManagedThreadId,-2}, ExecutingCount={command.ExecutingCount}, IsExecuting={command.IsExecuting}");
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(executedCount, Is.Zero);
                    Assert.That(command.ExecutingCount, Is.Zero);
                    Assert.That(command.IsExecuting, Is.False);
                }
                int num = 5;
                for (int j = 0; j < num; j++)
                {
                    _ = Task.Run(() => command.Execute(null));
                }
                while (Interlocked.CompareExchange(ref executedCount, 0, 0) != num)
                {
                    await Task.Delay(10).ConfigureAwait(false);
                }
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(executedCount, Is.EqualTo(num));
                    Assert.That(command.ExecutingCount, Is.EqualTo(num));
                    Assert.That(command.IsExecuting, Is.True);
                }
                command.Cancel();
                await command.WaitAsync();
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(executedCount, Is.Zero);
                    Assert.That(command.ExecutingCount, Is.Zero);
                    Assert.That(command.IsExecuting, Is.False);
                }
            }

            Assert.Pass();

            async Task ExecuteAsync(CancellationToken ct)
            {
                Interlocked.Increment(ref executedCount);
                await Task.Yield();
                Assert.That(ct.CanBeCanceled, Is.True);
                try
                {
                    ct.ThrowIfCancellationRequested();
                    for (int i = 0; i < 100; i++)
                    {
                        Assert.That(i, Is.LessThan(50));
                        ct.ThrowIfCancellationRequested();
                        await Task.Delay(100, ct).ConfigureAwait(false);
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref executedCount);
                }
            }

            void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(IRelayCommand.IsExecuting))
                {
                    Progress.WriteLine($"[{command.GetType().Name}] Thread={Environment.CurrentManagedThreadId,-2}, ExecutingCount={command.ExecutingCount}, IsExecuting={command.IsExecuting}");
                }
            }
        }

        [Test]
        public async Task CancelMultipleExecuteAsyncTestAsync()
        {
            int executedCount = 0;
            AsyncCommand command = null!;
            command = new AsyncCommand(ExecuteAsync) { AllowConcurrentExecution = true };

            (command as INotifyPropertyChanged).PropertyChanged += OnPropertyChanged;

            for (int i = 0; i < 100; i++)
            {
                await Progress.WriteLineAsync($"[{command.GetType().Name}, {i}] Thread={Environment.CurrentManagedThreadId,-2}, ExecutingCount={command.ExecutingCount}, IsExecuting={command.IsExecuting}");
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(executedCount, Is.Zero);
                    Assert.That(command.ExecutingCount, Is.Zero);
                    Assert.That(command.IsExecuting, Is.False);
                }
                int num = 5;
                for (int j = 0; j < num; j++)
                {
                    _ = Task.Run(async () => await command.ExecuteAsync(null));
                }
                while (Interlocked.CompareExchange(ref executedCount, 0, 0) != num)
                {
                    await Task.Delay(10).ConfigureAwait(false);
                }
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(executedCount, Is.EqualTo(num));
                    Assert.That(command.ExecutingCount, Is.EqualTo(num));
                    Assert.That(command.IsExecuting, Is.True);
                }
                command.Cancel();
                await command.WaitAsync();
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(executedCount, Is.Zero);
                    Assert.That(command.ExecutingCount, Is.Zero);
                    Assert.That(command.IsExecuting, Is.False);
                }
            }

            Assert.Pass();

            async Task ExecuteAsync(CancellationToken ct)
            {
                Interlocked.Increment(ref executedCount);
                await Task.Yield();
                Assert.That(ct.CanBeCanceled, Is.True);
                try
                {
                    ct.ThrowIfCancellationRequested();
                    for (int i = 0; i < 100; i++)
                    {
                        Assert.That(i, Is.LessThan(50));
                        ct.ThrowIfCancellationRequested();
                        await Task.Delay(100, ct).ConfigureAwait(false);
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref executedCount);
                }
            }

            void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(IRelayCommand.IsExecuting))
                {
                    Progress.WriteLine($"[{command.GetType().Name}] Thread={Environment.CurrentManagedThreadId,-2}, ExecutingCount={command.ExecutingCount}, IsExecuting={command.IsExecuting}");
                }
            }
        }

        [Test]
        public void Constructor_WithExecuteAndCanExecute_InitializesCorrectly()
        {
            // Arrange & Act
            var command = new AsyncCommand(
                ct => Task.CompletedTask,
                () => true);

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
            var command = new AsyncCommand(ct => Task.CompletedTask);

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(command.CanExecute(null), Is.True);
                Assert.That(command.CanExecute("not null"), Is.True);
            }
        }

        [Test]
        public void Constructor_WithFuncTaskOnly_CanExecuteAlwaysTrue()
        {
            // Arrange & Act
            var command = new AsyncCommand(() => Task.CompletedTask);

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(command.CanExecute(null), Is.True);
                Assert.That(command.CanExecute("not null"), Is.True);
            }
        }

        [Test]
        public void Constructor_NullExecute_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AsyncCommand((Func<CancellationToken, Task>)null!));
            Assert.Throws<ArgumentNullException>(() => new AsyncCommand((Func<CancellationToken, Task>)null!, null));
            Assert.Throws<ArgumentNullException>(() => new AsyncCommand((Func<Task>)null!));
            Assert.Throws<ArgumentNullException>(() => new AsyncCommand((Func<Task>)null!, null));
        }

        [Test]
        public async Task ExecuteAsync_CompletesSuccessfully()
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
        public void Execute_AsyncVoid_RaisesUnhandledExceptionOnError()
        {
            // Arrange
            var exception = new InvalidOperationException("Test");
            var command = new AsyncCommand(ct => Task.FromException(exception));

            Exception? capturedException = null;
            command.UnhandledException += (s, e) =>
            {
                capturedException = e.Exception;
            };

            // Act
            command.Execute(null);
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
            var command = new AsyncCommand(async ct =>
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
            var executeTask = command.ExecuteAsync(null, cts.Token);
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

        [Test]
        public void CanExecute_WithParameter_ReturnsTrueForNonNull()
        {
            // Arrange
            var command = new AsyncCommand(ct => Task.CompletedTask);

            using (Assert.EnterMultipleScope())
            {
                // Act & Assert
                Assert.That(command.CanExecute(null), Is.True);
                Assert.That(command.CanExecute("not null"), Is.True);
                Assert.That(command.CanExecute(42), Is.True);
                Assert.That(command.CanExecute(new object()), Is.True);
            }
        }

        [Test]
        public void CanExecute_WithCustomDelegate_RespectsDelegate()
        {
            // Arrange
            var canExecute = false;
            var command = new AsyncCommand(
                ct => Task.CompletedTask,
                () => canExecute);

            // Act & Assert
            Assert.That(command.CanExecute(null), Is.False);

            canExecute = true;
            command.RaiseCanExecuteChanged();
            Assert.That(command.CanExecute(null), Is.True);
        }

        [Test]
        public async Task ExecuteAsync_WithoutCancellationToken_Works()
        {
            // Arrange
            var executed = false;
            var command = new AsyncCommand(() =>
            {
                executed = true;
                return Task.CompletedTask;
            });

            // Act
            await command.ExecuteAsync(null);

            // Assert
            Assert.That(executed, Is.True);
        }

        [Test]
        public async Task ContinueOnCapturedContext_False_DoesNotCaptureContext()
        {
            SynchronizationContext? context = null;
            // Arrange
            var command = new AsyncCommand(async ct =>
            {
                await Task.Delay(1, ct);
                context = SynchronizationContext.Current;
            })
            {
                ContinueOnCapturedContext = false
            };

            // Act
            await command.ExecuteAsync(null);

            // Assert
            Assert.That(context, Is.Null);
        }

        [Test]
        public async Task PropertyChanged_IsExecuting_RaisesEvents()
        {
            // Arrange
            var command = new AsyncCommand(async ct =>
            {
                await Task.Delay(500, ct);
            });

            var propertyChanges = new List<string>();
            ((INotifyPropertyChanged)command).PropertyChanged += (s, e) =>
            {
                propertyChanges.Add(e.PropertyName!);
            };

            // Act
            var executeTask = command.ExecuteAsync(null);
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
        [CancelAfter(3000)]
        public async Task MultipleConcurrentExecutions_WhenAllowed()
        {
            // Arrange
            var executionCount = 0;
            var maxConcurrent = 0;
            var currentConcurrent = 0;

            var command = new AsyncCommand(async ct =>
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
                .Select(_ => command.ExecuteAsync(null))
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
        public void NonGenericExecuteAsync_WithObjectParameter_Works()
        {
            // Arrange
            var executed = false;
            var command = new AsyncCommand(async ct =>
            {
                await Task.Delay(50, ct);
                executed = true;
            });

            // Act & Assert
            Assert.DoesNotThrowAsync(async () => await ((IAsyncCommand)command).ExecuteAsync(null));
            Assert.That(executed, Is.True);
        }

        [Test]
        public void NonGenericExecuteAsync_WithCancellationToken_Works()
        {
            // Arrange
            var executed = false;
            var command = new AsyncCommand(async ct =>
            {
                await Task.Delay(50, ct);
                executed = true;
            });

            // Act & Assert
            Assert.DoesNotThrowAsync(async () =>
                await ((IAsyncCommand)command).ExecuteAsync(null, CancellationToken.None));
            Assert.That(executed, Is.True);
        }

        [Test]
        public void CanExecute_WithNonNullParameter_ReturnsTrueEvenIfCanExecuteDelegateReturnsTrue()
        {
            // Arrange
            var command = new AsyncCommand(
                ct => Task.CompletedTask,
                () => true); // The delegate returns true

            // Act & Assert
            Assert.That(command.CanExecute("some parameter"), Is.True);
        }

        [Test]
        public void ThreadSafety_ConcurrentAccessToCanExecute()
        {
            // Arrange
            var command = new AsyncCommand(ct => Task.CompletedTask);
            var canExecuteResults = new List<bool>();
            var exceptions = new List<Exception>();

            // Act
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

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(exceptions, Is.Empty);
                Assert.That(canExecuteResults.All(r => r == true || r == false), Is.True);
            }
        }

        [Test]
        public async Task AnonymousMethodsTestAsync()
        {
            AsyncCommand command = null!;
            command = new AsyncCommand(async () => { await Task.Delay(0); });
            await command.ExecuteAsync(null);

            Assert.That(command.IsExecuting, Is.False);
        }

        [Test]
        public async Task AsyncCommand_ExecuteAsync_CompletesSuccessfully()
        {
            // Arrange
            var executionCount = 0;
            var command = new AsyncCommand<int>(async (param, ct) =>
            {
                await Task.Delay(50, ct);
                Interlocked.Increment(ref executionCount);
            });

            // Act
            await command.ExecuteAsync(42);

            // Assert
            Assert.That(executionCount, Is.EqualTo(1));
        }

        [Test]
        public void AsyncCommand_Execute_WithException_RaisesUnhandledExceptionEvent()
        {
            // Arrange
            var exception = new InvalidOperationException("Test exception");
            var command = new AsyncCommand<int>((param, ct) =>
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
        public async Task AsyncCommand_Cancel_StopsAllExecutingTasks()
        {
            // Arrange
            var tasksStarted = 0;
            var tasksCancelled = 0;
            var command = new AsyncCommand<int>(async (param, ct) =>
            {
                Interlocked.Increment(ref tasksStarted);
                try
                {
                    await Task.Delay(1000, ct);
                }
                catch (TaskCanceledException)
                {
                    Interlocked.Increment(ref tasksCancelled);
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
            Assert.That(tasksStarted, Is.EqualTo(2));

            try
            {
                await Task.WhenAll(task1, task2);
            }
            catch (OperationCanceledException) { }

            Assert.That(tasksCancelled, Is.EqualTo(2));
        }

        [Test]
        public async Task AsyncCommand_WithCancellationToken_PropagatesCancellation()
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
        public void AsyncCommand_CanExecute_WithNullCanExecuteDelegate_ReturnsTrue()
        {
            // Arrange
            var command = new AsyncCommand<int>((param, ct) => Task.CompletedTask);

            // Act & Assert
            Assert.That(command.CanExecute(42), Is.True);
        }

        [Test]
        public void AsyncCommand_CanExecute_WithCustomDelegate_RespectsDelegate()
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
        public async Task AsyncCommand_WithoutCancellationToken_StillExecutes()
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
        public async Task AsyncCommand_ContinueOnCapturedContext_Respected()
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
    }
}
