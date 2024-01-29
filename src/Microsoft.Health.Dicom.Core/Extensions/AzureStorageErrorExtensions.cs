// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;

namespace Microsoft.Health.Dicom.Core.Extensions;

internal static class AzureStorageErrorExtensions
{
    public static bool IsStorageAccountUnknownHostError(this Exception exception)
    {
        return exception.Message.Contains("No such host is known", StringComparison.OrdinalIgnoreCase) ||
            exception.Message.Contains("Name or service not known", StringComparison.OrdinalIgnoreCase) ||
            (exception is AggregateException ag && ag.InnerExceptions.All(e => e.Message.Contains("No such host is known", StringComparison.OrdinalIgnoreCase)) ||
            (exception is AggregateException agex && agex.InnerExceptions.All(e => e.Message.Contains("Name or service not known", StringComparison.OrdinalIgnoreCase))));
    }
}
