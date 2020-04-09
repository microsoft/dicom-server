// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Messages.Store;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    /// <summary>
    /// Provides functionality to build the response for the store transaction.
    /// </summary>
    public class DicomStoreResponseBuilder
    {
        private readonly IUrlResolver _urlResolver;

        private DicomDataset _dataset;

        public DicomStoreResponseBuilder(IUrlResolver urlResolver)
        {
            EnsureArg.IsNotNull(urlResolver, nameof(urlResolver));

            _urlResolver = urlResolver;
        }

        /// <summary>
        /// Builds the response.
        /// </summary>
        /// <param name="studyInstanceUid">If specified and there is at least one success, then the RetrieveURL for the study will be set.</param>
        /// <returns>An instance of <see cref="DicomStoreResponse"/> representing the response.</returns>
        public DicomStoreResponse BuildResponse(string studyInstanceUid)
        {
            bool hasSuccess = _dataset?.TryGetSequence(DicomTag.ReferencedSOPSequence, out _) ?? false;
            bool hasFailure = _dataset?.TryGetSequence(DicomTag.FailedSOPSequence, out _) ?? false;

            HttpStatusCode statusCode = HttpStatusCode.NoContent;

            if (hasSuccess && hasFailure)
            {
                // There are both successes and failures.
                statusCode = HttpStatusCode.Accepted;
            }
            else if (hasSuccess)
            {
                // There are only success.
                statusCode = HttpStatusCode.OK;
            }
            else if (hasFailure)
            {
                // There are only failures.
                statusCode = HttpStatusCode.Conflict;
            }

            if (hasSuccess && studyInstanceUid != null)
            {
                _dataset.Add(DicomTag.RetrieveURL, _urlResolver.ResolveRetrieveStudyUri(studyInstanceUid).ToString());
            }

            return new DicomStoreResponse(statusCode, _dataset);
        }

        /// <summary>
        /// Adds a successful entry to the response.
        /// </summary>
        /// <param name="dicomDataset">The DICOM dataset that was successfully stored.</param>
        public void AddSuccess(DicomDataset dicomDataset)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            CreateDatasetIfNeeded();

            if (!_dataset.TryGetSequence(DicomTag.ReferencedSOPSequence, out DicomSequence referencedSopSequence))
            {
                referencedSopSequence = new DicomSequence(DicomTag.ReferencedSOPSequence);

                _dataset.Add(referencedSopSequence);
            }

            var dicomInstance = dicomDataset.ToDicomInstanceIdentifier();

            var referencedSop = new DicomDataset()
            {
                { DicomTag.ReferencedSOPInstanceUID, dicomDataset.GetSingleValue<string>(DicomTag.SOPInstanceUID) },
                { DicomTag.RetrieveURL, _urlResolver.ResolveRetrieveInstanceUri(dicomInstance).ToString() },
                { DicomTag.ReferencedSOPClassUID, dicomDataset.GetSingleValueOrDefault<string>(DicomTag.SOPClassUID) },
            };

            referencedSopSequence.Items.Add(referencedSop);
        }

        /// <summary>
        /// Adds a failed entry to the response.
        /// </summary>
        /// <param name="dicomDataset">The DICOM dataset that failed to be stored.</param>
        /// <param name="failureReason">The failure reason.</param>
        public void AddFailure(DicomDataset dicomDataset = null, ushort failureReason = DicomStoreFailureCodes.ProcessingFailure)
        {
            CreateDatasetIfNeeded();

            if (!_dataset.TryGetSequence(DicomTag.FailedSOPSequence, out DicomSequence failedSopSequence))
            {
                failedSopSequence = new DicomSequence(DicomTag.FailedSOPSequence);

                _dataset.Add(failedSopSequence);
            }

            var failedSop = new DicomDataset()
            {
                { DicomTag.FailureReason, failureReason },
            };

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
