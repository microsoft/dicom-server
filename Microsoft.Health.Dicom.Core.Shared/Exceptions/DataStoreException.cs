// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Core.Shared;

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    public class DataStoreException : DicomServerException
    {
        public DataStoreException(string message)
            : base(message)
        {
        }

        public DataStoreException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public DataStoreException(Exception innerException)
            : base(DicomCoreResource.DataStoreOperationFailed, innerException)
        {
        }
    }
}
