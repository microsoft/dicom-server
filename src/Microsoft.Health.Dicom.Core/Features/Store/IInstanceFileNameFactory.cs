// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Store;
public interface IInstanceFileNameFactory
{
    string GetInstanceFileName(DetailedInstanceIdentifier instanceIdentifier);

    string GetInstanceMetadataFileName(DetailedInstanceIdentifier instanceIdentifier);
}
