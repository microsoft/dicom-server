// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
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
    public class GetExtendedQueryTagsService : IGetExtendedQueryTagsService
    {
        private readonly IExtendedQueryTagStore _extendedQueryTagStore;
        private readonly IDicomTagParser _dicomTagParser;

        public GetExtendedQueryTagsService(IExtendedQueryTagStore extendedQueryTagStore, IDicomTagParser dicomTagParser)
        {
            EnsureArg.IsNotNull(extendedQueryTagStore, nameof(extendedQueryTagStore));
            EnsureArg.IsNotNull(dicomTagParser, nameof(dicomTagParser));

            _extendedQueryTagStore = extendedQueryTagStore;
            _dicomTagParser = dicomTagParser;
        }

        public async Task<GetExtendedQueryTagResponse> GetExtendedQueryTagAsync(string tagPath, CancellationToken cancellationToken)
        {
            DicomTag[] tags;
            if (_dicomTagParser.TryParse(tagPath, out tags, supportMultiple: false))
            {
                if (tags.Length > 1)
                {
                    throw new NotImplementedException(DicomCoreResource.SequentialDicomTagsNotSupported);
                }

                tagPath = tags[0].GetPath();
            }
            else
            {
                throw new InvalidExtendedQueryTagPathException(string.Format(DicomCoreResource.InvalidExtendedQueryTag, tagPath ?? string.Empty));
            }

            IReadOnlyList<ExtendedQueryTagStoreEntry> extendedQueryTags = await _extendedQueryTagStore.GetExtendedQueryTagsAsync(tagPath, cancellationToken);

            if (!extendedQueryTags.Any())
            {
                throw new ExtendedQueryTagNotFoundException(string.Format(DicomCoreResource.ExtendedQueryTagNotFound, tagPath));
            }

            return new GetExtendedQueryTagResponse(extendedQueryTags[0].ToExtendedQueryTagEntry());
        }

        public async Task<GetAllExtendedQueryTagsResponse> GetAllExtendedQueryTagsAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<ExtendedQueryTagStoreEntry> extendedQueryTags = await _extendedQueryTagStore.GetExtendedQueryTagsAsync(null, cancellationToken);

            return new GetAllExtendedQueryTagsResponse(extendedQueryTags.Select(x => x.ToExtendedQueryTagEntry()));
        }
    }
}
