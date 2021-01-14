// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Messages.CustomTag;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public class GetCustomTagsService : IGetCustomTagsService
    {
        private readonly ICustomTagStore _customTagStore;

        public GetCustomTagsService(ICustomTagStore customTagStore)
        {
            EnsureArg.IsNotNull(customTagStore, nameof(customTagStore));

            _customTagStore = customTagStore;
        }

        public async Task<GetCustomTagResponse> GetCustomTagAsync(string tagPath, CancellationToken cancellationToken = default)
        {
            string internalTagPath = ConvertTagPathToInternalFormat(tagPath);

            CustomTagEntry customTag = await _customTagStore.GetCustomTagAsync(internalTagPath, cancellationToken);

            return new GetCustomTagResponse(customTag);
        }

        public async Task<GetAllCustomTagsResponse> GetAllCustomTagsAsync(CancellationToken cancellationToken = default)
        {
            IEnumerable<CustomTagEntry> customTags = await _customTagStore.GetAllCustomTagsAsync(cancellationToken);

            return new GetAllCustomTagsResponse(customTags);
        }

        private string ConvertTagPathToInternalFormat(string requestedTagPath)
        {
            return string.Join(string.Empty, requestedTagPath.Split('(', ',', ')', '.'));
        }
    }
}
