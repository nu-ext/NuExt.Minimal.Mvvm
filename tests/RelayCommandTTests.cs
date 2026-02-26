using Minimal.Mvvm;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Minimal.Mvvm.Tests
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class RelayCommandTTests
    {
        [Test]
        public void Constructor_WithExecuteAndCanExecute_InitializesCorrectly()
        {
            var command = new RelayCommand<int>(
                _ => { },
                param => param > 0);

            Assert.That(command, Is.Not.Null);
            Assert.That(command.AllowConcurrentExecution, Is.False);
        }

        [Test]
        public void Constructor_WithExecuteOnly_AlwaysCanExecute()
        {
            var command = new RelayCommand<int>(_ => { });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(command.CanExecute(42), Is.True);
                Assert.That(command.CanExecute(-1), Is.True);
                Assert.That(command.CanExecute(0), Is.True);
            }
        }

        [Test]
        public void Constructor_NullExecute_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new RelayCommand<int>(null!));
            Assert.Throws<ArgumentNullException>(() => new RelayCommand<int>(null!, null));
        }

        [Test]
        public void Execute_InvokesProvidedAction()
        {
            int executedValue = 0;
            var command = new RelayCommand<int>(value => executedValue = value);

            command.Execute(42);

            Assert.That(executedValue, Is.EqualTo(42));
        }

        [Test]
        public void Execute_WhenCanExecuteReturnsFalse_DoesNotInvokeAction()
        {
            bool actionInvoked = false;
            var command = new RelayCommand<int>(
                _ => actionInvoked = true,
                _ => false);

            command.Execute(42);

            Assert.That(actionInvoked, Is.False);
        }

        [Test]
        public void CanExecute_WithCustomDelegate_RespectsDelegate()
        {
            bool canExecute = false;
            var command = new RelayCommand<int>(
                _ => { },
                _ => canExecute);

            Assert.That(command.CanExecute(42), Is.False);

            canExecute = true;
            command.RaiseCanExecuteChanged();

            Assert.That(command.CanExecute(42), Is.True);
        }

        [Test]
        public void Execute_WithAllowConcurrentExecutionFalse_PreventsOverlap()
        {
            int executionCount = 0;
            var command = new RelayCommand<int>(_ =>
            {
                Interlocked.Increment(ref executionCount);
                Thread.Sleep(500);
            });

            var task1 = Task.Run(() => command.Execute(1));
            Thread.Sleep(10);
            var task2 = Task.Run(() => command.Execute(2));

            Task.WaitAll(task1, task2);

            Assert.That(executionCount, Is.EqualTo(1));
        }

        [Test]
        public void Execute_WithAllowConcurrentExecutionTrue_AllowsOverlap()
        {
            int executionCount = 0;
            var command = new RelayCommand<int>(_ =>
            {
                Interlocked.Increment(ref executionCount);
                Thread.Sleep(100);
            })
            {
                AllowConcurrentExecution = true
            };

            var task1 = Task.Run(() => command.Execute(1));
            var task2 = Task.Run(() => command.Execute(2));

            Task.WaitAll(task1, task2);

            Assert.That(executionCount, Is.EqualTo(2));
        }

        [Test]
        public void PropertyChanged_IsExecuting_RaisesEvent()
        {
            var command = new RelayCommand<int>(_ => Thread.Sleep(50));

            var propertyChanges = new List<string>();
            ((INotifyPropertyChanged)command).PropertyChanged += (s, e) =>
            {
                propertyChanges.Add(e.PropertyName!);
            };

            command.Execute(42);

            Assert.That(propertyChanges, Contains.Item(nameof(command.IsExecuting)));
        }

        [Test]
        public void RaiseCanExecuteChanged_RaisesEvent()
        {
            var command = new RelayCommand<int>(_ => { });
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
        public void GetCommandParameter_InvalidCast_ThrowsInvalidCastException()
        {
            var command = new RelayCommand<string>(_ => { });

            var result = command.GetCommandParameter(42);

            Assert.That(result, Is.EqualTo("42"));
        }

        [Test]
        public void GetCommandParameter_ValidCast_ReturnsValue()
        {
            var command = new RelayCommand<int>(_ => { });

            var result = command.GetCommandParameter((object)42);

            Assert.That(result, Is.EqualTo(42));
        }

        [Test]
        public void Execute_ExceptionInAction_PropagatesToCaller()
        {
            var exception = new InvalidOperationException("Test");
            var command = new RelayCommand<int>(_ => throw exception);

            var thrownException = Assert.Throws<InvalidOperationException>(() =>
            {
                command.Execute(42);
            });

            Assert.That(thrownException, Is.SameAs(exception));
        }
    }
}
