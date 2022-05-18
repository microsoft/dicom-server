// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Exceptions;

public class DataStoreNotReadyException : DataStoreException
{
    public DataStoreNotReadyException(string message)
        : base(message)
    {
    }

    public DataStoreNotReadyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
