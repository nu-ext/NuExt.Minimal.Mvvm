#if NET5_0_OR_GREATER || NETSTANDARD2_1
using System.Windows.Input;
using static NUnit.Framework.TestContext;

namespace Minimal.Mvvm.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class AsyncValueCommandTests
    {
        [SetUp]
        public void SetUp()
        {
        }

        [TearDown]
        public void TearDown()
        {
        }

        #region AsyncValueCommand<T> Tests

        [Test]
        public void AsyncValueCommandT_Constructor_NullExecute_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new AsyncValueCommand<string>((Func<string, ValueTask>)null!));
        }

        [Test]
        public async Task AsyncValueCommandT_Execute_WithValueTask_Succeeds()
        {
            // Arrange
            bool executed = false;
            var command = new AsyncValueCommand<string>(async (param, ct) =>
            {
                await Task.Delay(10, ct);
                executed = param == "test";
            });

            // Act
            await command.ExecuteAsync("test");

            // Assert
            Assert.That(executed, Is.True);
        }

        [Test]
        public async Task AsyncValueCommandT_Execute_SynchronousValueTask_Succeeds()
        {
            // Arrange
            int executionCount = 0;
            var command = new AsyncValueCommand<string>((param, ct) =>
            {
                executionCount++;
                return ValueTask.CompletedTask;
            });

            // Act
            await command.ExecuteAsync("test");

            // Assert
            Assert.That(executionCount, Is.EqualTo(1));
        }

        [Test]
        public async Task AsyncValueCommandT_Execute_WithCancellationToken_CancelsOperation()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            bool wasCancelled = false;

            var command = new AsyncValueCommand<string>(async (param, ct) =>
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
            cts.CancelAfter(50);
            var executeTask = command.ExecuteAsync("test", cts.Token);

            // Assert
            Assert.ThrowsAsync<TaskCanceledException>(async () => await executeTask);
            Assert.That(wasCancelled, Is.True);
        }

        [Test]
        public void AsyncValueCommandT_CanExecute_WithCustomLogic_RespectsLogic()
        {
            // Arrange
            var command = new AsyncValueCommand<string>(
                execute: (p, ct) => ValueTask.CompletedTask,
                canExecute: p => p == "allowed");

            using (Assert.EnterMultipleScope())
            {
                // Act & Assert
                Assert.That(command.CanExecute("allowed"), Is.True);
                Assert.That(command.CanExecute("disallowed"), Is.False);
            }
        }

        [Test]
        public async Task AsyncValueCommandT_Execute_ThrowsException_PropagatesToGlobalHandler()
        {
            // Arrange
            Exception? capturedException = null;
            AsyncCommand.GlobalUnhandledException += (sender, args) =>
            {
                capturedException = args.Exception;
                args.Handled = true;
            };

            var command = new AsyncValueCommand<string>((p, ct) =>
                ValueTask.FromException(new InvalidOperationException("Test error")));

            // Act & Assert - Execute (async void)
            ((ICommand)command).Execute("test");

            // Wait a bit for async void to complete
            await Task.Delay(100);
            Assert.That(capturedException,  Is.Not.Null);
            Assert.That(capturedException!.Message, Is.EqualTo("Test error"));
        }

#if NET
        [Test]
        public async Task AsyncValueCommandT_ExecuteAsync_WithValueTaskFromIValueTaskSource_CompletesCorrectly()
        {
            // Arrange
            var channel = System.Threading.Channels.Channel.CreateUnbounded<int>();
            var writer = channel.Writer;
            var reader = channel.Reader;

            writer.TryWrite(42);

            var command = new AsyncValueCommand<string>(async (param, ct) =>
            {
                var result = await reader.ReadAsync(ct);
                Assert.That(result, Is.EqualTo(42));
            });

            // Act & Assert
            await command.ExecuteAsync("test");
        }
#endif

        [Test]
        public void AsyncValueCommandT_MultipleConstructors_WorkCorrectly()
        {
            // Arrange & Act
            var command1 = new AsyncValueCommand<string>((p, ct) => ValueTask.CompletedTask);
            var command2 = new AsyncValueCommand<string>((p, ct) => ValueTask.CompletedTask, p => true);
            var command3 = new AsyncValueCommand<string>(p => ValueTask.CompletedTask);
            var command4 = new AsyncValueCommand<string>(p => ValueTask.CompletedTask, p => true);

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(command1, Is.Not.Null);
                Assert.That(command2, Is.Not.Null);
                Assert.That(command3, Is.Not.Null);
                Assert.That(command4, Is.Not.Null);
            }
        }

        #endregion

        #region AsyncValueCommand Tests

        [Test]
        public void AsyncValueCommand_Constructor_NullExecute_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new AsyncValueCommand((Func<ValueTask>)null!));
        }

        [Test]
        public async Task AsyncValueCommand_Execute_WithValueTask_Succeeds()
        {
            // Arrange
            bool executed = false;
            var command = new AsyncValueCommand(async (ct) =>
            {
                await Task.Delay(10, ct);
                executed = true;
            });

            // Act
            await command.ExecuteAsync(null);

            // Assert
            Assert.That(executed, Is.True);
        }

        [Test]
        public async Task AsyncValueCommand_Execute_SynchronousValueTask_Succeeds()
        {
            // Arrange
            int executionCount = 0;
            var command = new AsyncValueCommand((ct) =>
            {
                executionCount++;
                return ValueTask.CompletedTask;
            });

            // Act
            await command.ExecuteAsync(null);

            // Assert
            Assert.That(executionCount, Is.EqualTo(1));
        }

        [Test]
        public async Task AsyncValueCommand_Execute_WithCancellationToken_CancelsOperation()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            bool wasCancelled = false;

            var command = new AsyncValueCommand(async (ct) =>
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
            cts.CancelAfter(50);
            var executeTask = command.ExecuteAsync(null, cts.Token);

            // Assert
            Assert.ThrowsAsync<TaskCanceledException>(async () => await executeTask);
            Assert.That(wasCancelled, Is.True);
        }

        [Test]
        public void AsyncValueCommand_CanExecute_WithCustomLogic_RespectsLogic()
        {
            // Arrange
            bool canExecuteFlag = false;
            var command = new AsyncValueCommand(
                execute: ct => ValueTask.CompletedTask,
                canExecute: () => canExecuteFlag);

            // Act & Assert
            Assert.That(command.CanExecute(null), Is.False);

            canExecuteFlag = true;
            Assert.That(command.CanExecute(null), Is.True);
        }

        [Test]
        public async Task AsyncValueCommand_Execute_ThrowsException_PropagatesToGlobalHandler()
        {
            // Arrange
            Exception? capturedException = null;
            AsyncCommand.GlobalUnhandledException += (sender, args) =>
            {
                capturedException = args.Exception;
                args.Handled = true;
            };

            var command = new AsyncValueCommand((ct) =>
                ValueTask.FromException(new InvalidOperationException("Test error")));

            // Act - Call the async void Execute method
            ((ICommand)command).Execute(null);

            // Wait for async void completion
            await Task.Delay(100);

            // Assert - Verify the exception reached the global handler
            Assert.That(capturedException, Is.Not.Null);
            Assert.That(capturedException!.Message, Is.EqualTo("Test error"));
        }

        [Test]
        public void AsyncValueCommand_Execute_WithCanceledValueTask_ThrowsOperationCanceledException()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            // Arrange - Create a command with a pre-canceled ValueTask
            var command = new AsyncValueCommand(() => ValueTask.FromCanceled(cts.Token));

            // Act & Assert - ExecuteAsync should throw OperationCanceledException
            var exception = Assert.ThrowsAsync<TaskCanceledException>(
                async () => await command.ExecuteAsync(null, CancellationToken.None));

            Assert.That(exception, Is.Not.Null);
        }

        [Test]
        public void AsyncValueCommand_Execute_WithCanceledValueTask_AndCancellationToken_ThrowOperationCanceledException()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            // Arrange - Use the passed cancellation token
            var command = new AsyncValueCommand(() => ValueTask.FromCanceled(cts.Token));

            // Act & Assert
            var exception = Assert.ThrowsAsync<OperationCanceledException>(
                async () => await command.ExecuteAsync(null, cts.Token));

            Assert.That(exception, Is.Not.Null);
        }

        [Test]
        public async Task AsyncValueCommand_Execute_WithCancellationTokenSourceCanceled_ThrowsOperationCanceledException()
        {
            // Arrange - Cancel the token before execution
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            bool wasExecuted = false;
            var command = new AsyncValueCommand((ct) =>
            {
                wasExecuted = true;
                return ValueTask.CompletedTask;
            });

            // Act & Assert
            var exception = Assert.ThrowsAsync<OperationCanceledException>(
                async () => await command.ExecuteAsync(null, cts.Token));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(exception, Is.Not.Null);
                Assert.That(wasExecuted, Is.False, "Delegate should not be executed when token is already canceled");
            }
        }

        [Test]
        public async Task AsyncValueCommand_Execute_WithCancellationDuringExecution_ThrowsOperationCanceledException()
        {
            // Arrange
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

            var command = new AsyncValueCommand(async (ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(1), ct);
            });

            // Act & Assert
            var exception = Assert.ThrowsAsync<TaskCanceledException>(
                async () => await command.ExecuteAsync(null, cts.Token));

            Assert.That(exception, Is.Not.Null);
            Assert.That(exception.CancellationToken.IsCancellationRequested, Is.True);
        }

        [Test]
        public void AsyncValueCommand_Execute_WithCanceledValueTask_DoesNotRaiseUnhandledException()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            // Arrange - Global handler should NOT be called for OperationCanceledException
            bool globalHandlerCalled = false;
            AsyncCommand.GlobalUnhandledException += (sender, args) =>
            {
                globalHandlerCalled = true;
            };

            var command = new AsyncValueCommand(() => ValueTask.FromCanceled(cts.Token));

            // Act - Call via ICommand.Execute (async void)
            ((ICommand)command).Execute(null);

            // Wait a bit and check
            Thread.Sleep(100);

            // Assert - Global handler should NOT be called for cancellation
            Assert.That(globalHandlerCalled, Is.False,
                "OperationCanceledException should not trigger GlobalUnhandledException");
        }

        [Test]
        public void AsyncValueCommand_MultipleConstructors_WorkCorrectly()
        {
            // Arrange & Act
            var command1 = new AsyncValueCommand(ct => ValueTask.CompletedTask);
            var command2 = new AsyncValueCommand(ct => ValueTask.CompletedTask, () => true);
            var command3 = new AsyncValueCommand(() => ValueTask.CompletedTask);
            var command4 = new AsyncValueCommand(() => ValueTask.CompletedTask, () => true);

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(command1, Is.Not.Null);
                Assert.That(command2, Is.Not.Null);
                Assert.That(command3, Is.Not.Null);
                Assert.That(command4, Is.Not.Null);
            }
        }

        [Test]
        public async Task AsyncValueCommand_Execute_ParameterIgnored()
        {
            // Arrange
            bool executed = false;
            var command = new AsyncValueCommand(async (ct) =>
            {
                await Task.Delay(10, ct);
                executed = true;
            });

            // Act
            await command.ExecuteAsync("ignored parameter");

            // Assert
            Assert.That(executed, Is.True);
        }

        #endregion

        #region Concurrency Tests

        [Test]
        public async Task AsyncValueCommandT_ConcurrentExecution_WhenAllowed_ExecutesMultipleTimes()
        {
            // Arrange
            int concurrentExecutions = 0;
            int maxConcurrent = 0;
            var semaphore = new SemaphoreSlim(5, 5);

            var command = new AsyncValueCommand<string>(async (p, ct) =>
            {
                var current = Interlocked.Increment(ref concurrentExecutions);
                var oldMax = maxConcurrent;
                while (current > oldMax)
                {
                    oldMax = Interlocked.CompareExchange(ref maxConcurrent, current, oldMax);
                }

                await semaphore.WaitAsync(ct);
                try
                {
                    await Task.Delay(100, ct);
                }
                finally
                {
                    semaphore.Release();
                    Interlocked.Decrement(ref concurrentExecutions);
                }
            })
            { AllowConcurrentExecution = true };

            // Act
            var tasks = new Task[10];
            for (int i = 0; i < 10; i++)
            {
                tasks[i] = command.ExecuteAsync($"task{i}");
            }

            await Task.WhenAll(tasks);

            // Assert
            await Progress.WriteLineAsync($"Max concurrent executions: {maxConcurrent}");
            Assert.That(maxConcurrent, Is.GreaterThan(1));
        }

        [Test]
        public async Task AsyncValueCommandT_ConcurrentExecution_WhenNotAllowed_SequentialExecution()
        {
            // Arrange
            int executionCount = 0;
            var command = new AsyncValueCommand<string>(async (p, ct) =>
            {
                var current = Interlocked.Increment(ref executionCount);
                await Task.Delay(50, ct);
                Assert.That(current, Is.EqualTo(1));
                Interlocked.Decrement(ref executionCount);
            })
            { AllowConcurrentExecution = false };

            // Act
            var tasks = new Task[5];
            for (int i = 0; i < 5; i++)
            {
                tasks[i] = command.ExecuteAsync($"task{i}");
            }

            await Task.WhenAll(tasks);

            // Assert
            Assert.That(executionCount, Is.Zero);
        }

        #endregion
    }
}
#endif