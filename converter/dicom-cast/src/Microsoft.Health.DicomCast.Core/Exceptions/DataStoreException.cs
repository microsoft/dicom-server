// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.DicomCast.Core.Exceptions
{
    public class DataStoreException : Exception
    {
        public DataStoreException(Exception innerException)
            : base(DicomCastCoreResource.DataStoreOperationFailed, innerException)
        {
        }
    }
}
