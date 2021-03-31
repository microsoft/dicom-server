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
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
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
        private readonly bool _enableExtendedQueryTags;

        public GetExtendedQueryTagsService(IExtendedQueryTagStore extendedQueryTagStore, IDicomTagParser dicomTagParser, IOptions<FeatureConfiguration> featureConfiguration)
        {
            EnsureArg.IsNotNull(extendedQueryTagStore, nameof(extendedQueryTagStore));
            EnsureArg.IsNotNull(dicomTagParser, nameof(dicomTagParser));
            EnsureArg.IsNotNull(featureConfiguration?.Value, nameof(featureConfiguration));

            _extendedQueryTagStore = extendedQueryTagStore;
            _dicomTagParser = dicomTagParser;
            _enableExtendedQueryTags = featureConfiguration.Value.EnableExtendedQueryTags;
        }

        public async Task<GetExtendedQueryTagResponse> GetExtendedQueryTagAsync(string tagPath, CancellationToken cancellationToken)
        {
            if (!_enableExtendedQueryTags)
            {
                throw new ExtendedQueryTagFeatureDisabledException();
            }

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
            if (!_enableExtendedQueryTags)
            {
                throw new ExtendedQueryTagFeatureDisabledException();
            }

            IReadOnlyList<ExtendedQueryTagStoreEntry> extendedQueryTags = await _extendedQueryTagStore.GetExtendedQueryTagsAsync(null, cancellationToken);

            return new GetAllExtendedQueryTagsResponse(extendedQueryTags.Select(x => x.ToExtendedQueryTagEntry()));
        }
    }
}
