// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using MediatR;

namespace Microsoft.Health.Dicom.Core.Messages.Delete
{
    public class DeleteDicomResourcesRequest : IRequest<DeleteDicomResourcesResponse>
    {
        public DeleteDicomResourcesRequest(string studyInstanceUID, string seriesUID, string instanceUID)
        {
            StudyInstanceUID = studyInstanceUID;
            SeriesUID = seriesUID;
            InstanceUID = instanceUID;
            ResourceType = DeleteResourceType.Instance;
        }

        public DeleteDicomResourcesRequest(string studyInstanceUID, string seriesUID)
        {
            StudyInstanceUID = studyInstanceUID;
            SeriesUID = seriesUID;
            ResourceType = DeleteResourceType.Series;
        }

        public DeleteDicomResourcesRequest(string studyInstanceUID)
        {
            StudyInstanceUID = studyInstanceUID;
            ResourceType = DeleteResourceType.Study;
        }

        public DeleteResourceType ResourceType { get; }

        public string StudyInstanceUID { get; }

        public string SeriesUID { get; }

        public string InstanceUID { get; }
    }
}
