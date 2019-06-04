// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Features.Routing
{
    public sealed class DicomRouteProvider : IDicomRouteProvider
    {
        public Uri GetStudyUri(string baseAddress, string studyInstanceUID)
            => new Uri($"{baseAddress}/studies/{studyInstanceUID}");

        public Uri GetSeriesUri(string baseAddress, string studyInstanceUID, string seriesInstanceUID)
            => new Uri($"{baseAddress}/studies/{studyInstanceUID}/series/{seriesInstanceUID}");

        public Uri GetInstanceUri(string baseAddress, string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID)
            => new Uri($"{baseAddress}/studies/{studyInstanceUID}/series/{seriesInstanceUID}/instances/{sopInstanceUID}");
    }
}
