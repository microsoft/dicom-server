// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    public class WorkitemDataset : DicomDataset
    {
        /// <inheritdoc/>
        public WorkitemDataset()
            : base()
        {
        }

        /// <inheritdoc/>
        public WorkitemDataset(DicomTransferSyntax internalTransferSyntax)
            : base(internalTransferSyntax)
        {
        }

        /// <inheritdoc/>
        public WorkitemDataset(params DicomItem[] items)
            : base(items)
        {
        }

        /// <inheritdoc/>
        public WorkitemDataset(IEnumerable<DicomItem> items)
            : base(items)
        {
        }

        private DicomUID GetSopClassUid()
        {
            return GetSingleValue<DicomUID>(DicomTag.SOPClassUID);
        }

        public WorkitemProcedureStepState GetState()
        {
            var stateValue = GetSingleValue<string>(DicomTag.ProcedureStepState);

            if (Enum.TryParse(stateValue, out WorkitemProcedureStepState result))
            {
                return result;
            }

            throw new DicomDataException("Unrecognized ProcedureStepState");
        }

        private string GetTransactionUid()
        {
            return GetSingleValue<string>(DicomTag.TransactionUID);
        }

        public bool ValidateForCreation()
        {
            if (!String.IsNullOrEmpty(GetTransactionUid())) return false;

            if (GetSopClassUid() != DicomUID.UnifiedProcedureStepPush) return false;

            if (GetState() != WorkitemProcedureStepState.Scheduled) return false;

            var performedProcedureSequence = GetSingleValue<DicomSequence>(DicomTag.UnifiedProcedureStepPerformedProcedureSequence);
            if (performedProcedureSequence.Items.Count > 0) return false;

            // TODO: validate required attributes
            return true;
        }
    }

    public enum WorkitemProcedureStepState
    {
        [JsonPropertyName("SCHEDULED")]
        Scheduled,
        [JsonPropertyName("IN PROGRESS")]
        InProgress,
        [JsonPropertyName("COMPLETED")]
        Completed,
        [JsonPropertyName("CANCELLED")]
        Cancelled
    }
}
