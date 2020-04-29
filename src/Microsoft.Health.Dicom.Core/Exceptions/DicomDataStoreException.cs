// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    public class DicomDataStoreException : DicomServerException
    {
        public DicomDataStoreException(string message)
            : base(message)
        {
        }

        public DicomDataStoreException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public DicomDataStoreException(Exception innerException)
            : base(DicomCoreResource.DataStoreOperationFailed, innerException)
        {
        }
    }
}
