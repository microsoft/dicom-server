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
        public Uri GetRetrieveUri(Uri baseUri, string studyInstaceUid)
        {
            EnsureArg.IsNotNull(baseUri, nameof(baseUri));
            EnsureArg.IsNotNull(studyInstaceUid, nameof(studyInstaceUid));

            return new Uri(baseUri, $"/studies/{studyInstaceUid}");
        }

        /// <inheritdoc />
        public Uri GetRetrieveUri(Uri baseUri, DicomDatasetIdentifier dicomInstance)
        {
            EnsureArg.IsNotNull(baseUri, nameof(baseUri));
            EnsureArg.IsNotNull(dicomInstance, nameof(dicomInstance));

            return new Uri(baseUri, $"/studies/{dicomInstance.StudyInstanceUid}/series/{dicomInstance.SeriesInstanceUid}/instances/{dicomInstance.SopInstanceUid}");
        }
    }
}
