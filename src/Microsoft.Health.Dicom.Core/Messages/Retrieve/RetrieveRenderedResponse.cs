// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Messages.Retrieve;
public class RetrieveRenderedResponse
{
    public RetrieveRenderedResponse(RetrieveResourceInstance responseStream, string contentType)
    {
        ResponseInstance = EnsureArg.IsNotNull(responseStream, nameof(responseStream));
        ContentType = EnsureArg.IsNotEmptyOrWhiteSpace(contentType, nameof(contentType));
    }

    /// <summary>
    /// Stream used in response
    /// </summary>
    public RetrieveResourceInstance ResponseInstance { get; }

    public string ContentType { get; }
}
