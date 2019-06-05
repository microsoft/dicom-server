// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Routing
{
    public sealed class DicomRouteProvider : IDicomRouteProvider
    {
        /// <inheritdoc />
        public Uri GetStudyUri(Uri baseUri, string studyInstanceUID)
        {
            EnsureArg.IsNotNull(baseUri, nameof(baseUri));
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUID, nameof(studyInstanceUID));

            return new Uri(baseUri, $"/studies/{studyInstanceUID}");
        }

        /// <inheritdoc />
        public Uri GetSeriesUri(Uri baseUri, string studyInstanceUID, string seriesInstanceUID)
        {
            EnsureArg.IsNotNull(baseUri, nameof(baseUri));
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUID, nameof(studyInstanceUID));
            EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUID, nameof(seriesInstanceUID));

            return new Uri(baseUri, $"/studies/{studyInstanceUID}/series/{seriesInstanceUID}");
        }

        /// <inheritdoc />
        public Uri GetInstanceUri(Uri baseUri, string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID)
        {
            EnsureArg.IsNotNull(baseUri, nameof(baseUri));
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUID, nameof(studyInstanceUID));
            EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUID, nameof(seriesInstanceUID));
            EnsureArg.IsNotNullOrWhiteSpace(sopInstanceUID, nameof(sopInstanceUID));

            return new Uri(baseUri, $"/studies/{studyInstanceUID}/series/{seriesInstanceUID}/instances/{sopInstanceUID}");
        }
    }
}
