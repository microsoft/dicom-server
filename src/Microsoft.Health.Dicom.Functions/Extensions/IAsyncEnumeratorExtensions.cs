// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using EnsureThat;

namespace Microsoft.Health.Dicom.Functions.Extensions;

internal static class IAsyncEnumeratorExtensions
{
    public static async IAsyncEnumerable<T> Take<T>(
        this IAsyncEnumerator<T> source,
        int count,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(source, nameof(source));

        for (int i = 0; i < Math.Max(0, count) && await source.MoveNextAsync(cancellationToken); i++)
        {
            yield return source.Current;
        }
    }
}
