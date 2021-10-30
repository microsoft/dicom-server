// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Model
{
    public class InstanceMetadata
    {
        public VersionedInstanceIdentifier VersionedInstanceIdentifier { get; set; }

        public string InternalTransferSyntax { get; set; }
    }
}
