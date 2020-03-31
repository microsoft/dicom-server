// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Features.Routing
{
    public interface IDicomRouteProvider
    {
        Uri GetRetrieveUri(Uri baseUri, string studyInstaceUid);

        Uri GetRetrieveUri(Uri baseUri, DicomDatasetIdentifier dicomInstance);
    }
}
