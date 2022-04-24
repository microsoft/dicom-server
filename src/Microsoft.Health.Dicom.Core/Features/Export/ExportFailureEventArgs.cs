// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Models.Common;

namespace Microsoft.Health.Dicom.Core.Features.Export;

public sealed class ReadFailureEventArgs : EventArgs
{
    public DicomIdentifier Identifier { get; }

    public Exception Exception { get; }

    public ReadFailureEventArgs(DicomIdentifier identifier, Exception exception)
    {
        Identifier = identifier;
        Exception = EnsureArg.IsNotNull(exception, nameof(exception));
    }
}
