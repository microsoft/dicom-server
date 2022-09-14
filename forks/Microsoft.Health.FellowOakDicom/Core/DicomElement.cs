// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using FellowOakDicom;
using FellowOakDicom.IO.Buffer;

namespace Microsoft.Health.FellowOakDicom.Core;

/// <summary>Decimal String (DS)</summary>
public class DicomDecimalString : DicomMultiStringOrNumberElement<decimal>
{
    #region Public Constructors

    public DicomDecimalString(DicomTag tag, params decimal[] values)
        : base(tag,
              new Func<decimal, string>(x => ToDecimalString(x)),
              new Func<string, decimal>(x => decimal.Parse(x, NumberStyles.Any, CultureInfo.InvariantCulture)),
              values)
    {
    }

    public DicomDecimalString(DicomTag tag, params string[] values)
        : base(tag, values)
    {
    }

    public DicomDecimalString(DicomTag tag, IByteBuffer data)
        : base(tag, data)
    {
    }

    #endregion

    #region Public Properties

    public override DicomVR ValueRepresentation => DicomVR.DS;

    #endregion

    #region Public Members

    public static string ToDecimalString(decimal value)
    {
        var valueString = value.ToString(CultureInfo.InvariantCulture);
        if (valueString.Length > 16)
        {
            valueString = value.ToString("G11", CultureInfo.InvariantCulture);
        }
        return valueString;
    }

    #endregion

}

/// <summary>Integer String (IS)</summary>
public class DicomIntegerString : DicomMultiStringOrNumberElement<int>
{
    #region Public Constructors

    public DicomIntegerString(DicomTag tag, params int[] values)
        : base(tag,
              new Func<int, string>(x => x.ToString(CultureInfo.InvariantCulture)),
              new Func<string, int>(x => int.Parse(x, NumberStyles.Integer | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture)),
              values)
    {
    }

    public DicomIntegerString(DicomTag tag, params string[] values)
        : base(tag, values)
    {
    }

    public DicomIntegerString(DicomTag tag, IByteBuffer data)
        : base(tag, data)
    {
    }

    #endregion

    #region Public Properties

    public override DicomVR ValueRepresentation => DicomVR.IS;

    #endregion
}

/// <summary>Signed Very Long (SV)</summary>
public class DicomSignedVeryLong : DicomMultiStringOrNumberElement<long>
{
    #region Public Constructors

    public DicomSignedVeryLong(DicomTag tag, params long[] values)
        : base(tag,
              new Func<long, string>(x => x.ToString()),
              new Func<string, long>(x => long.Parse(x, NumberStyles.Any, CultureInfo.InvariantCulture)),
              values)
    {
    }

    public DicomSignedVeryLong(DicomTag tag, IByteBuffer data)
        : base(tag, data)
    {
    }

    #endregion

    #region Public Properties

    public override DicomVR ValueRepresentation => DicomVR.SV;

    #endregion
}

/// <summary>Unsigned Very Long (UV)</summary>
public class DicomUnsignedVeryLong : DicomMultiStringOrNumberElement<ulong>
{
    #region Public Constructors

    public DicomUnsignedVeryLong(DicomTag tag, params ulong[] values)
        : base(tag,
              new Func<ulong, string>(x => x.ToString()),
              new Func<string, ulong>(x => ulong.Parse(x, NumberStyles.Any, CultureInfo.InvariantCulture)),
              values)
    {
    }

    public DicomUnsignedVeryLong(DicomTag tag, IByteBuffer data)
        : base(tag, data)
    {
    }

    #endregion

    #region Public Properties

    public override DicomVR ValueRepresentation => DicomVR.UV;

    #endregion
}

/// <summary>
/// Base class to handle Multi String/Number VR Types
/// e.g. DS, IS, SV, and UV
/// </summary>
public abstract class DicomMultiStringOrNumberElement<TType> : DicomMultiStringElement where TType : struct
{
    #region FIELDS

    private TType[] _values;

    private readonly Func<string, TType> _toNumber;

    private readonly DicomVR _vrType;

    #endregion

    #region Public Constructors

    public DicomMultiStringOrNumberElement(DicomTag tag, Func<TType, string> toString, Func<string, TType> toNumber, params TType[] values)
        : base(tag, values.Select(x => toString(x)).ToArray())
    {
        _toNumber = toNumber;
    }

    public DicomMultiStringOrNumberElement(DicomTag tag, params string[] values)
        : base(tag, values)
    {
    }

    public DicomMultiStringOrNumberElement(DicomTag tag, IByteBuffer data)
        : base(tag, null, data)
    {
    }

    #endregion

    #region Public Members

    public override T Get<T>(int item = -1)
    {
        // no need to parse values if returning string(s)
        if (typeof(T) == typeof(string) || typeof(T) == typeof(string[])) return base.Get<T>(item);

        if (item == -1)
        {
            item = 0;
        }

        if (_values == null)
        {
            _values = base.Get<string[]>().Select(x => _toNumber(x)).ToArray();
        }

        if (typeof(T).GetTypeInfo().IsArray)
        {
            var t = typeof(T).GetElementType();

            if (t == typeof(T)) return (T)(object)_values;

            var tu = Nullable.GetUnderlyingType(t) ?? t;
            var tmp = _values.Select(x => Convert.ChangeType(x, tu));

            if (t == typeof(object)) return (T)(object)tmp.ToArray();
            if (t == typeof(decimal)) return (T)(object)tmp.Cast<decimal>().ToArray();
            if (t == typeof(double)) return (T)(object)tmp.Cast<double>().ToArray();
            if (t == typeof(float)) return (T)(object)tmp.Cast<float>().ToArray();
            if (t == typeof(long)) return (T)(object)tmp.Cast<long>().ToArray();
            if (t == typeof(int)) return (T)(object)tmp.Cast<int>().ToArray();
            if (t == typeof(short)) return (T)(object)tmp.Cast<short>().ToArray();
            if (t == typeof(byte)) return (T)(object)tmp.Cast<byte>().ToArray();
            if (t == typeof(ulong)) return (T)(object)tmp.Cast<ulong>().ToArray();
            if (t == typeof(uint)) return (T)(object)tmp.Cast<uint>().ToArray();
            if (t == typeof(ushort)) return (T)(object)tmp.Cast<ushort>().ToArray();
            if (t == typeof(decimal?)) return (T)(object)tmp.Cast<decimal?>().ToArray();
            if (t == typeof(double?)) return (T)(object)tmp.Cast<double?>().ToArray();
            if (t == typeof(float?)) return (T)(object)tmp.Cast<float?>().ToArray();
            if (t == typeof(long?)) return (T)(object)tmp.Cast<long?>().ToArray();
            if (t == typeof(int?)) return (T)(object)tmp.Cast<int?>().ToArray();
            if (t == typeof(short?)) return (T)(object)tmp.Cast<short?>().ToArray();
            if (t == typeof(byte?)) return (T)(object)tmp.Cast<byte?>().ToArray();
            if (t == typeof(ulong?)) return (T)(object)tmp.Cast<ulong?>().ToArray();
            if (t == typeof(uint?)) return (T)(object)tmp.Cast<uint?>().ToArray();
            if (t == typeof(ushort?)) return (T)(object)tmp.Cast<ushort?>().ToArray();
        }
        else if (typeof(T).GetTypeInfo().IsValueType || typeof(T) == typeof(object))
        {
            if (item == -1) item = 0;
            if (item < 0 || item >= Count) throw new ArgumentOutOfRangeException(nameof(item), "Index is outside the range of available value items");

            // If nullable, need to apply conversions on underlying type (#212)
            var t = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

            return (T)Convert.ChangeType(_values[item], t);
        }

        return base.Get<T>(item);
    }

    #endregion
}
