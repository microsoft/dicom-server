// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Models.Update;

namespace Microsoft.Health.Dicom.Functions.Update;

/// <summary>
/// Represents the input to the dicom update operation.
/// </summary>
public class UpdateInput
{
    public UpdateSpecification UpdateSpec { get; set; }

    /// <summary>
    /// Gets or sets the options that configure how the operation batches groups of Dicom update operations.
    /// </summary>
    /// <value>A set of update batching options.</value>
    public BatchingOptions Batching { get; set; }

    public string Dataset { get; set; }
}
