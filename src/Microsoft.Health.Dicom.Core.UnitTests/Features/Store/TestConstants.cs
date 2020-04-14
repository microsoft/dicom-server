// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Store
{
    internal static class TestConstants
    {
        public const ushort ProcessingFailureReasonCode = 272;
        public const ushort ValidationFailureReasonCode = 43264;
        public const ushort MismatchStudyInstanceUidReasonCode = 43265;
        public const ushort SopInstanceAlreadyExistsReasonCode = 45070;
    }
}
