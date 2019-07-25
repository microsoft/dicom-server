// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Core.Features.Persistence;

namespace Microsoft.Health.Dicom.Core.Features.Routing
{
    public interface IDicomRouteProvider
    {
        Uri GetRetrieveUri(Uri baseUri, DicomStudy dicomStudy);

        Uri GetRetrieveUri(Uri baseUri, DicomSeries dicomSeries);

        Uri GetRetrieveUri(Uri baseUri, DicomInstance dicomInstance);
    }
}
