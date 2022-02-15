// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Model
{
    public class InstanceMetadata
    {
        public InstanceMetadata(VersionedInstanceIdentifier versionedInstanceIdentifier, InstanceProperties instanceProperties)
        {
            VersionedInstanceIdentifier = EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));
            InstanceProperties = instanceProperties;
        }
        public VersionedInstanceIdentifier VersionedInstanceIdentifier { get; }
        public InstanceProperties InstanceProperties { get; }
    }
}
