// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    public class GetExtendedQueryTagsService : IGetExtendedQueryTagsService
    {
        private readonly IExtendedQueryTagStore _extendedQueryTagStore;
        public GetExtendedQueryTagsService(IExtendedQueryTagStore extendedQueryTagStore)
        {
            EnsureArg.IsNotNull(extendedQueryTagStore, nameof(extendedQueryTagStore));
            _extendedQueryTagStore = extendedQueryTagStore;
        }

        public async Task<GetExtendedQueryTagResponse> GetExtendedQueryTagAsync(string tagPath, CancellationToken cancellationToken)
        {
            string numericalTagPath = ExtendedQueryTagValidator.ValidateTagPath(tagPath).GetPath();
            IReadOnlyList<ExtendedQueryTagStoreEntry> extendedQueryTags = await _extendedQueryTagStore.GetExtendedQueryTagsAsync(numericalTagPath, cancellationToken);

            if (!extendedQueryTags.Any())
            {
                throw new ExtendedQueryTagNotFoundException(string.Format(DicomCoreResource.ExtendedQueryTagNotFound, tagPath));
            }

            return new GetExtendedQueryTagResponse(extendedQueryTags[0].ToExtendedQueryTagEntry());
        }

        public async Task<GetAllExtendedQueryTagsResponse> GetAllExtendedQueryTagsAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<ExtendedQueryTagStoreEntry> extendedQueryTags = await _extendedQueryTagStore.GetExtendedQueryTagsAsync((string)null, cancellationToken);

            return new GetAllExtendedQueryTagsResponse(extendedQueryTags.Select(x => x.ToExtendedQueryTagEntry()));
        }
    }
}
