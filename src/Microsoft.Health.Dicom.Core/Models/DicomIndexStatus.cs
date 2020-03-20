// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Models
{
    /// <summary>
    /// Representing the index status.
    /// </summary>
    public static class DicomIndexStatus
    {
        public const byte Creating = 0;

        public const byte Created = 1;

        public const byte Deleted = 2;
    }
}
