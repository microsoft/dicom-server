// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using FellowOakDicom;

namespace Microsoft.Health.Dicom.Core.Features.Validation;

public static class ImplicitValueRepresentationValidator
{
    public static bool IsImplicitVR(DicomDataset dicomDataset)
    {
        EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
        EnsureArg.IsNotNull(dicomDataset.InternalTransferSyntax, nameof(dicomDataset.InternalTransferSyntax));

        return !dicomDataset.InternalTransferSyntax.IsExplicitVR;
    }
}
