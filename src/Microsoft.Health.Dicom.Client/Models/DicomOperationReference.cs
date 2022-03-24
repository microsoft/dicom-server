// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Client.Models;

/// <summary>
/// Represents a reference to an existing DICOM long-running operation.
/// </summary>
public class DicomOperationReference : OperationReference, IResourceReference<OperationState<DicomOperation>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DicomOperationReference"/> class.
    /// </summary>
    /// <param name="id">The unique operation ID.</param>
    /// <param name="href">The resource URL for the operation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="href"/> is <see langword="null"/>.</exception>
    public DicomOperationReference(Guid id, Uri href)
        : base(id, href)
    { }
}
