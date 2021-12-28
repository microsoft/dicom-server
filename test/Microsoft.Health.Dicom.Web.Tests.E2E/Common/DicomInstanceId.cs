// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Common
{
    internal class DicomInstanceId
    {
        public DicomInstanceId(string partitionName, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            StudyInstanceUid = EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
            SeriesInstanceUid = EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid));
            SopInstanceUid = EnsureArg.IsNotNullOrWhiteSpace(sopInstanceUid, nameof(sopInstanceUid));
            PartitionName = partitionName;
        }

        public string StudyInstanceUid { get; }
        public string SeriesInstanceUid { get; }
        public string SopInstanceUid { get; }
        public string PartitionName { get; }

        public static DicomInstanceId FromDicomFile(DicomFile dicomFile, string partitionName = default)
        {
            InstanceIdentifier instanceIdentifier = dicomFile.Dataset.ToInstanceIdentifier();
            return new DicomInstanceId(partitionName, instanceIdentifier.StudyInstanceUid, instanceIdentifier.SeriesInstanceUid, instanceIdentifier.SopInstanceUid);
        }
    }
}
