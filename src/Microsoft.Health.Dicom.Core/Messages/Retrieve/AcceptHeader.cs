// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Primitives;

namespace Microsoft.Health.Dicom.Core.Messages.Retrieve
{
    public class AcceptHeader
    {
        public AcceptHeader(StringSegment mediaType, bool isMultipartRelated, StringSegment transferSyntax = default, double? quality = null)
        {
            MediaType = mediaType;
            IsMultipartRelated = isMultipartRelated;
            TransferSyntax = transferSyntax;
            Quality = quality;
        }

        public StringSegment MediaType { get; }

        public bool IsMultipartRelated { get; }

        public StringSegment TransferSyntax { get; }

        public double? Quality { get; }
    }
}
