// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.Core.Features.Store;

namespace Microsoft.Health.Dicom.Tests.Common.Extensions
{
    public static class IDicomDatasetValidatorExtensions
    {
        public static void Validate(this IDicomDatasetValidator dicomDatasetValidator, DicomDataset dicomDataset, string requiredStudyInstanceUid)
        {
            dicomDatasetValidator.Validate(dicomDataset, Array.Empty<CustomTagEntry>(), requiredStudyInstanceUid);
        }
    }
}
