// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using MediatR;

namespace Microsoft.Health.Dicom.Core.Messages.Delete
{
    public class DeleteDicomResourcesRequest : IRequest<DeleteDicomResourcesResponse>
    {
        public DeleteDicomResourcesRequest(string studyInstanceUID)
        {
            StudyInstanceUID = studyInstanceUID;
            ResourceType = ResourceType.Study;
        }

        public DeleteDicomResourcesRequest(string studyInstanceUID, string seriesInstanceUID)
        {
            StudyInstanceUID = studyInstanceUID;
            SeriesInstanceUID = seriesInstanceUID;
            ResourceType = ResourceType.Series;
        }

        public DeleteDicomResourcesRequest(string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID)
        {
            StudyInstanceUID = studyInstanceUID;
            SeriesInstanceUID = seriesInstanceUID;
            SopInstanceUID = sopInstanceUID;
            ResourceType = ResourceType.Instance;
        }

        public ResourceType ResourceType { get; }

        public string StudyInstanceUID { get; }

        public string SeriesInstanceUID { get; }

        public string SopInstanceUID { get; }
    }
}
