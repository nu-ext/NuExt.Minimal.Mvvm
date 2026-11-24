namespace Minimal.Mvvm.Tests;

[TestFixture]
public class CastValidateTargetTypeTests
{
    private enum TestEnum
    {
        First,
        Second
    }

    [Test]
    public void ValidateTargetType_AcceptsExactType()
    {
        Assert.DoesNotThrow(() => Cast<bool>.ValidateTargetType(typeof(bool)));
        Assert.DoesNotThrow(() => Cast<int>.ValidateTargetType(typeof(int)));
    }

    [Test]
    public void ValidateTargetType_AcceptsBaseType()
    {
        Assert.DoesNotThrow(() => Cast<TestEnum>.ValidateTargetType(typeof(Enum)));
        Assert.DoesNotThrow(() => Cast<bool>.ValidateTargetType(typeof(object)));
    }

    [Test]
    public void ValidateTargetType_AcceptsNullableTargetType()
    {
        Assert.DoesNotThrow(() => Cast<bool>.ValidateTargetType(typeof(bool?)));
        Assert.DoesNotThrow(() => Cast<int>.ValidateTargetType(typeof(int?)));
        Assert.DoesNotThrow(() => Cast<TestEnum>.ValidateTargetType(typeof(TestEnum?)));
    }

    [Test]
    public void ValidateTargetType_AcceptsNullableTargetForNullableType()
    {
        Assert.DoesNotThrow(() => Cast<bool?>.ValidateTargetType(typeof(bool)));
        Assert.DoesNotThrow(() => Cast<bool?>.ValidateTargetType(typeof(bool?)));
    }

    [Test]
    public void ValidateTargetType_ThrowsForIncompatibleType()
    {
        Assert.Throws<InvalidCastException>(() => Cast<bool>.ValidateTargetType(typeof(int)));
        Assert.Throws<InvalidCastException>(() => Cast<bool>.ValidateTargetType(typeof(int?)));
        Assert.Throws<InvalidCastException>(() => Cast<TestEnum>.ValidateTargetType(typeof(string)));
    }

    [Test]
    public void ValidateTargetType_ThrowsForNull()
    {
        Assert.Throws<ArgumentNullException>(() => Cast<bool>.ValidateTargetType(null!));
    }
}
