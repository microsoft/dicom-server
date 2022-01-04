// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Messages.WorkitemMessages;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    /// <summary>
    /// Provides functionality to build the response for the store transaction.
    /// </summary>
    public class WorkitemStoreResponseBuilder : IWorkitemStoreResponseBuilder
    {
        private readonly IUrlResolver _urlResolver;

        private DicomDataset _dataset;

        public WorkitemStoreResponseBuilder(IUrlResolver urlResolver)
        {
            EnsureArg.IsNotNull(urlResolver, nameof(urlResolver));

            _urlResolver = urlResolver;
        }

        /// <inheritdoc />
        public WorkitemStoreResponse BuildResponse(string workitemInstanceUid)
        {
            bool hasSuccess = _dataset?.TryGetSequence(DicomTag.ReferencedSOPSequence, out _) ?? false;
            bool hasFailure = _dataset?.TryGetSequence(DicomTag.FailedSOPSequence, out _) ?? false;

            var status = WorkitemStoreResponseStatus.None;

            if (hasSuccess)
            {
                // There are only success.
                status = WorkitemStoreResponseStatus.Success;
            }
            else if (hasFailure)
            {
                // There are only failures.
                status = WorkitemStoreResponseStatus.Failure;
            }

            if (hasSuccess && workitemInstanceUid != null)
            {
                _dataset.Add(DicomTag.RetrieveURL, _urlResolver.ResolveRetrieveWorkitemUri(workitemInstanceUid).ToString());
            }

            return new WorkitemStoreResponse(status, _dataset);
        }

        /// <inheritdoc />
        public void AddSuccess(DicomDataset dicomDataset)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            CreateDatasetIfNeeded();

            if (!_dataset.TryGetSequence(DicomTag.ReferencedSOPSequence, out DicomSequence referencedSopSequence))
            {
                referencedSopSequence = new DicomSequence(DicomTag.ReferencedSOPSequence);

                _dataset.Add(referencedSopSequence);
            }

            var dicomInstance = dicomDataset.ToInstanceIdentifier();

            var referencedSop = new DicomDataset()
            {
                { DicomTag.ReferencedSOPInstanceUID, dicomDataset.GetSingleValue<string>(DicomTag.SOPInstanceUID) },
                { DicomTag.RetrieveURL, _urlResolver.ResolveRetrieveInstanceUri(dicomInstance).ToString() },
                { DicomTag.ReferencedSOPClassUID, dicomDataset.GetSingleValueOrDefault<string>(DicomTag.SOPClassUID) },
            };

            referencedSopSequence.Items.Add(referencedSop);
        }

        /// <inheritdoc />
        public void AddFailure(DicomDataset dicomDataset, ushort failureReasonCode)
        {
            CreateDatasetIfNeeded();

            if (!_dataset.TryGetSequence(DicomTag.FailedSOPSequence, out DicomSequence failedSopSequence))
            {
                failedSopSequence = new DicomSequence(DicomTag.FailedSOPSequence);

                _dataset.Add(failedSopSequence);
            }

            var failedSop = new DicomDataset()
            {
                { DicomTag.FailureReason, failureReasonCode },
            };

            // We want to turn off auto validation for FailedSOPSequence item
            // because the failure might be caused by invalid UID value.
#pragma warning disable CS0618 // Type or member is obsolete
            failedSop.AutoValidate = false;
#pragma warning restore CS0618 // Type or member is obsolete

            failedSop.AddValueIfNotNull(
                DicomTag.ReferencedSOPClassUID,
                dicomDataset?.GetSingleValueOrDefault<string>(DicomTag.SOPClassUID));

            failedSop.AddValueIfNotNull(
                DicomTag.ReferencedSOPInstanceUID,
                dicomDataset?.GetSingleValueOrDefault<string>(DicomTag.SOPInstanceUID));

            failedSopSequence.Items.Add(failedSop);
        }

        private void CreateDatasetIfNeeded()
        {
            if (_dataset == null)
            {
                _dataset = new DicomDataset();
            }
        }
    }
}
