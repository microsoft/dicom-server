// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using EnsureThat;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve;

public class AcceptHeaderDescriptor
{
    public AcceptHeaderDescriptor(
        PayloadTypes payloadType,
        string mediaType,
        bool isTransferSyntaxMandatory,
        string transferSyntaxWhenMissing,
        ISet<string> acceptableTransferSyntaxes
        )
    {
        EnsureArg.IsNotEmptyOrWhiteSpace(mediaType, nameof(mediaType));

        // When transfersyntax is not mandatory, transferSyntaxWhenMissing has to be presented
        if (!isTransferSyntaxMandatory)
        {
            EnsureArg.IsNotEmptyOrWhiteSpace(transferSyntaxWhenMissing, nameof(transferSyntaxWhenMissing));
        }

        EnsureArg.IsNotNull(acceptableTransferSyntaxes, nameof(acceptableTransferSyntaxes));

        PayloadType = payloadType;
        MediaType = mediaType;
        IsTransferSyntaxMandatory = isTransferSyntaxMandatory;
        TransferSyntaxWhenMissing = transferSyntaxWhenMissing;
        AcceptableTransferSyntaxes = acceptableTransferSyntaxes;
    }

    public PayloadTypes PayloadType { get; }

    public string MediaType { get; }

    public bool IsTransferSyntaxMandatory { get; }

    public string TransferSyntaxWhenMissing { get; }

    public ISet<string> AcceptableTransferSyntaxes { get; }

    public bool IsAcceptable(AcceptHeader acceptHeader)
    {
        EnsureArg.IsNotNull(acceptHeader, nameof(acceptHeader));

        // Check if payload type match
        if ((PayloadType & acceptHeader.PayloadType) == PayloadTypes.None)
        {
            return false;
        }

        if (!StringSegment.Equals(acceptHeader.MediaType, MediaType, StringComparison.InvariantCultureIgnoreCase))
        {
            return false;
        }

        if (StringSegment.IsNullOrEmpty(acceptHeader.TransferSyntax))
        {
            if (IsTransferSyntaxMandatory)
            {
                return false;
            }
            return true;
        }

        if (AcceptableTransferSyntaxes.Contains(acceptHeader.TransferSyntax.Value))
        {
            return true;
        }

        return false;
    }

    public StringSegment GetTransferSyntax(AcceptHeader acceptHeader)
    {
        EnsureArg.IsNotNull(acceptHeader, nameof(acceptHeader));

        // when transfer syntax not supplied and was not mandatory to be supplied, use default syntax
        if (!IsTransferSyntaxMandatory && StringSegment.IsNullOrEmpty(acceptHeader.TransferSyntax))
        {
            return TransferSyntaxWhenMissing;
        }

        return acceptHeader.TransferSyntax;
    }
}
