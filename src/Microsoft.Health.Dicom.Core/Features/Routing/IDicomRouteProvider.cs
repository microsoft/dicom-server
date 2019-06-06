// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Features.Routing
{
    public interface IDicomRouteProvider
    {
        /// <summary>
        /// Gets the study retrieve transaction endpoint.
        /// </summary>
        /// <param name="baseUri">The base address for the server providing the retrieve transaction capability.</param>
        /// <param name="studyInstanceUID">The instance UID of the study.</param>
        /// <returns>The study retrieve transaction endpoint.</returns>
        Uri GetStudyUri(Uri baseUri, string studyInstanceUID);

        /// <summary>
        /// Gets the series retrieve transaction endpoint.
        /// </summary>
        /// <param name="baseUri">The base address for the server providing the retrieve transaction capability.</param>
        /// <param name="studyInstanceUID">The instance UID of the study.</param>
        /// <param name="seriesInstanceUID">The instance UID of the series.</param>
        /// <returns>The series retrieve transaction endpoint.</returns>
        Uri GetSeriesUri(Uri baseUri, string studyInstanceUID, string seriesInstanceUID);

        /// <summary>
        /// Gets the study retrieve transaction endpoint.
        /// </summary>
        /// <param name="baseUri">The base address for the server providing the retrieve transaction capability.</param>
        /// <param name="studyInstanceUID">The instance UID of the study.</param>
        /// <param name="seriesInstanceUID">The instance UID of the series.</param>
        /// <param name="sopInstanceUID">The SOP instance UID.</param>
        /// <returns>The instance retrieve transaction endpoint.</returns>
        Uri GetInstanceUri(Uri baseUri, string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID);
    }
}
