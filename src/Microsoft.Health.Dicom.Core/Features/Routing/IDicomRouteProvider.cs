// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Features.Routing
{
    public interface IDicomRouteProvider
    {
        Uri GetStudyUri(Uri baseUri, string studyInstanceUID);

        Uri GetSeriesUri(Uri baseUri, string studyInstanceUID, string seriesInstanceUID);

        Uri GetInstanceUri(Uri baseUri, string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID);
    }
}
