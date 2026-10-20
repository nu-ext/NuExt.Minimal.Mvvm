using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Minimal.Mvvm;

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
            result = (T)Enum.Parse(s_underlyingType, str, ignoreCase: false);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or OverflowException)
        {
            // Not a valid enum name
        }
#endif

        // Try numeric string conversion
        try
        {
            var underlyingValue = Convert.ChangeType(str, s_enumUnderlyingType!, CultureInfo.InvariantCulture);
            result = (T)Enum.ToObject(s_underlyingType, underlyingValue);
            return true;
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
        {
            result = default!;
            return false;
        }
    }

    private static bool TryConvertPrimitiveFromString(string str, CultureInfo culture, [MaybeNullWhen(false)] out T result)
    {
        switch (s_typeCode)
        {
            case TypeCode.Boolean:
                if (bool.TryParse(str, out var b))
                {
                    result = (T)(object)b;
                    return true;
                }
                break;

            case TypeCode.Byte:
                if (byte.TryParse(str, NumberStyles.Integer, culture, out var bt))
                {
                    result = (T)(object)bt;
                    return true;
                }
                break;

            case TypeCode.Char:
                if (char.TryParse(str, out var c))
                {
                    result = (T)(object)c;
                    return true;
                }
                break;

            case TypeCode.DateTime:
                if (DateTime.TryParse(str, culture, DateTimeStyles.None, out var dt))
                {
                    result = (T)(object)dt;
                    return true;
                }
                break;

            case TypeCode.Decimal:
                if (decimal.TryParse(str, NumberStyles.Number, culture, out var d))
                {
                    result = (T)(object)d;
                    return true;
                }
                break;

            case TypeCode.Double:
                if (double.TryParse(str, NumberStyles.Float | NumberStyles.AllowThousands, culture, out var db))
                {
                    result = (T)(object)db;
                    return true;
                }
                break;

            case TypeCode.Int16:
                if (short.TryParse(str, NumberStyles.Integer, culture, out var i16))
                {
                    result = (T)(object)i16;
                    return true;
                }
                break;

            case TypeCode.Int32:
                if (int.TryParse(str, NumberStyles.Integer, culture, out var i32))
                {
                    result = (T)(object)i32;
                    return true;
                }
                break;

            case TypeCode.Int64:
                if (long.TryParse(str, NumberStyles.Integer, culture, out var i64))
                {
                    result = (T)(object)i64;
                    return true;
                }
                break;

            case TypeCode.SByte:
                if (sbyte.TryParse(str, NumberStyles.Integer, culture, out var sb))
                {
                    result = (T)(object)sb;
                    return true;
                }
                break;

            case TypeCode.Single:
                if (float.TryParse(str, NumberStyles.Float | NumberStyles.AllowThousands, culture, out var f))
                {
                    result = (T)(object)f;
                    return true;
                }
                break;

            case TypeCode.String:
                result = (T)(object)str;
                return true;

            case TypeCode.UInt16:
                if (ushort.TryParse(str, NumberStyles.Integer, culture, out var ui16))
                {
                    result = (T)(object)ui16;
                    return true;
                }
                break;

            case TypeCode.UInt32:
                if (uint.TryParse(str, NumberStyles.Integer, culture, out var ui32))
                {
                    result = (T)(object)ui32;
                    return true;
                }
                break;

            case TypeCode.UInt64:
                if (ulong.TryParse(str, NumberStyles.Integer, culture, out var ui64))
                {
                    result = (T)(object)ui64;
                    return true;
                }
                break;
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
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
        {
            result = default!;
            return false;
        }
    }

    private static bool TryConvertEnum(object value, CultureInfo culture, [MaybeNullWhen(false)] out T result)
    {
        try
        {
            var underlyingValue = Convert.ChangeType(value, s_enumUnderlyingType!, culture);
            result = (T)Enum.ToObject(s_underlyingType, underlyingValue);

            return true;
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
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
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
        {
            result = default!;
            return false;
        }
    }
}