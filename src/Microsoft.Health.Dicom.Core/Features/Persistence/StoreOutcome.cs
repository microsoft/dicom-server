// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Persistence
{
    internal class StoreOutcome
    {
        public StoreOutcome(DicomIdentity dicomIdentity, bool isStored)
        {
            DicomIdentity = dicomIdentity;
            IsStored = isStored;
        }

        public DicomIdentity DicomIdentity { get; }

        public bool IsStored { get; }
    }
}
