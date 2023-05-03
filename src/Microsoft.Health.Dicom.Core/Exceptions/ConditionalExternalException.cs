// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Exceptions;

/// <summary>
/// base class for exceptions that can have different customer experience based on accessing system or customer provided resource
/// </summary>
public abstract class ConditionalExternalException : DicomServerException
{
    protected ConditionalExternalException(string message, Exception innerException, bool isExternal = false)
        : base(message, innerException)
    {
        IsExternal = isExternal;
    }

    public bool IsExternal { get; }
}
