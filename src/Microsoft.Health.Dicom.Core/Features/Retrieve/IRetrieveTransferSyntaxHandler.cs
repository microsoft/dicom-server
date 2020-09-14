// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public interface IRetrieveTransferSyntaxHandler
    {
        string GetTransferSyntax(ResourceType resourceType, IEnumerable<AcceptHeader> acceptHeaders, out AcceptHeaderDescriptor acceptHeaderDescriptor);
    }
}
