// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    public class DeleteExtendedQueryTagService : IDeleteExtendedQueryTagService
    {
        private readonly IExtendedQueryTagStore _extendedQueryTagStore;
        private readonly IDicomTagParser _dicomTagParser;
        private readonly bool _enableExtendedQueryTags;

        public DeleteExtendedQueryTagService(IExtendedQueryTagStore extendedQueryTagStore, IDicomTagParser dicomTagParser, IOptions<FeatureConfiguration> featureConfiguration)
        {
            EnsureArg.IsNotNull(extendedQueryTagStore, nameof(extendedQueryTagStore));
            EnsureArg.IsNotNull(dicomTagParser, nameof(dicomTagParser));
            EnsureArg.IsNotNull(featureConfiguration?.Value, nameof(featureConfiguration));

            _extendedQueryTagStore = extendedQueryTagStore;
            _dicomTagParser = dicomTagParser;
            _enableExtendedQueryTags = featureConfiguration.Value.EnableExtendedQueryTags;
        }

        public async Task DeleteExtendedQueryTagAsync(string tagPath, CancellationToken cancellationToken)
        {
            if (!_enableExtendedQueryTags)
            {
                throw new ExtendedQueryTagFeatureDisabledException();
            }

            DicomTag[] tags;
            if (!_dicomTagParser.TryParse(tagPath, out tags, supportMultiple: false))
            {
                throw new InvalidExtendedQueryTagPathException(
                    string.Format(CultureInfo.InvariantCulture, DicomCoreResource.InvalidExtendedQueryTag, tagPath ?? string.Empty));
            }

            string normalizedPath = tags[0].GetPath();

            IReadOnlyList<ExtendedQueryTagStoreEntry> extendedQueryTagEntries = await _extendedQueryTagStore.GetExtendedQueryTagsAsync(normalizedPath, cancellationToken);

            if (extendedQueryTagEntries.Count > 0)
            {
                await _extendedQueryTagStore.DeleteExtendedQueryTagAsync(normalizedPath, extendedQueryTagEntries[0].VR, cancellationToken);
            }
        }
    }
}
