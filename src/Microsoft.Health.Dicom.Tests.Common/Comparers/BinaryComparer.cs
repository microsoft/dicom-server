// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Hashing;

namespace Microsoft.Health.Dicom.Tests.Common.Comparers;

#nullable enable

/// <summary>
/// Represents both an <see cref="IComparer{T}"/> and <see cref="IEqualityComparer{T}"/> for contiguous regions of memory.
/// </summary>
/// <remarks>
/// <see cref="ReadOnlySpan{T}"/> is a ref struct and cannot be used as a type argument for
/// <see cref="IComparer{T}"/> and <see cref="IEqualityComparer{T}"/>. However, equivalent methods
/// are available without the interface implementation such as <see cref="Equals(ReadOnlySpan{byte}, ReadOnlySpan{byte})"/>.
/// </remarks>
public sealed class BinaryComparer : IComparer<byte[]>, IComparer<Stream>, IEqualityComparer<byte[]>, IEqualityComparer<Stream>
{
    /// <summary>
    /// Gets the singleton <see cref="BinaryComparer"/> instance for comparing byte arrays.
    /// </summary>
    /// <value>The singleton instance.</value>
    public static BinaryComparer Instance { get; } = new BinaryComparer();

    private BinaryComparer()
    { }

    /// <summary>
    /// Compares two arrays and returns a value indicating whether one is less than, equal to, or greater than the other.
    /// </summary>
    /// <remarks>
    /// Array values are first compared by their byte values, starting with first index, followed by their respective
    /// lengths such that shorter arrays are considered smaller values.
    /// </remarks>
    /// <param name="x">The first array to compare.</param>
    /// <param name="y">The second array to compare.</param>
    /// <returns>
    /// A signed integer that indicates the relative values of <paramref name="x"/> and <paramref name="y"/>,
    /// as shown in the following table.
    /// <list type="table">
    /// <listheader>
    /// <term>Return Value</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term>Less than zero</term>
    /// <description>
    /// <paramref name="x"/> is less than <paramref name="y"/>, or <paramref name="x"/> is <see langword="null"/>
    /// and <paramref name="y"/> is not <see langword="null"/>.
    /// </description>
    /// </item>
    /// <item>
    /// <term>Zero</term>
    /// <description>
    /// <paramref name="x"/> is equal to <paramref name="y"/>, or <paramref name="x"/> and <paramref name="y"/>
    /// are both <see langword="null"/>.
    /// </description>
    /// </item>
    /// <item>
    /// <term>Greater than zero</term>
    /// <description>
    /// <paramref name="x"/> is greater than <paramref name="y"/>, or <paramref name="y"/> is <see langword="null"/>
    /// and <paramref name="x"/> is not <see langword="null"/>.
    /// </description>
    /// </item>
    /// </list>
    /// </returns>
    public int Compare(byte[]? x, byte[]? y)
    {
        if (ReferenceEquals(x, y))
            return 0;
        else if (x is null)
            return -1;
        else if (y is null)
            return 1;

        return Compare(new ReadOnlySpan<byte>(x), new ReadOnlySpan<byte>(y));
    }

    /// <summary>
    /// Compares two regions of memory and returns a value indicating whether one is less than, equal to,
    /// or greater than the other.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Regions are first compared by their byte values, starting with first index, followed by their respective
    /// lengths such that shorter arrays are considered smaller values.
    /// </para>
    /// <para>
    /// Be careful of implicit type conversions. For example, <see cref="byte"/>[] can be implicitly cast to
    /// <see cref="ReadOnlySpan{T}"/>. The <see cref="Compare(byte[], byte[])"/> method will return a
    /// non-zero result given both a <see langword="null"/> value and an empty array, but when cast to
    /// <see cref="ReadOnlySpan{T}"/> the <see cref="Compare(ReadOnlySpan{byte}, ReadOnlySpan{byte})"/> method
    /// will return <c>0</c> instead.
    /// </para>
    /// </remarks>
    /// <param name="x">The first region to compare.</param>
    /// <param name="y">The second region to compare.</param>
    /// <returns>
    /// A signed integer that indicates the relative values of <paramref name="x"/> and <paramref name="y"/>,
    /// as shown in the following table.
    /// <list type="table">
    /// <listheader>
    /// <term>Return Value</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term>Less than zero</term>
    /// <description>
    /// <paramref name="x"/> is less than <paramref name="y"/>.
    /// </description>
    /// </item>
    /// <item>
    /// <term>Zero</term>
    /// <description>
    /// <paramref name="x"/> is equal to <paramref name="y"/>.
    /// </description>
    /// </item>
    /// <item>
    /// <term>Greater than zero</term>
    /// <description>
    /// <paramref name="x"/> is greater than <paramref name="y"/>.
    /// </description>
    /// </item>
    /// </list>
    /// </returns>
    public int Compare(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
    {
        int xCount = x.Length;
        int yCount = y.Length;

        unsafe
        {
            fixed (byte* xFixed = x)
            fixed (byte* yFixed = y)
            {
                nuint* px = (nuint*)xFixed;
                nuint* py = (nuint*)yFixed;

                for (; xCount >= sizeof(nuint) && yCount >= sizeof(nuint); px++, py++, xCount -= sizeof(nuint), yCount -= sizeof(nuint))
                {
                    if (*px < *py)
                        return -1;
                    if (*px > *py)
                        return 1;
                }

                return Compare((byte*)px, (byte*)py, xCount, yCount);
            }
        }
    }

    /// <summary>
    /// Compares two streams and returns a value indicating whether one is less than, equal to, or greater than the other.
    /// </summary>
    /// <remarks>
    /// Streams are first compared by their byte values, starting from their current position, followed by their
    /// respective lengths such that shorter streams are considered smaller values.
    /// </remarks>
    /// <param name="x">The first stream to compare.</param>
    /// <param name="y">The second stream to compare.</param>
    /// <returns>
    /// A signed integer that indicates the relative values of <paramref name="x"/> and <paramref name="y"/>,
    /// as shown in the following table.
    /// <list type="table">
    /// <listheader>
    /// <term>Return Value</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term>Less than zero</term>
    /// <description>
    /// <paramref name="x"/> is less than <paramref name="y"/>, or <paramref name="x"/> is <see langword="null"/>
    /// and <paramref name="y"/> is not <see langword="null"/>.
    /// </description>
    /// </item>
    /// <item>
    /// <term>Zero</term>
    /// <description>
    /// <paramref name="x"/> is equal to <paramref name="y"/>, or <paramref name="x"/> and <paramref name="y"/>
    /// are both <see langword="null"/>.
    /// </description>
    /// </item>
    /// <item>
    /// <term>Greater than zero</term>
    /// <description>
    /// <paramref name="x"/> is greater than <paramref name="y"/>, or <paramref name="y"/> is <see langword="null"/>
    /// and <paramref name="x"/> is not <see langword="null"/>.
    /// </description>
    /// </item>
    /// </list>
    /// </returns>
    public int Compare(Stream? x, Stream? y)
    {
        if (ReferenceEquals(x, y))
            return 0;
        else if (x is null)
            return -1;
        else if (y is null)
            return 1;

        unsafe
        {
            nuint xWord;
            nuint yWord;
            Span<byte> xBuffer = new(&xWord, sizeof(nuint));
            Span<byte> yBuffer = new(&yWord, sizeof(nuint));

            int cmp;
            int xCount;
            int yCount;

            do
            {
                xCount = FillBuffer(x, xBuffer);
                yCount = FillBuffer(y, yBuffer);

                if (xCount < sizeof(nuint) || yCount < sizeof(nuint))
                    return Compare((byte*)&xWord, (byte*)&yWord, xCount, yCount);

                cmp = xWord.CompareTo(yWord);
            } while (cmp == 0);

            return cmp;
        }
    }

    /// <summary>
    /// Determines whether the specified arrays are equal.
    /// </summary>
    /// <param name="x">The first array to compare.</param>
    /// <param name="y">The second array to compare.</param>
    /// <returns>
    /// <see langword="true"/> if the specified arrays are either both <see langword="null"/> or of equal length
    /// and contain the same byte values in the same order; otherwise, <see langword="false"/>.
    /// </returns>
    public bool Equals(byte[]? x, byte[]? y)
    {
        if (ReferenceEquals(x, y))
            return true;

        // We can safely short-circuit as the above predicate checks if they're both null
        if (x is null || y is null)
            return false;

        return Equals(new ReadOnlySpan<byte>(x), new ReadOnlySpan<byte>(y));
    }

    /// <summary>
    /// Determines whether the specified regions of memory are equal.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Be careful of implicit type conversions.
    /// </para>
    /// <para>For example, <see cref="byte"/>[] can be implicitly cast to
    /// <see cref="ReadOnlySpan{T}"/>. The <see cref="Equals(byte[], byte[])"/> method will return
    /// <see langword="false"/> given both a <see langword="null"/> value and an empty array, but when cast to
    /// <see cref="ReadOnlySpan{T}"/> the <see cref="Equals(ReadOnlySpan{byte}, ReadOnlySpan{byte})"/> method
    /// will return <see langword="true"/> instead.
    /// </para>
    /// </remarks>
    /// <param name="x">The first region to compare.</param>
    /// <param name="y">The second region to compare.</param>
    /// <returns>
    /// <see langword="true"/> if the specified regions are of equal length
    /// and contain the same byte values in the same order; otherwise, <see langword="false"/>.
    /// </returns>
    public bool Equals(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
    {
        if (x.Length != y.Length)
            return false;

        unsafe
        {
            fixed (byte* xFixed = x)
            fixed (byte* yFixed = y)
            {
                int count = x.Length;
                nuint* px = (nuint*)xFixed;
                nuint* py = (nuint*)yFixed;

                for (; count >= sizeof(nuint); px++, py++, count -= sizeof(nuint))
                {
                    if (*px != *py)
                        return false;
                }

                return Equals((byte*)px, (byte*)py, count);
            }
        }
    }

    /// <summary>
    /// Determines whether the specified streams are equal.
    /// </summary>
    /// <param name="x">The first stream to compare.</param>
    /// <param name="y">The second stream to compare.</param>
    /// <returns>
    /// <see langword="true"/> if the specified streams are either both <see langword="null"/> or of equal length
    /// and contain the same byte values in the same order; otherwise, <see langword="false"/>.
    /// </returns>
    public bool Equals(Stream? x, Stream? y)
    {
        if (ReferenceEquals(x, y))
            return true;

        // We can safely short-circuit as the above predicate checks if they're both null
        if (x is null || y is null)
            return false;

        unsafe
        {
            nuint xWord;
            nuint yWord;
            Span<byte> xBuffer = new(&xWord, sizeof(nuint));
            Span<byte> yBuffer = new(&yWord, sizeof(nuint));

            int xCount;
            int yCount;

            do
            {
                xCount = FillBuffer(x, xBuffer);
                yCount = FillBuffer(y, yBuffer);

                if (xCount < sizeof(nuint) || yCount < sizeof(nuint))
                    return Equals((byte*)&xWord, (byte*)&yWord, xCount, yCount);
            } while (xWord == yWord);

            return false;
        }
    }

    /// <summary>
    /// Returns a hash code for the specified array.
    /// </summary>
    /// <param name="obj">The array for which a hash code is to be returned.</param>
    /// <returns>A hash code for the specified array.</returns>
    public int GetHashCode(byte[] obj)
        => obj is null ? 0 : GetHashCode(new ReadOnlySpan<byte>(obj));

    /// <summary>
    /// Returns a hash code for the specified region of memory.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Be careful of implicit type conversions.
    /// </para>
    /// <para>For example, <see cref="byte"/>[] can be implicitly cast to
    /// <see cref="ReadOnlySpan{T}"/>. The <see cref="GetHashCode(byte[])"/> method will return different values
    /// for <see langword="null"/> values and empty arrays, but when cast to <see cref="ReadOnlySpan{T}"/>
    /// the <see cref="GetHashCode(ReadOnlySpan{byte})"/> method will return equivalent values.
    /// </para>
    /// </remarks>
    /// <param name="obj">The region for which a hash code is to be returned.</param>
    /// <returns>A hash code for the specified region.</returns>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "ReadOnlySpan<T> cannot be used as type argument.")]
    public int GetHashCode(ReadOnlySpan<byte> obj)
    {
        // TODO: Should algo be based on architecture?
        unsafe
        {
            int hash;
            XxHash32.Hash(obj, new Span<byte>(&hash, 4));
            return hash;
        }
    }

    /// <summary>
    /// Returns a hash code for the specified stream.
    /// </summary>
    /// <param name="obj">The stream for which a hash code is to be returned.</param>
    /// <returns>A hash code for the specified stream.</returns>
    public int GetHashCode(Stream obj)
    {
        if (obj is null)
            return 0;

        XxHash32 algo = new();
        algo.Append(obj);

        unsafe
        {
            int hash;
            algo.GetCurrentHash(new Span<byte>(&hash, 4));
            return hash;
        }
    }

    private static unsafe int Compare(byte* x, byte* y, int xCount, int yCount)
    {
        // TODO: Unroll loop for better performance as we know count will always be < sizeof(nuint)
        int cmp = 0;
        int minCount = xCount < yCount ? xCount : yCount;
        for (int i = 0; i < minCount; i++)
        {
            cmp = (x + i)->CompareTo(*(y + i));
            if (cmp != 0)
                return cmp;
        }

        return xCount.CompareTo(yCount);
    }

    private static unsafe bool Equals(byte* x, byte* y, int count)
    {
        // TODO: Unroll loop for better performance as we know count will always be < sizeof(nuint)
        for (int i = 0; i < count; i++)
        {
            if (*(x + i) != *(y + i))
                return false;
        }

        return true;
    }

    private static unsafe bool Equals(byte* x, byte* y, int xCount, int yCount)
    {
        // TODO: Unroll loop for better performance as we know count will always be < sizeof(nuint)
        int minCount = xCount < yCount ? xCount : yCount;
        for (int i = 0; i < minCount; i++)
        {
            if (*(x + i) != *(y + i))
                return false;
        }

        return xCount == yCount;
    }

    private static int FillBuffer(Stream stream, Span<byte> buffer)
    {
        int read = 0;
        while (buffer.Length > 0)
        {
            int next = stream.Read(buffer);
            if (next == 0)
                break;

            read += next;
            buffer = buffer[next..];
        }

        return read;
    }
}
