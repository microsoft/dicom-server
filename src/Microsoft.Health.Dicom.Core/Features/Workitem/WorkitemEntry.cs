// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    public class WorkitemEntry
    {
        public int PartitionKey { get; }

        public long WorkitemKey { get; }

        public string WorkitemUid { get; }

        public DateTimeOffset CreatedDate { get; }

        public WorkitemDataset WorkitemDataset { get; }

        public WorkitemEntry(int partitionKey, long workitemKey, string workitemUid, DateTimeOffset createdDate = default, DicomDataset dataset = default)
        {
            PartitionKey = partitionKey;
            WorkitemKey = workitemKey;
            WorkitemUid = EnsureArg.IsNotNull(workitemUid, nameof(workitemUid));
            CreatedDate = createdDate;

            EnsureArg.IsNotNull(dataset, nameof(dataset));
            dataset.CopyTo(WorkitemDataset);
        }
    }
}
