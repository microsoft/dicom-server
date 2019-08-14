// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Persistence
{
    public class DicomMetadata
    {
        public DicomMetadata(DicomDataset dicomDataset, bool resultCoalesced)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            DicomDataset = dicomDataset;
            ResultCoalesced = resultCoalesced;
        }

        public DicomDataset DicomDataset { get; }

        public bool ResultCoalesced { get; }
    }
}
