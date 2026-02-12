using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Minimal.Mvvm
{
    /// <summary>
    /// Provides optimized type conversion methods for generic type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The target type for conversion.</typeparam>
    public static partial class Cast<T>
    {
        private static readonly Type s_targetType = typeof(T);
        private static readonly bool s_isNullableValueType = Nullable.GetUnderlyingType(s_targetType) != null;
        private static readonly Type s_underlyingType = s_isNullableValueType
            ? Nullable.GetUnderlyingType(s_targetType)!
            : s_targetType;

        private static readonly bool s_isEnum = s_underlyingType.IsEnum;
        private static readonly bool s_isValueType = s_underlyingType.IsValueType;
        private static readonly TypeCode s_typeCode = Type.GetTypeCode(s_underlyingType);

        // Cached delegates for fast conversions
        private static readonly Func<object, CultureInfo, T>? s_convertPrimitive;
        private static readonly Func<object, CultureInfo, T>? s_convertEnum;

        static Cast()
        {
            // Initialize cached delegates for primitive types
            if (s_isEnum)
            {
                s_convertEnum = CreateEnumConverter();
            }
            else if (s_typeCode != TypeCode.Object && s_typeCode != TypeCode.DBNull)
            {
                s_convertPrimitive = CreatePrimitiveConverter();
            }
        }

        /// <summary>
        /// Validates that type <typeparamref name="T"/> can be assigned to the specified target type.
        /// </summary>
        /// <param name="targetType">The target type to validate assignment to.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="targetType"/> is null.</exception>
        /// <exception cref="InvalidCastException">
        /// Thrown when type <typeparamref name="T"/> cannot be assigned to <paramref name="targetType"/>.
        /// </exception>
        public static void ValidateTargetType(Type targetType)
        {
            _ = targetType ?? throw new ArgumentNullException(nameof(targetType));

            if (!targetType.IsAssignableFrom(s_underlyingType))
            {
                throw new InvalidCastException(
                    $"Type '{s_underlyingType}' cannot be assigned to target type '{targetType}'.");
            }
        }

        /// <summary>
        /// Converts the given value to the specified generic type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="value">The value to be converted. Can be null.</param>
        /// <param name="throwCastException">
        /// If true, an exception will be thrown if the conversion fails; otherwise, default value of type <typeparamref name="T"/> will be returned.
        /// </param>
        /// <returns>
        /// The converted value of type <typeparamref name="T"/>, or default value of type <typeparamref name="T"/> if the conversion fails and <paramref name="throwCastException"/> is false.
        /// </returns>
        /// <exception cref="InvalidCastException">Thrown when the conversion to <typeparamref name="T"/> fails and <paramref name="throwCastException"/> is true.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T To(object? value, bool throwCastException = true)
            => To(value, CultureInfo.InvariantCulture, throwCastException);

        /// <summary>
        /// Converts the given value to the specified generic type <typeparamref name="T"/> using the specified culture.
        /// </summary>
        /// <param name="value">The value to be converted. Can be null.</param>
        /// <param name="culture">The culture to use for the conversion. If null, <see cref="CultureInfo.InvariantCulture"/> is used.</param>
        /// <param name="throwCastException">
        /// If true, an exception will be thrown if the conversion fails; otherwise, default value of type <typeparamref name="T"/> will be returned.
        /// </param>
        /// <returns>
        /// The converted value of type <typeparamref name="T"/>, or default value of type <typeparamref name="T"/> if the conversion fails and <paramref name="throwCastException"/> is false.
        /// </returns>
        /// <exception cref="InvalidCastException">Thrown when the conversion to <typeparamref name="T"/> fails and <paramref name="throwCastException"/> is true.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T To(object? value, CultureInfo? culture, bool throwCastException = true)
        {
            culture ??= CultureInfo.InvariantCulture;

            // Fast path: null for nullable types
            if (value is null)
            {
                return s_isValueType && !s_isNullableValueType && throwCastException
                    ? throw new InvalidCastException($"Cannot convert null to value type {s_targetType}")
                    : default!;
            }

            // Fast path: already correct type
            if (value is T typedValue)
            {
                return typedValue;
            }

            // Optimized path for primitive types
            if (s_convertPrimitive != null)
            {
                return ConvertPrimitive(value, culture, throwCastException);
            }

            // Enum handling
            if (s_isEnum)
            {
                return ConvertEnum(value, culture, throwCastException);
            }

            // Reference type handling
            if (!s_isValueType)
            {
                return ConvertReferenceType(value, throwCastException);
            }

            // Slow path for other value types
            return ConvertSlow(value, culture, throwCastException);
        }

        private static T ConvertPrimitive(object value, CultureInfo culture, bool throwCastException)
        {
            try
            {
                return s_convertPrimitive!(value, culture);
            }
            catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
            {
                if (!throwCastException)
                {
                    return default!;
                }
                throw new InvalidCastException($"Cannot convert '{value}' to {s_targetType}", ex);
            }
        }

        private static T ConvertEnum(object value, CultureInfo culture, bool throwCastException)
        {
            try
            {
                return s_convertEnum!(value, culture);
            }
            catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException or ArgumentException)
            {
                if (!throwCastException)
                {
                    return default!;
                }
                throw new InvalidCastException($"Cannot convert '{value}' to enum type {s_underlyingType}", ex);
            }
        }

        private static T ConvertReferenceType(object value, bool throwCastException)
        {
            try
            {
                return (T)value;
            }
            catch (InvalidCastException)
            {
                if (!throwCastException)
                {
                    return default!;
                }
                throw;
            }
        }

        private static T ConvertSlow(object value, CultureInfo culture, bool throwCastException)
        {
            try
            {
                if (value is IConvertible && s_underlyingType != typeof(object))
                {
                    return (T)Convert.ChangeType(value, s_underlyingType, culture);
                }

                return (T)value;
            }
            catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
            {
                if (!throwCastException)
                {
                    return default!;
                }
                throw new InvalidCastException($"Cannot convert '{value}' to {s_targetType}", ex);
            }
        }

        private static Func<object, CultureInfo, T> CreateEnumConverter()
        {
            return static (value, culture) =>
            {
                // For .NET 5+ we can use TryParse for better performance
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                if (value is string s && Enum.TryParse(s_underlyingType, s, ignoreCase: false, out var result))
                    return (T)result;
#endif
                try
                {
                    var underlyingValue = Convert.ChangeType(value, s_underlyingType.GetEnumUnderlyingType(), culture);
                    return (T)Enum.ToObject(s_underlyingType, underlyingValue);
                }
                catch (FormatException)
                {
#if !NET5_0_OR_GREATER && !NETSTANDARD2_1_OR_GREATER
                    if (value is string str)
                    {
                        return (T)Enum.Parse(s_underlyingType, str, ignoreCase: false);
                    }
#endif
                    throw;
                }
            };
        }

        private static Func<object, CultureInfo, T> CreatePrimitiveConverter()
        {
            return s_typeCode switch
            {
                TypeCode.Boolean => static (v, culture) => (T)(object)Convert.ToBoolean(v, culture),
                TypeCode.Byte => static (v, culture) => (T)(object)Convert.ToByte(v, culture),
                TypeCode.Char => static (v, culture) => (T)(object)Convert.ToChar(v, culture),
                TypeCode.DateTime => static (v, culture) => (T)(object)Convert.ToDateTime(v, culture),
                TypeCode.Decimal => static (v, culture) => (T)(object)Convert.ToDecimal(v, culture),
                TypeCode.Double => static (v, culture) => (T)(object)Convert.ToDouble(v, culture),
                TypeCode.Int16 => static (v, culture) => (T)(object)Convert.ToInt16(v, culture),
                TypeCode.Int32 => static (v, culture) => (T)(object)Convert.ToInt32(v, culture),
                TypeCode.Int64 => static (v, culture) => (T)(object)Convert.ToInt64(v, culture),
                TypeCode.SByte => static (v, culture) => (T)(object)Convert.ToSByte(v, culture),
                TypeCode.Single => static (v, culture) => (T)(object)Convert.ToSingle(v, culture),
                TypeCode.String => static (v, culture) => (T)(object)Convert.ToString(v, culture)!,
                TypeCode.UInt16 => static (v, culture) => (T)(object)Convert.ToUInt16(v, culture),
                TypeCode.UInt32 => static (v, culture) => (T)(object)Convert.ToUInt32(v, culture),
                TypeCode.UInt64 => static (v, culture) => (T)(object)Convert.ToUInt64(v, culture),
                _ => null!
            };
        }
    }
}
