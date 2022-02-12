// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    public class WorkitemInstanceIdentifier
    {
        private const StringComparison EqualsStringComparison = StringComparison.Ordinal;

        public WorkitemInstanceIdentifier(
            string workitemUid,
            long workitemKey,
            int partitionKey = default,
            long? watermark = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(workitemUid, nameof(workitemUid));

            WorkitemUid = workitemUid;
            WorkitemKey = workitemKey;
            PartitionKey = partitionKey;
            Watermark = watermark;
        }

        public int PartitionKey { get; }

        public long WorkitemKey { get; }

        public string WorkitemUid { get; }

        public long? Watermark { get; }

        public override bool Equals(object obj)
        {
            if (obj is WorkitemInstanceIdentifier identifier)
            {
                return WorkitemUid.Equals(identifier.WorkitemUid, EqualsStringComparison) &&
                        WorkitemKey == identifier.WorkitemKey &&
                        PartitionKey == identifier.PartitionKey;
            }

            return false;
        }

        public override int GetHashCode()
            => (PartitionKey + WorkitemUid + WorkitemKey.ToString()).GetHashCode(EqualsStringComparison);

        public override string ToString()
            => $"PartitionKey: {PartitionKey}, WorkitemUID: {WorkitemUid}, WorkitemKey: {WorkitemKey}";
    }
}
