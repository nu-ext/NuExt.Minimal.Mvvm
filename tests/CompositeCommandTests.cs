using Minimal.Mvvm.Tests.Infrastructure;
using NUnit.Framework.Legacy;
using System.ComponentModel;
using System.Windows.Input;

namespace Minimal.Mvvm.Tests
{
    [TestFixture]
    public class CompositeCommandTests
    {
        [Test]
        public void Ctor_Deduplicates_PreservesFirstOccurrence()
        {
            var a = new ToggleCommand();
            var b = new ToggleCommand();
            var sut = new CompositeCommand(a, b, a); // duplicate 'a'

            Assert.That(sut.Count, Is.EqualTo(2));
            var snapshot = sut.ToArray();
            Assert.That(snapshot, Is.EqualTo(new ICommand[] { a, b }));
        }

        [Test]
        public void TryAdd_AddsUnique_RejectsDuplicate()
        {
            var a = new ToggleCommand();
            var b = new ToggleCommand();

            var sut = new CompositeCommand(a);
            Assert.That(sut.Count, Is.EqualTo(1));

            var addedB = sut.TryAdd(b);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(addedB, Is.True);
                Assert.That(sut.Count, Is.EqualTo(2));
            }

            var addedAgainA = sut.TryAdd(a);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(addedAgainA, Is.False);
                Assert.That(sut.Count, Is.EqualTo(2));
            }
        }

        [Test]
        public void TryRemove_RemovesByReference_FirstOccurrenceOnly()
        {
            var a = new ToggleCommand();
            var b = new ToggleCommand();

            var sut = new CompositeCommand(a, b);
            Assert.That(sut.Count, Is.EqualTo(2));

            var removedA = sut.TryRemove(a);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(removedA, Is.True);
                Assert.That(sut.Count, Is.EqualTo(1));
            }

            var removedAAgain = sut.TryRemove(a);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(removedAAgain, Is.False);
                Assert.That(sut.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void Clear_RemovesAll_AndRaisesCountChange()
        {
            var a = new ToggleCommand();
            var b = new ToggleCommand();

            var sut = new CompositeCommand(a, b);
            int countChanged = 0;
            ((INotifyPropertyChanged)sut).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(CompositeCommand.Count)) countChanged++;
            };

            sut.Clear();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(sut.Count, Is.Zero);
                Assert.That(countChanged, Is.GreaterThanOrEqualTo(1));
            }
        }

        [Test]
        public void CanExecute_All_Mode()
        {
            bool canA = true, canB = true;
            var a = new RelayCommand(() => { }, () => canA);
            var b = new RelayCommand(() => { }, () => canB);

            var sut = new CompositeCommand(a, b) { CanExecuteMode = CanExecuteMode.All };

            Assert.That(sut.CanExecute(null), Is.True);

            canB = false;
            Assert.That(sut.CanExecute(null), Is.False);

            canA = false;
            Assert.That(sut.CanExecute(null), Is.False);

            canA = canB = true;
            Assert.That(sut.CanExecute(null), Is.True);
        }

        [Test]
        public void CanExecute_Any_Mode()
        {
            bool canA = false, canB = false;
            var a = new RelayCommand(() => { }, () => canA);
            var b = new RelayCommand(() => { }, () => canB);

            var sut = new CompositeCommand(a, b) { CanExecuteMode = CanExecuteMode.Any };

            Assert.That(sut.CanExecute(null), Is.False);

            canA = true;
            Assert.That(sut.CanExecute(null), Is.True);

            canA = false; canB = true;
            Assert.That(sut.CanExecute(null), Is.True);

            canA = canB = false;
            Assert.That(sut.CanExecute(null), Is.False);
        }

        [Test]
        public void CanExecute_First_Mode_UsesFirstOnly()
        {
            bool canA = false, canB = true;
            var a = new RelayCommand(() => { }, () => canA);
            var b = new RelayCommand(() => { }, () => canB);

            var sut = new CompositeCommand(a, b) { CanExecuteMode = CanExecuteMode.First };

            Assert.That(sut.CanExecute(null), Is.False);
            canA = true;
            Assert.That(sut.CanExecute(null), Is.True);

            // Changing second has no effect in First mode CanExecute
            canB = false;
            Assert.That(sut.CanExecute(null), Is.True);
        }

        [Test]
        public async Task Execute_Order_SyncThenAsync()
        {
            var log = new List<string>();
            var a = new RelayCommand(TestHelpers.SyncStep(log, "A"));
            var b = new AsyncCommand(TestHelpers.AsyncStep(log, "B"));

            var sut = new CompositeCommand(a, b) { CanExecuteMode = CanExecuteMode.All };
            Assert.That(sut.CanExecute(null), Is.True);

            await ((IAsyncCommand)sut).ExecuteAsync(null, CancellationToken.None);

            Assert.That(log, Is.EqualTo([
                "A",
                "B:start",
                "B:end"
            ]).AsCollection);
        }

        [Test]
        public async Task Execute_Any_SkipsNonExecutable()
        {
            bool canA = false, canB = true;
            var log = new List<string>();

            var a = new RelayCommand(TestHelpers.SyncStep(log, "A"), () => canA);
            var b = new RelayCommand(TestHelpers.SyncStep(log, "B"), () => canB);

            var sut = new CompositeCommand(a, b) { CanExecuteMode = CanExecuteMode.Any };

            await ((IAsyncCommand)sut).ExecuteAsync(null, CancellationToken.None);

            Assert.That(log, Is.EqualTo(["B"]).AsCollection);
        }

        [Test]
        public async Task Execute_First_StopsOnFirstNonExecutable()
        {
            bool canA = true, canB = false;
            var log = new List<string>();

            var a = new RelayCommand(TestHelpers.SyncStep(log, "A"), () => canA);
            var b = new RelayCommand(TestHelpers.SyncStep(log, "B"), () => canB);
            var c = new RelayCommand(TestHelpers.SyncStep(log, "C"));

            var sut = new CompositeCommand(a, b, c) { CanExecuteMode = CanExecuteMode.First };

            await ((IAsyncCommand)sut).ExecuteAsync(null, CancellationToken.None);

            // Executes A, then re-checks B => false => stops; C is not executed
            Assert.That(log, Is.EqualTo(["A"]).AsCollection);
        }

        [Test]
        public void Execute_Exceptions_StopAndPropagate()
        {
            var a = new RelayCommand(() => { });
            var boom = new RelayCommand(() => throw new InvalidOperationException("boom"));
            var c = new RelayCommand(() => { });

            var sut = new CompositeCommand(a, boom, c) { CanExecuteMode = CanExecuteMode.All };

            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await ((IAsyncCommand)sut).ExecuteAsync(null, CancellationToken.None);
            });

            Assert.That(ex!.Message, Is.EqualTo("boom"));
        }

        [Test]
        public void Execute_Cancellation_BetweenCommands()
        {
            var cts = new CancellationTokenSource();
            var log = new List<string>();

            var first = new AsyncCommand(async ct =>
            {
                log.Add("first:start");
                await Task.Delay(5, ct).ConfigureAwait(false);
                log.Add("first:end");
                cts.Cancel(); // cancel after first completes
            });

            var second = new AsyncCommand(async ct =>
            {
                log.Add("second:should-not-run");
                await Task.CompletedTask;
            });

            var sut = new CompositeCommand(first, second) { CanExecuteMode = CanExecuteMode.All };

            Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await ((IAsyncCommand)sut).ExecuteAsync(null, cts.Token);
            });

            // Only the first ran
            Assert.That(log, Is.EqualTo(["first:start", "first:end"]).AsCollection);
        }

        [Test]
        public async Task Snapshot_AddDuringExecution_DoesNotAffectCurrentRun()
        {
            var log = new List<string>();
            var extra = new RelayCommand(TestHelpers.SyncStep(log, "EXTRA"));

            var after = new RelayCommand(TestHelpers.SyncStep(log, "AFTER"));

            CompositeCommand? sut = null;

            var mutatorWithAdd = new RelayCommand(() =>
            {
                log.Add("MUTATOR:add");
                _ = sut!.TryAdd(extra);
            });

            sut = new CompositeCommand(mutatorWithAdd, after) { CanExecuteMode = CanExecuteMode.All };

            await ((IAsyncCommand)sut).ExecuteAsync(null, CancellationToken.None);

            // EXTRA must not appear in the current run
            Assert.That(log, Is.EqualTo(["MUTATOR:add", "AFTER"]).AsCollection);

            // Next run should see EXTRA
            log.Clear();
            await ((IAsyncCommand)sut).ExecuteAsync(null, CancellationToken.None);
            Assert.That(log, Is.EqualTo(["MUTATOR:add", "AFTER", "EXTRA"]).AsCollection);
        }

        [Test]
        public async Task Snapshot_RemoveDuringExecution_DoesNotAffectCurrentRun()
        {
            var log = new List<string>();

            var first = new RelayCommand(() => { /* noop */ });
            var second = new RelayCommand(TestHelpers.SyncStep(log, "SECOND"));

            CompositeCommand? sut = null;

            var removerThatRemoves = new RelayCommand(() =>
            {
                _ = sut!.TryRemove(second);
            });

            sut = new CompositeCommand(first, second, removerThatRemoves)
            {
                CanExecuteMode = CanExecuteMode.All
            };

            await ((IAsyncCommand)sut).ExecuteAsync(null, CancellationToken.None);

            // SECOND still executed in the current run (snapshot was taken before removal)
            Assert.That(log, Is.EqualTo(["SECOND"]).AsCollection);

            // Next run: SECOND is gone
            log.Clear();
            await ((IAsyncCommand)sut).ExecuteAsync(null, CancellationToken.None);
            Assert.That(log, Is.EqualTo(Array.Empty<string>()).AsCollection);
        }

        [Test]
        public void CanExecuteChanged_Forwarded_FromChild()
        {
            var child = new ToggleCommand(initial: false);
            var sut = new CompositeCommand(child) { CanExecuteMode = CanExecuteMode.All };

            int raised = 0;
            sut.CanExecuteChanged += (_, __) => raised++;

            // Toggle child; composite must forward one event
            child.SetCanExecute(true);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(raised, Is.EqualTo(1));
                Assert.That(sut.CanExecute(null), Is.True);
            }
        }

        [Test]
        public void Dispose_RaisesOnce_AndBecomesNonExecutable_AndThrowsOnMutation()
        {
            var a = new ToggleCommand();
            var sut = new CompositeCommand(a) { CanExecuteMode = CanExecuteMode.All };

            int canExecChanged = 0;
            sut.CanExecuteChanged += (_, __) => canExecChanged++;

            int countChanged = 0;
            ((INotifyPropertyChanged)sut).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(CompositeCommand.Count)) countChanged++;
                if (e.PropertyName == nameof(CompositeCommand.IsDisposed)) countChanged += 0; // just touch
            };

            sut.Dispose();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(sut.IsDisposed, Is.True);
                Assert.That(sut.CanExecute(null), Is.False);
                Assert.That(canExecChanged, Is.EqualTo(1)); // exactly one notification from Dispose (oldCount>0)
                Assert.That(sut.Count, Is.Zero);
            }

            Assert.Throws<ObjectDisposedException>(() => sut.TryAdd(new ToggleCommand()));
            Assert.Throws<ObjectDisposedException>(() => sut.TryRemove(a));
            Assert.Throws<ObjectDisposedException>(() => sut.Clear());
        }

        [Test]
        public void ToArray_ReturnsDetachedSnapshot()
        {
            var a = new ToggleCommand();
            var sut = new CompositeCommand(a);
            var snapshot = sut.ToArray();

            sut.TryRemove(a);
            Assert.That(snapshot, Has.Length.EqualTo(1));
            Assert.That(snapshot[0], Is.EqualTo(a));
        }
    }
}