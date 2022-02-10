// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Store;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    /// <summary>
    /// Provides functionality to validate a <see cref="DicomDataset"/> to make sure it meets the minimum requirement when Adding.
    /// </summary>
    public class AddWorkitemDatasetValidator : WorkitemDatasetValidator
    {
        protected override void OnValidate(DicomDataset dicomDataset, string workitemInstanceUid)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            ValidateWorkitemInstanceUid(dicomDataset, workitemInstanceUid);

            ValidateRequiredTags(dicomDataset);

            ValidateProcedureStepState(dicomDataset, workitemInstanceUid);

            ValidateTransactionUID(dicomDataset, workitemInstanceUid);

            ValidateForDuplicateTagValuesInSequence(dicomDataset);
        }

        private static void ValidateRequiredTags(DicomDataset dicomDataset)
        {
            // Ensure required tags are present.
            foreach (DicomTag tag in QueryLimit.RequiredWorkitemSingleTags)
            {
                EnsureRequiredTagIsPresent(dicomDataset, tag);
            }

            // Ensure required sequence tags are present
            foreach (DicomTag tag in QueryLimit.RequiredWorkitemSequenceTags)
            {
                EnsureRequiredSequenceTagIsPresent(dicomDataset, tag);
            }
        }

        internal static void ValidateForDuplicateTagValuesInSequence(DicomDataset dicomDataset)
        {
            var sequenceList = dicomDataset.Where(t => t.ValueRepresentation == DicomVR.SQ).Cast<DicomSequence>();

            var tagValueMap = new Dictionary<string, string>();
            foreach (DicomDataset sqDataset in from sequence in sequenceList
                                               from DicomDataset sqDataset in sequence.Items
                                               select sqDataset)
            {
                tagValueMap.Clear();

                foreach ((DicomItem item, string tagPath) in
                            from item in sqDataset
                            let tagPath = item.Tag.GetPath()
                            select (item, tagPath))
                {
                    if (sqDataset.TryGetString(item.Tag, out var tagValue) &&
                        tagValueMap.ContainsKey(tagPath) &&
                        string.Equals(tagValue, tagValueMap[tagPath], StringComparison.Ordinal))
                    {
                        throw new DatasetValidationException(
                            FailureReasonCodes.ValidationFailure,
                            string.Format(
                                CultureInfo.InvariantCulture,
                                DicomCoreResource.DuplicateTagValueNotSupported,
                                tagValue,
                                tagPath));
                    }

                    tagValueMap[tagPath] = tagValue;
                }
            }
        }

        private static void ValidateTransactionUID(DicomDataset dicomDataset, string workitemInstanceUid)
        {
            // ProcedureStepState should be empty for create
            if (dicomDataset.TryGetString(DicomTag.TransactionUID, out var transactionUID) && !string.IsNullOrEmpty(transactionUID))
            {
                throw new DatasetValidationException(
                    FailureReasonCodes.ValidationFailure,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DicomCoreResource.InvalidTransactionUID,
                        transactionUID,
                        workitemInstanceUid));
            }
        }
    }
}
