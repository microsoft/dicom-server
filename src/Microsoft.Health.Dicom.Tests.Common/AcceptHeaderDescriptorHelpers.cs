// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;

namespace Microsoft.Health.Dicom.Tests.Common
{
    public static class AcceptHeaderDescriptorHelpers
    {
        public static AcceptHeaderDescriptor CreateAcceptHeaderDescriptor(AcceptHeader acceptHeader, bool match = true)
        {
            return new AcceptHeaderDescriptor(
                payloadType: acceptHeader.PayloadType,
                mediaType: acceptHeader.MediaType.Value,
                isTransferSyntaxMandatory: true,
                transferSyntaxWhenMissing: string.Empty,
                acceptableTransferSyntaxes: match ? new HashSet<string>() { acceptHeader.TransferSyntax.Value } : new HashSet<string>());
        }
    }
}
