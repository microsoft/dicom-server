// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    public class ExtendedQueryTagErrorsService : IExtendedQueryTagErrorsService
    {
        private readonly IExtendedQueryTagErrorStore _extendedQueryTagErrorStore;
        private readonly IDicomTagParser _dicomTagParser;

        public ExtendedQueryTagErrorsService(IExtendedQueryTagErrorStore extendedQueryTagStore, IDicomTagParser dicomTagParser)
        {
            _extendedQueryTagErrorStore = EnsureArg.IsNotNull(extendedQueryTagStore, nameof(extendedQueryTagStore));
            _dicomTagParser = EnsureArg.IsNotNull(dicomTagParser, nameof(dicomTagParser));
        }

        public async Task<GetExtendedQueryTagErrorsResponse> GetExtendedQueryTagErrorsAsync(string tagPath, CancellationToken cancellationToken = default)
        {
            string numericalTagPath = _dicomTagParser.TryParse(tagPath, out DicomTag[] tags, supportMultiple: false)
                ? tags[0].GetPath()
                : throw new InvalidExtendedQueryTagPathException(string.Format(DicomCoreResource.InvalidExtendedQueryTag, tagPath ?? string.Empty));

            IReadOnlyList<ExtendedQueryTagError> extendedQueryTagErrors = await _extendedQueryTagErrorStore.GetExtendedQueryTagErrorsAsync(numericalTagPath, cancellationToken);
            return new GetExtendedQueryTagErrorsResponse(extendedQueryTagErrors);
        }

        public Task<int> AddExtendedQueryTagErrorAsync(int tagKey, short errorCode, long watermark, DateTime createdTime, CancellationToken cancellationToken = default)
        {
            return _extendedQueryTagErrorStore.AddExtendedQueryTagErrorAsync(
                tagKey,
                errorCode,
                watermark,
                createdTime,
                cancellationToken);
        }
    }
}
