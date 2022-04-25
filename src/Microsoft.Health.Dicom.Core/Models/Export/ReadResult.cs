// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Export;

[SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Not meant for comparison.")]
public readonly struct ReadResult
{
    public VersionedInstanceIdentifier Identifier { get; }

    public ReadFailureEventArgs Failure { get; }

    private ReadResult(VersionedInstanceIdentifier identifier, ReadFailureEventArgs failure)
    {
        Identifier = identifier;
        Failure = failure;
    }

    public static ReadResult ForIdentifier(VersionedInstanceIdentifier identifier)
        => new ReadResult(EnsureArg.IsNotNull(identifier, nameof(identifier)), null);

    public static ReadResult ForFailure(ReadFailureEventArgs args)
        => new ReadResult(null, args);
}
