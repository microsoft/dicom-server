// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Export;

[SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Not meant for comparison.")]
public readonly struct SourceElement
{
    public VersionedInstanceIdentifier Identifier { get; }

    public ReadFailureEventArgs Failure { get; }

    private SourceElement(VersionedInstanceIdentifier identifier, ReadFailureEventArgs failure)
    {
        Identifier = identifier;
        Failure = failure;
    }

    public static SourceElement ForIdentifier(VersionedInstanceIdentifier identifier)
        => new SourceElement(EnsureArg.IsNotNull(identifier, nameof(identifier)), null);

    public static SourceElement ForFailure(ReadFailureEventArgs args)
        => new SourceElement(null, args);
}
