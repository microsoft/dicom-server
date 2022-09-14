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


/// <summary>Signed Very Long (SV)</summary>
public class DicomSignedVeryLong : DicomMultiStringElement
{
    #region FIELDS

    private long[] _values;

    #endregion

    #region Public Constructors

    public DicomSignedVeryLong(DicomTag tag, params long[] values)
        : base(tag, values.Select(x => x.ToString()).ToArray())
    {
    }

    public DicomSignedVeryLong(DicomTag tag, IByteBuffer data)
        : base(tag, null, data)
    {
    }

    #endregion

    #region Public Properties

    public override DicomVR ValueRepresentation => DicomVR.SV;

    #endregion

    #region Public Members

    public override T Get<T>(int item = -1)
    {
        // no need to parse values if returning string(s)
        if (typeof(T) == typeof(string) || typeof(T) == typeof(string[])) return base.Get<T>(item);

        if (_values == null)
        {
            _values =
                base.Get<string[]>()
                    .Select(x => long.Parse(x, NumberStyles.Any, CultureInfo.InvariantCulture))
                    .ToArray();
        }

        if (typeof(T).GetTypeInfo().IsArray)
        {
            var t = typeof(T).GetElementType();

            if (t == typeof(long)) return (T)(object)_values;

            var tu = Nullable.GetUnderlyingType(t) ?? t;
            var tmp = _values.Select(x => Convert.ChangeType(x, tu));

            if (t == typeof(object)) return (T)(object)tmp.ToArray();
            if (t == typeof(double)) return (T)(object)tmp.Cast<double>().ToArray();
            if (t == typeof(float)) return (T)(object)tmp.Cast<float>().ToArray();
            if (t == typeof(long)) return (T)(object)tmp.Cast<long>().ToArray();
            if (t == typeof(ulong)) return (T)(object)tmp.Cast<ulong>().ToArray();
            if (t == typeof(int)) return (T)(object)tmp.Cast<int>().ToArray();
            if (t == typeof(short)) return (T)(object)tmp.Cast<short>().ToArray();
            if (t == typeof(decimal?)) return (T)(object)tmp.Cast<decimal?>().ToArray();
            if (t == typeof(double?)) return (T)(object)tmp.Cast<double?>().ToArray();
            if (t == typeof(float?)) return (T)(object)tmp.Cast<float?>().ToArray();
            if (t == typeof(ulong?)) return (T)(object)tmp.Cast<ulong?>().ToArray();
            if (t == typeof(long?)) return (T)(object)tmp.Cast<long?>().ToArray();
            if (t == typeof(int?)) return (T)(object)tmp.Cast<int?>().ToArray();
            if (t == typeof(short?)) return (T)(object)tmp.Cast<short?>().ToArray();
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


/// <summary>Unsigned Very Long (UV)</summary>
public class DicomUnsignedVeryLong : DicomMultiStringElement
{
    #region FIELDS

    private ulong[] _values;

    #endregion

    #region Public Constructors

    public DicomUnsignedVeryLong(DicomTag tag, params ulong[] values)
        : base(tag, values.Select(x => x.ToString()).ToArray())
    {
    }

    public DicomUnsignedVeryLong(DicomTag tag, IByteBuffer data)
        : base(tag, null, data)
    {
    }

    #endregion

    #region Public Properties

    public override DicomVR ValueRepresentation => DicomVR.UV;

    #endregion

    #region Public Members

    public override T Get<T>(int item = -1)
    {
        // no need to parse values if returning string(s)
        if (typeof(T) == typeof(string) || typeof(T) == typeof(string[])) return base.Get<T>(item);

        if (_values == null)
        {
            _values =
                base.Get<string[]>()
                    .Select(x => ulong.Parse(x, NumberStyles.Any, CultureInfo.InvariantCulture))
                    .ToArray();
        }

        if (typeof(T).GetTypeInfo().IsArray)
        {
            var t = typeof(T).GetElementType();

            if (t == typeof(ulong)) return (T)(object)_values;

            var tu = Nullable.GetUnderlyingType(t) ?? t;
            var tmp = _values.Select(x => Convert.ChangeType(x, tu));

            if (t == typeof(object)) return (T)(object)tmp.ToArray();
            if (t == typeof(double)) return (T)(object)tmp.Cast<double>().ToArray();
            if (t == typeof(float)) return (T)(object)tmp.Cast<float>().ToArray();
            if (t == typeof(long)) return (T)(object)tmp.Cast<long>().ToArray();
            if (t == typeof(ulong)) return (T)(object)tmp.Cast<ulong>().ToArray();
            if (t == typeof(int)) return (T)(object)tmp.Cast<int>().ToArray();
            if (t == typeof(short)) return (T)(object)tmp.Cast<short>().ToArray();
            if (t == typeof(decimal?)) return (T)(object)tmp.Cast<decimal?>().ToArray();
            if (t == typeof(double?)) return (T)(object)tmp.Cast<double?>().ToArray();
            if (t == typeof(float?)) return (T)(object)tmp.Cast<float?>().ToArray();
            if (t == typeof(ulong?)) return (T)(object)tmp.Cast<ulong?>().ToArray();
            if (t == typeof(long?)) return (T)(object)tmp.Cast<long?>().ToArray();
            if (t == typeof(int?)) return (T)(object)tmp.Cast<int?>().ToArray();
            if (t == typeof(short?)) return (T)(object)tmp.Cast<short?>().ToArray();
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
