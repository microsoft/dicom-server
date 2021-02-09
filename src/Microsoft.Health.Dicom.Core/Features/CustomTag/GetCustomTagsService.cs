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

            IEnumerable<CustomTagEntry> customTags = await _customTagStore.GetCustomTagsAsync(internalTagPath, cancellationToken);

            if (!customTags.Any())
            {
                throw new CustomTagNotFoundException(string.Format(DicomCoreResource.CustomTagNotFound, tagPath));
            }
            else if (customTags.Count() > 1)
            {
                throw new MultipleCustomTagsFoundException(string.Format(DicomCoreResource.MultipleCustomTagsFound, tagPath));
            }

            return new GetCustomTagResponse(customTags.First());
        }

        public async Task<GetAllCustomTagsResponse> GetAllCustomTagsAsync(CancellationToken cancellationToken)
        {
            IEnumerable<CustomTagEntry> customTags = await _customTagStore.GetCustomTagsAsync(null, cancellationToken);

            if (!customTags.Any())
            {
                throw new CustomTagNotFoundException();
            }

            return new GetAllCustomTagsResponse(customTags);
        }
    }
}
