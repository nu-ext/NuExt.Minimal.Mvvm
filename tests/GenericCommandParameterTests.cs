using System.ComponentModel;

namespace Minimal.Mvvm.Tests
{
    class GenericCommandParameterTests
    {
        protected static IRelayCommand<T> CreateCommand<T>(Action<T> execute, Func<T, bool>? canExecute = null)
        {
            return new RelayCommand<T>(execute, canExecute);
        }

        [Test]
        public void GenericEnumTypeCommandTest()
        {
            var command = CreateCommand<BindingDirection>(x => { }, x => true);

            Assert.That(command.CanExecute(BindingDirection.TwoWay), Is.True);
            command.Execute(BindingDirection.TwoWay);

            Assert.That(command.CanExecute((object)BindingDirection.TwoWay), Is.True);
            command.Execute((object)BindingDirection.TwoWay);

            Assert.That(command.CanExecute(1), Is.True);
            command.Execute(1);

            Assert.That(command.CanExecute("TwoWay"), Is.True);
            command.Execute("TwoWay");

            Assert.That(command.CanExecute("x"), Is.False);
            Assert.Throws<InvalidCastException>(() => command.Execute("x"));

            command.CanExecute((object?)null);
            Assert.Throws<InvalidCastException>(() => command.Execute((object?)null));

            command.CanExecute(new object());
            Assert.Throws<InvalidCastException>(() => command.Execute(new object()));

            Assert.That(command.CanExecute(int.MaxValue), Is.True);
            command.Execute(int.MaxValue);

            Assert.That(command.CanExecute(long.MaxValue), Is.False);
            Assert.Throws<InvalidCastException>(() => command.Execute(long.MaxValue));

            Assert.Pass();
        }
    }
}
