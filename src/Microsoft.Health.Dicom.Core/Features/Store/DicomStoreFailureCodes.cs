// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    /// <summary>
    /// If any of the failure codes are modified, please check they match the DICOM conformance statement.
    /// </summary>
    internal static class DicomStoreFailureCodes
    {
        public const ushort ProcessingFailure = 272;
        public const ushort ValidationFailure = 43264;
        public const ushort MismatchStudyInstanceUid = 43265;
        public const ushort SopInstanceAlreadyExists = 45070;
    }
}
