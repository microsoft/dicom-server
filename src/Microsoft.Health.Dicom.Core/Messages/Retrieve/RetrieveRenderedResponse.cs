// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Messages.Retrieve;
public class RetrieveRenderedResponse
{
    public RetrieveRenderedResponse(Stream responseStream, long responseLength, string contentType)
    {
        ResponseStream = EnsureArg.IsNotNull(responseStream, nameof(responseStream));
        ContentType = EnsureArg.IsNotEmptyOrWhiteSpace(contentType, nameof(contentType));

        ResponseLength = responseLength;
    }

    public Stream ResponseStream { get; }

    public long ResponseLength { get; }

    public string ContentType { get; }
}
