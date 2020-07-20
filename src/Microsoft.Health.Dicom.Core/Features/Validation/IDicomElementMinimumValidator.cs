// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    public interface IDicomElementMinimumValidator
    {
        void Validate(DicomTag dicomTag, string value);
    }
}
