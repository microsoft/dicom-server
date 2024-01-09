// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Azure;

namespace Microsoft.Health.Dicom.Blob.Extensions;

internal static class AzureStorageErrorExtensions
{
    private static readonly List<string> Customer403ErrorCodes = new List<string>
    {
        "AuthorizationPermissionMismatch",
        "AuthorizationFailure",
        "KeyVaultEncryptionKeyNotFound",
    };

    private const string FileSystemNotFoundErrorCode = "FilesystemNotFound";

    public static bool IsConnectedStoreCustomerError(this RequestFailedException rfe)
    {
        return (rfe.Status == 403 && Customer403ErrorCodes.Contains(rfe.ErrorCode)) ||
            (rfe.Status == 404 && rfe.ErrorCode.Equals(FileSystemNotFoundErrorCode, StringComparison.OrdinalIgnoreCase)));
    }

    public static bool IsStorageAccountUnknownError(this Exception exception)
    {
        return exception.Message.Contains("No such host is known", StringComparison.OrdinalIgnoreCase) ||
            (exception is AggregateException ag && ag.InnerExceptions.Any(e => e.Message.Contains("No such host is known", StringComparison.OrdinalIgnoreCase)));
    }
}
