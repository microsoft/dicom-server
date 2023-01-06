// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Core.Features.Retrieve;

namespace Microsoft.Health.Dicom.Core.Messages.Retrieve;

public class AcceptHeader
{
    public const double DefaultQuality = 1.0;
    public AcceptHeader(StringSegment mediaType, PayloadTypes payloadType, StringSegment transferSyntax = default, double? quality = DefaultQuality)
    {
        MediaType = mediaType;
        PayloadType = payloadType;
        TransferSyntax = transferSyntax;
        Quality = quality;
    }

    public StringSegment MediaType { get; }

    public PayloadTypes PayloadType { get; }

    public StringSegment TransferSyntax { get; }

    public double? Quality { get; }

    public bool IsSinglePart
    {
        get { return PayloadType == PayloadTypes.SinglePart; }
    }

    public override string ToString()
    {
        return $"MediaType:'{MediaType}', PayloadType:'{PayloadType}', TransferSyntax:'{TransferSyntax}', Quality:'{Quality}'";
    }
}
