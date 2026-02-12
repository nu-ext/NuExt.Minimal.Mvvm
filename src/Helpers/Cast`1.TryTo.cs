using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Minimal.Mvvm
{
    partial class Cast<T>
    {
        /// <summary>
        /// Attempts to convert the given value to the specified generic type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="value">The value to be converted. Can be null.</param>
        /// <param name="result">
        /// When this method returns, contains the converted value if the conversion succeeded, or default value of type <typeparamref name="T"/> if the conversion failed.
        /// </param>
        /// <returns><see langword="true"/> if the conversion succeeded; otherwise, <see langword="false"/>.</returns>
        public static bool TryTo(object? value, [MaybeNullWhen(false)] out T result)
            => TryTo(value, null, out result);

        /// <summary>
        /// Attempts to convert the given value to the specified generic type <typeparamref name="T"/> using the specified culture.
        /// </summary>
        /// <param name="value">The value to be converted. Can be null.</param>
        /// <param name="culture">The culture to use for the conversion. If null, <see cref="CultureInfo.InvariantCulture"/> is used.</param>
        /// <param name="result">
        /// When this method returns, contains the converted value if the conversion succeeded, or default value of type <typeparamref name="T"/> if the conversion failed.
        /// </param>
        /// <returns><see langword="true"/> if the conversion succeeded; otherwise, <see langword="false"/>.</returns>
        public static bool TryTo(object? value, CultureInfo? culture, [MaybeNullWhen(false)] out T result)
        {
            culture ??= CultureInfo.InvariantCulture;

            // Fast path: null for nullable types
            if (value is null)
            {
                if (s_isValueType && !s_isNullableValueType)
                {
                    result = default!;
                    return false;
                }
                result = default!;
                return true;
            }

            // Fast path: already correct type
            if (value is T typedValue)
            {
                result = typedValue;
                return true;
            }

            // For strings, try optimized path first
            if (value is string str)
            {
                return TryConvertFromString(str, culture, out result);
            }

            // For primitive types
            if (s_convertPrimitive != null)
            {
                return TryConvertPrimitive(value, culture, out result);
            }

            // Handle enum conversions for non-string values
            if (s_isEnum)
            {
                return TryConvertEnum(value, culture, out result);
            }

            // For value types, try conversion via Convert.ChangeType
            if (s_isValueType)
            {
                return TryConvertValueType(value, culture, out result);
            }

            // For reference types that are not T, conversion is impossible
            result = default!;
            return false;
        }

        private static bool TryConvertFromString(string str, CultureInfo culture, [MaybeNullWhen(false)] out T result)
        {
            // Try as enum first
            if (s_isEnum)
            {
                return TryConvertEnumFromString(str, out result);
            }

            // Try as primitive type using TryParse methods
            if (s_typeCode != TypeCode.Object && s_typeCode != TypeCode.DBNull)
            {
                return TryConvertPrimitiveFromString(str, culture, out result);
            }

            // For other reference types, can't convert from string
            result = default!;
            return false;
        }

        private static bool TryConvertEnumFromString(string str, [MaybeNullWhen(false)] out T result)
        {
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            if (Enum.TryParse(s_underlyingType, str, ignoreCase: false, out var enumResult))
            {
                result = (T)enumResult;
                return true;
            }
#else
            try
            {
                if (Enum.IsDefined(s_underlyingType, str))
                {
                    result = (T)Enum.Parse(s_underlyingType, str, ignoreCase: false);
                    return true;
                }
            }
            catch
            {
                // Not a valid enum name
            }
#endif

            // Try numeric string conversion
            try
            {
                var underlyingValue = Convert.ChangeType(str, s_underlyingType.GetEnumUnderlyingType(), CultureInfo.InvariantCulture);
                result = (T)Enum.ToObject(s_underlyingType, underlyingValue);
                return true;
            }
            catch
            {
                result = default!;
                return false;
            }
        }

        private static bool TryConvertPrimitiveFromString(string str, CultureInfo culture, [MaybeNullWhen(false)] out T result)
        {
            bool success = false;
            object? underlyingValue = null;

            switch (s_typeCode)
            {
                case TypeCode.Boolean:
                    success = bool.TryParse(str, out var b);
                    underlyingValue = b;
                    break;
                case TypeCode.Byte:
                    success = byte.TryParse(str, NumberStyles.Integer, culture, out var bt);
                    underlyingValue = bt;
                    break;
                case TypeCode.Char:
                    success = char.TryParse(str, out var c);
                    underlyingValue = c;
                    break;
                case TypeCode.DateTime:
                    success = DateTime.TryParse(str, culture, DateTimeStyles.None, out var dt);
                    underlyingValue = dt;
                    break;
                case TypeCode.Decimal:
                    success = decimal.TryParse(str, NumberStyles.Number, culture, out var d);
                    underlyingValue = d;
                    break;
                case TypeCode.Double:
                    success = double.TryParse(str, NumberStyles.Float, culture, out var db);
                    underlyingValue = db;
                    break;
                case TypeCode.Int16:
                    success = short.TryParse(str, NumberStyles.Integer, culture, out var i16);
                    underlyingValue = i16;
                    break;
                case TypeCode.Int32:
                    success = int.TryParse(str, NumberStyles.Integer, culture, out var i32);
                    underlyingValue = i32;
                    break;
                case TypeCode.Int64:
                    success = long.TryParse(str, NumberStyles.Integer, culture, out var i64);
                    underlyingValue = i64;
                    break;
                case TypeCode.SByte:
                    success = sbyte.TryParse(str, NumberStyles.Integer, culture, out var sb);
                    underlyingValue = sb;
                    break;
                case TypeCode.Single:
                    success = float.TryParse(str, NumberStyles.Float, culture, out var f);
                    underlyingValue = f;
                    break;
                case TypeCode.String:
                    underlyingValue = str;
                    success = true;
                    break;
                case TypeCode.UInt16:
                    success = ushort.TryParse(str, NumberStyles.Integer, culture, out var ui16);
                    underlyingValue = ui16;
                    break;
                case TypeCode.UInt32:
                    success = uint.TryParse(str, NumberStyles.Integer, culture, out var ui32);
                    underlyingValue = ui32;
                    break;
                case TypeCode.UInt64:
                    success = ulong.TryParse(str, NumberStyles.Integer, culture, out var ui64);
                    underlyingValue = ui64;
                    break;
            }

            if (success)
            {
                result = (T)underlyingValue!;
                return true;
            }

            result = default!;
            return false;
        }

        private static bool TryConvertPrimitive(object value, CultureInfo culture, [MaybeNullWhen(false)] out T result)
        {
            try
            {
                result = s_convertPrimitive!(value, culture);
                return true;
            }
            catch
            {
                result = default!;
                return false;
            }
        }

        private static bool TryConvertEnum(object value, CultureInfo culture, [MaybeNullWhen(false)] out T result)
        {
            try
            {
                var underlyingType = s_underlyingType.GetEnumUnderlyingType();
                var underlyingValue = Convert.ChangeType(value, underlyingType, culture);
                result = (T)Enum.ToObject(s_underlyingType, underlyingValue);

                return true;
            }
            catch
            {
                result = default!;
                return false;
            }
        }

        private static bool TryConvertValueType(object value, CultureInfo culture, [MaybeNullWhen(false)] out T result)
        {
            try
            {
                if (value is IConvertible && s_underlyingType != typeof(object))
                {
                    result = (T)Convert.ChangeType(value, s_underlyingType, culture);
                    return true;
                }

                result = (T)value;
                return true;
            }
            catch
            {
                result = default!;
                return false;
            }
        }
    }
}
