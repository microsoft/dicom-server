// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Messages.CustomTag;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public class GetCustomTagsService : IGetCustomTagsService
    {
        private readonly ICustomTagStore _customTagStore;
        private readonly IDicomTagParser _dicomTagParser;

        public GetCustomTagsService(ICustomTagStore customTagStore, IDicomTagParser dicomTagParser)
        {
            EnsureArg.IsNotNull(customTagStore, nameof(customTagStore));
            EnsureArg.IsNotNull(dicomTagParser, nameof(dicomTagParser));

            _customTagStore = customTagStore;
            _dicomTagParser = dicomTagParser;
        }

        public async Task<GetCustomTagResponse> GetCustomTagAsync(string tagPath, CancellationToken cancellationToken)
        {
            string internalTagPath = _dicomTagParser.ParseFormattedTagPath(tagPath);

            CustomTagEntry customTag = await _customTagStore.GetCustomTagAsync(internalTagPath, cancellationToken);

            return new GetCustomTagResponse(customTag);
        }

        public async Task<GetAllCustomTagsResponse> GetAllCustomTagsAsync(CancellationToken cancellationToken)
        {
            IEnumerable<CustomTagEntry> customTags = await _customTagStore.GetAllCustomTagsAsync(cancellationToken);

            return new GetAllCustomTagsResponse(customTags);
        }
    }
}
