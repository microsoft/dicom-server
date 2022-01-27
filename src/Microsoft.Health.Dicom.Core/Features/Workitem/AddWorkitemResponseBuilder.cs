// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using FellowOakDicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Messages.WorkitemMessages;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    /// <summary>
    /// Provides functionality to build the response for the store transaction.
    /// </summary>
    public class AddWorkitemResponseBuilder : IAddWorkitemResponseBuilder
    {
        private readonly IUrlResolver _urlResolver;

        private DicomDataset _dataset;

        public AddWorkitemResponseBuilder(IUrlResolver urlResolver)
        {
            EnsureArg.IsNotNull(urlResolver, nameof(urlResolver));

            _urlResolver = urlResolver;
        }

        /// <inheritdoc />
        public AddWorkitemResponse BuildResponse()
        {
            Uri url = null;
            WorkitemResponseStatus status = WorkitemResponseStatus.Failure;

            if (_dataset.TryGetSingleValue<string>(DicomTag.AffectedSOPInstanceUID, out var workitemInstanceUid)
                && !_dataset.TryGetSingleValue<ushort>(DicomTag.FailureReason, out var _))
            {
                // There are only success.
                status = WorkitemResponseStatus.Success;
                url = _urlResolver.ResolveRetrieveWorkitemUri(workitemInstanceUid);
            }

            return new AddWorkitemResponse(status, url);
        }

        /// <inheritdoc />
        public void AddSuccess(DicomDataset dicomDataset)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            _dataset = dicomDataset;
        }

        /// <inheritdoc />
        public void AddFailure(DicomDataset dicomDataset, ushort failureReasonCode)
        {
            _dataset = dicomDataset ?? new DicomDataset();

            _dataset.Add(DicomTag.FailureReason, failureReasonCode);
        }
    }
}
