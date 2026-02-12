namespace Minimal.Mvvm.Tests
{
    [TestFixture]
    public class CastTests
    {
        [TestFixture]
        public class PrimitiveTypeTests
        {
            [Test]
            public void To_Int32_ConvertsFromString()
            {
                var result = Cast<int>.To("42");
                Assert.That(result, Is.EqualTo(42));
            }

            [Test]
            public void To_Int32_ConvertsFromDouble()
            {
                var result = Cast<int>.To(42.9);
                Assert.That(result, Is.EqualTo(43));
            }

            [Test]
            public void To_Int32_ThrowsOnInvalidString()
            {
                Assert.Throws<InvalidCastException>(() => Cast<int>.To("invalid"));
            }

            [Test]
            public void To_Int32_ReturnsDefaultOnInvalidStringWhenNotThrowing()
            {
                var result = Cast<int>.To("invalid", throwCastException: false);
                Assert.That(result, Is.Zero);
            }

            [Test]
            public void To_Int32_HandlesNull()
            {
                Assert.Throws<InvalidCastException>(() => Cast<int>.To(null));
            }

            [Test]
            public void To_Int32_ReturnsDefaultForNullWhenNotThrowing()
            {
                var result = Cast<int>.To(null, throwCastException: false);
                Assert.That(result, Is.Zero);
            }
        }

        [TestFixture]
        public class NullableTypeTests
        {
            [Test]
            public void To_NullableInt32_AcceptsNull()
            {
                var result = Cast<int?>.To(null);
                Assert.That(result, Is.Null);
            }

            [Test]
            public void To_NullableInt32_ConvertsValidValue()
            {
                var result = Cast<int?>.To("42");
                Assert.That(result, Is.EqualTo(42));
            }

            [Test]
            public void To_NullableInt32_ReturnsNullOnInvalidWhenNotThrowing()
            {
                var result = Cast<int?>.To("invalid", throwCastException: false);
                Assert.That(result, Is.Null);
            }
        }

        [TestFixture]
        public class EnumTests
        {
            private enum TestEnum { First, Second }

            [Test]
            public void To_Enum_ConvertsFromString()
            {
                var result = Cast<TestEnum>.To("Second");
                Assert.That(result, Is.EqualTo(TestEnum.Second));
            }

            [Test]
            public void To_Enum_ConvertsFromInt32()
            {
                var result = Cast<TestEnum>.To(1);
                Assert.That(result, Is.EqualTo(TestEnum.Second));
            }

            [Test]
            public void To_Enum_ThrowsOnInvalidString()
            {
                Assert.Throws<InvalidCastException>(() => Cast<TestEnum>.To("InvalidValue"));
            }
        }

        [TestFixture]
        public class StringTypeTests
        {
            [Test]
            public void To_String_ConvertsFromInt32()
            {
                var result = Cast<string>.To(42);
                Assert.That(result, Is.EqualTo("42"));
            }

            [Test]
            public void To_String_ReturnsNullForNullWhenNotThrowing()
            {
                var result = Cast<string>.To(null, throwCastException: false);
                Assert.That(result, Is.Null);
            }

            [Test]
            public void To_String_ReturnsSameInstance()
            {
                var original = "test";
                var result = Cast<string>.To(original);
                Assert.That(result, Is.SameAs(original));
            }
        }

        [TestFixture]
        public class ReferenceTypeTests
        {
            private class TestClass { }

            [Test]
            public void To_ReferenceType_ReturnsSameInstance()
            {
                var instance = new TestClass();
                var result = Cast<TestClass>.To(instance);
                Assert.That(result, Is.SameAs(instance));
            }

            [Test]
            public void To_ReferenceType_ThrowsOnIncompatibleType()
            {
                Assert.Throws<InvalidCastException>(() => Cast<TestClass>.To("not a TestClass"));
            }
        }

        [TestFixture]
        public class DecimalTypeTests
        {
            [Test]
            public void To_Decimal_ConvertsFromString()
            {
                var result = Cast<decimal>.To("123.45");
                Assert.That(result, Is.EqualTo(123.45m));
            }

            [Test]
            public void To_Decimal_ConvertsFromDouble()
            {
                var result = Cast<decimal>.To(123.45);
                Assert.That(result, Is.EqualTo(123.45m));
            }
        }

        [TestFixture]
        public class BooleanTypeTests
        {
            [Test]
            public void To_Boolean_ConvertsFromString()
            {
                var result = Cast<bool>.To("true");
                Assert.That(result, Is.True);
            }

            [Test]
            public void To_Boolean_ConvertsFromInt32()
            {
                var result = Cast<bool>.To(1);
                Assert.That(result, Is.True);
            }
        }

        [TestFixture]
        public class DateTimeTypeTests
        {
            [Test]
            public void To_DateTime_ConvertsFromString()
            {
                var result = Cast<DateTime>.To("2023-01-15");
                Assert.That(result, Is.EqualTo(new DateTime(2023, 1, 15)));
            }
        }

        [Test]
        public void To_SameType_ReturnsInput()
        {
            var input = new object();
            var result = Cast<object>.To(input);
            Assert.That(result, Is.SameAs(input));
        }

        [Test]
        public void To_WithIConvertibleImplementation_Works()
        {
            var convertible = new ConvertibleMock(42);
            var result = Cast<int>.To(convertible);
            Assert.That(result, Is.EqualTo(42));
        }

        private class ConvertibleMock(int value) : IConvertible
        {
            private readonly int _value = value;

            public TypeCode GetTypeCode() => TypeCode.Int32;
            public bool ToBoolean(IFormatProvider? provider) => throw new NotSupportedException();
            public byte ToByte(IFormatProvider? provider) => throw new NotSupportedException();
            public char ToChar(IFormatProvider? provider) => throw new NotSupportedException();
            public DateTime ToDateTime(IFormatProvider? provider) => throw new NotSupportedException();
            public decimal ToDecimal(IFormatProvider? provider) => throw new NotSupportedException();
            public double ToDouble(IFormatProvider? provider) => throw new NotSupportedException();
            public short ToInt16(IFormatProvider? provider) => throw new NotSupportedException();
            public int ToInt32(IFormatProvider? provider) => _value;
            public long ToInt64(IFormatProvider? provider) => throw new NotSupportedException();
            public sbyte ToSByte(IFormatProvider? provider) => throw new NotSupportedException();
            public float ToSingle(IFormatProvider? provider) => throw new NotSupportedException();
            public string ToString(IFormatProvider? provider) => throw new NotSupportedException();
            public object ToType(Type conversionType, IFormatProvider? provider) => throw new NotSupportedException();
            public ushort ToUInt16(IFormatProvider? provider) => throw new NotSupportedException();
            public uint ToUInt32(IFormatProvider? provider) => throw new NotSupportedException();
            public ulong ToUInt64(IFormatProvider? provider) => throw new NotSupportedException();
        }

        [TestFixture]
        public class PerformanceTests
        {
            [Test]
            [Repeat(1000)]
            public void To_Int32_Performance()
            {
                var result = Cast<int>.To("42");
                Assert.That(result, Is.EqualTo(42));
            }

            [Test]
            public void To_Enum_Performance()
            {
                for (int i = 0; i < 1000; i++)
                {
                    var result = Cast<StringComparison>.To("OrdinalIgnoreCase");
                    Assert.That(result, Is.EqualTo(StringComparison.OrdinalIgnoreCase));
                }
            }
        }
    }
}