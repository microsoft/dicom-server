// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Functions.Update.Models;

public class UpdateInstanceResponse
{
    public IReadOnlyList<InstanceMetadata> InstanceMetadataList { get; }

    public IReadOnlyList<string> Errors { get; }

    public UpdateInstanceResponse(IReadOnlyList<InstanceMetadata> instanceMetadataList, IReadOnlyList<string> errors)
    {
        InstanceMetadataList = EnsureArg.IsNotNull(instanceMetadataList, nameof(instanceMetadataList));
        Errors = errors;
    }
}
