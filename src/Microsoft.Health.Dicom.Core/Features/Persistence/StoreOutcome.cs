// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Features.Persistence
{
    public class StoreOutcome
    {
        public StoreOutcome(bool isStored, DicomIdentity dicomIdentity, Exception exception = null)
        {
            IsStored = isStored;
            DicomIdentity = dicomIdentity;
            Exception = exception;
        }

        public bool IsStored { get; }

        public DicomIdentity DicomIdentity { get; }

        public Exception Exception { get; }
    }
}
