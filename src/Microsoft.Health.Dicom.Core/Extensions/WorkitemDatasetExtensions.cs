// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Extensions
{
    public static class WorkitemDatasetExtensions
    {
        /// <summary>
        /// Creates an instance of <see cref="WorkitemInstanceIdentifier"/> from <see cref="DicomDataset"/>.
        /// </summary>
        /// <param name="dicomDataset">The DICOM dataset to get the workitem identifiers from.</param>
        /// <param name="workitemKey"></param>
        /// <param name="partitionKey">Data Partition key</param>
        /// <returns>An instance of <see cref="WorkitemInstanceIdentifier"/> representing the <paramref name="dicomDataset"/>.</returns>
        public static WorkitemInstanceIdentifier ToWorkitemInstanceIdentifier(this DicomDataset dicomDataset, long workitemKey = default, int partitionKey = default)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            var workitemInstanceUid = dicomDataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, null as string)
                ?? dicomDataset.GetSingleValueOrDefault(DicomTag.AffectedSOPInstanceUID, string.Empty);

            return ToWorkitemInstanceIdentifier(workitemInstanceUid, workitemKey, partitionKey);
        }

        public static WorkitemInstanceIdentifier ToWorkitemInstanceIdentifier(string workitemInstanceUid, long workitemKey = default, int partitionKey = default)
        {
            EnsureArg.IsNotNull(workitemInstanceUid, nameof(workitemInstanceUid));

            return new WorkitemInstanceIdentifier(workitemInstanceUid, workitemKey, partitionKey);
        }
    }
}
