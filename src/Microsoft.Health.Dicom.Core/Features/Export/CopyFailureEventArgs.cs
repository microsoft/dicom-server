// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Export;

public sealed class CopyFailureEventArgs : EventArgs
{
    public VersionedInstanceIdentifier Identifier { get; }

    public Exception Exception { get; }

    public CopyFailureEventArgs(VersionedInstanceIdentifier identifier, Exception exception)
    {
        Identifier = EnsureArg.IsNotNull(identifier, nameof(identifier));
        Exception = EnsureArg.IsNotNull(exception, nameof(exception));
    }
}
