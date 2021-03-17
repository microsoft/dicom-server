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
                throw new InvalidCustomTagPathException(string.Format(DicomCoreResource.InvalidCustomTag, tagPath ?? string.Empty));
            }

            IReadOnlyList<CustomTagStoreEntry> customTags = await _customTagStore.GetCustomTagsAsync(tagPath, cancellationToken);

            if (!customTags.Any())
            {
                throw new CustomTagNotFoundException(string.Format(DicomCoreResource.CustomTagNotFound, tagPath));
            }

            return new GetCustomTagResponse(customTags[0].ToCustomTagEntry());
        }

        public async Task<GetAllCustomTagsResponse> GetAllCustomTagsAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<CustomTagStoreEntry> customTags = await _customTagStore.GetCustomTagsAsync(null, cancellationToken);

            return new GetAllCustomTagsResponse(customTags.Select(x => x.ToCustomTagEntry()));
        }
    }
}
