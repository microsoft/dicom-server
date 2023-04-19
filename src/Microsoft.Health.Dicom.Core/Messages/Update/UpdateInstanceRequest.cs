// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using MediatR;
using Microsoft.Health.Dicom.Core.Models.Update;

namespace Microsoft.Health.Dicom.Core.Messages.Update;

public class UpdateInstanceRequest : IRequest<UpdateInstanceResponse>
{
    public UpdateInstanceRequest(UpdateSpecification updateSpec)
    {
        UpdateSpec = updateSpec;
    }

    public UpdateSpecification UpdateSpec { get; }
}
