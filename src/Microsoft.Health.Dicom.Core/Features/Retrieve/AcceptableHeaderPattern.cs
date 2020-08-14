// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public class AcceptableHeaderPattern
    {
        public AcceptableHeaderPattern(bool isMultipartRelated, string mediaType, bool isTransferSyntaxMandatory, string transferSyntaxWhenMissing, ISet<string> acceptableTransferSyntaxes)
        {
            IsMultipartRelated = isMultipartRelated;
            MediaType = mediaType;
            IsTransferSyntaxMandatory = isTransferSyntaxMandatory;
            TransferSyntaxWhenMissing = transferSyntaxWhenMissing;
            AcceptableTransferSyntaxes = acceptableTransferSyntaxes;
        }

        public bool IsMultipartRelated { get; }

        public string MediaType { get; }

        public bool IsTransferSyntaxMandatory { get; }

        public string TransferSyntaxWhenMissing { get; }

        public ISet<string> AcceptableTransferSyntaxes { get; }

        public bool IsAcceptable(AcceptHeader acceptHeader, out string transferSyntax)
        {
            transferSyntax = null;

            if (acceptHeader.IsMultipartRelated != IsMultipartRelated)
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

                // when transfer syntax is missed from accept header, use default one
                transferSyntax = TransferSyntaxWhenMissing;
                return true;
            }

            if (AcceptableTransferSyntaxes.Contains(acceptHeader.TransferSyntax.Value))
            {
                transferSyntax = acceptHeader.TransferSyntax.Value;
                return true;
            }

            return false;
        }
    }
}
