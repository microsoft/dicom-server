// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public class CustomTagEntry
    {
        public long TagId { get; set; }

        public string Path { get; set; }

        public string VR { get; set; }

        public CustomTagLevel Level { get; set; }

        public long Version { get; set; }

        public CustomTagStatus Status { get; set; }
    }
}
