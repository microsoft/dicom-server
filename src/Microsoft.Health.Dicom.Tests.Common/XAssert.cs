// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.Health.Dicom.Tests.Common;

/// <summary>
/// Represents a <see langword="static"/> collection of methods for test assertion that extend XUnit.
/// </summary>
public static class XAssert
{
    /// <summary>
    /// Verifies that the read-only set contains the given items.
    /// </summary>
    /// <typeparam name="T">The type of the items to be verified.</typeparam>
    /// <param name="items">The items expected to be in the set.</param>
    /// <param name="set">The set to be inspected.</param>
    /// <exception cref="ContainsException">Thrown when one or more <paramref name="items"/> is not present in the <paramref name="set"/>.</exception>
    public static void ContainsExactlyAll<T>(IEnumerable<T> items, IReadOnlySet<T> set)
    {
        int count = 0;
        foreach (T item in items)
        {
            Assert.Contains(item, set);
            count++;
        }

        Assert.Equal(set.Count, count);
    }
}
