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
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Retrieve;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    public class DeleteExtendedQueryTagService : IDeleteExtendedQueryTagService
    {
        private readonly IExtendedQueryTagStore _extendedQueryTagStore;
        private readonly IDicomTagParser _dicomTagParser;
        private readonly IReindexService _reindexService;

        public DeleteExtendedQueryTagService(IExtendedQueryTagStore extendedQueryTagStore, IDicomTagParser dicomTagParser, IReindexService reindexService)
        {
            EnsureArg.IsNotNull(extendedQueryTagStore, nameof(extendedQueryTagStore));
            EnsureArg.IsNotNull(dicomTagParser, nameof(dicomTagParser));
            EnsureArg.IsNotNull(reindexService, nameof(reindexService));

            _extendedQueryTagStore = extendedQueryTagStore;
            _dicomTagParser = dicomTagParser;
            _reindexService = reindexService;
        }

        public async Task DeleteExtendedQueryTagAsync(string tagPath, CancellationToken cancellationToken)
        {
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
                var entry = extendedQueryTagEntries[0];

                // TODO: merge this into SQL transaction? seems not work with optionmization if merged.
                if (entry.Status == ExtendedQueryTagStatus.Adding)
                {
                    await _reindexService.RemoveTagFromReindexing(entry, cancellationToken);
                }
                await _extendedQueryTagStore.DeleteExtendedQueryTagAsync(normalizedPath, entry.VR, cancellationToken);
            }
            else
            {
                throw new ExtendedQueryTagNotFoundException(string.Format(DicomCoreResource.ExtendedQueryTagNotFound, tagPath));
            }
        }
    }
}
