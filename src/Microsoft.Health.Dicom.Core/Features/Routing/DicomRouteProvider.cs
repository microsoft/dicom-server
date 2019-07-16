// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Persistence;

namespace Microsoft.Health.Dicom.Core.Features.Routing
{
    public sealed class DicomRouteProvider : IDicomRouteProvider
    {
        /// <inheritdoc />
        public Uri GetRetrieveUri(Uri baseUri, DicomStudy dicomStudy)
        {
            EnsureArg.IsNotNull(baseUri, nameof(baseUri));
            EnsureArg.IsNotNull(dicomStudy, nameof(dicomStudy));

            return new Uri(baseUri, $"/studies/{dicomStudy.StudyInstanceUID}");
        }

        /// <inheritdoc />
        public Uri GetRetrieveUri(Uri baseUri, DicomSeries dicomSeries)
        {
            EnsureArg.IsNotNull(baseUri, nameof(baseUri));
            EnsureArg.IsNotNull(dicomSeries, nameof(dicomSeries));

            return new Uri(baseUri, $"/studies/{dicomSeries.StudyInstanceUID}/series/{dicomSeries.SeriesInstanceUID}");
        }

        /// <inheritdoc />
        public Uri GetRetrieveUri(Uri baseUri, DicomInstance dicomInstance)
        {
            EnsureArg.IsNotNull(baseUri, nameof(baseUri));
            EnsureArg.IsNotNull(dicomInstance, nameof(dicomInstance));

            return new Uri(baseUri, $"/studies/{dicomInstance.StudyInstanceUID}/series/{dicomInstance.SeriesInstanceUID}/instances/{dicomInstance.SopInstanceUID}");
        }
    }
}
