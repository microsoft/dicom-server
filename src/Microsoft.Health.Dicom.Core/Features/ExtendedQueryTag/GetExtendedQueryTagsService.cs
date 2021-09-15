// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    public class GetExtendedQueryTagsService : IGetExtendedQueryTagsService
    {
        private readonly IExtendedQueryTagStore _extendedQueryTagStore;
        private readonly IUrlResolver _urlResolver;

        public GetExtendedQueryTagsService(
            IExtendedQueryTagStore extendedQueryTagStore,
            IUrlResolver urlResolver)
        {
            _extendedQueryTagStore = EnsureArg.IsNotNull(extendedQueryTagStore, nameof(extendedQueryTagStore));
            _urlResolver = EnsureArg.IsNotNull(urlResolver, nameof(urlResolver));
        }

        public async Task<GetExtendedQueryTagResponse> GetExtendedQueryTagAsync(string tagPath, CancellationToken cancellationToken = default)
        {
            DicomTag tag;
            string numericalTagPath;
            if (DicomTagParser.TryParse(tagPath, out tag))
            {
                numericalTagPath = tag.GetPath();
            }
            else
            {
                throw new InvalidExtendedQueryTagPathException(string.Format(DicomCoreResource.InvalidExtendedQueryTag, tagPath ?? string.Empty));
            }

            ExtendedQueryTagStoreJoinEntry extendedQueryTag = await _extendedQueryTagStore.GetExtendedQueryTagAsync(numericalTagPath, cancellationToken);
            return new GetExtendedQueryTagResponse(extendedQueryTag.ToExtendedQueryTagEntry(_urlResolver));
        }

        public async Task<GetExtendedQueryTagsResponse> GetExtendedQueryTagsAsync(int limit, int offset = 0, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<ExtendedQueryTagStoreJoinEntry> extendedQueryTags = await _extendedQueryTagStore.GetExtendedQueryTagsAsync(limit, offset, cancellationToken);
            return new GetExtendedQueryTagsResponse(extendedQueryTags.Select(x => x.ToExtendedQueryTagEntry(_urlResolver)));
        }
    }
}
