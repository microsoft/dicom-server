// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    public class DeleteExtendedQueryTagService : IDeleteExtendedQueryTagService
    {
        private readonly IExtendedQueryTagStore _extendedQueryTagStore;
        public DeleteExtendedQueryTagService(IExtendedQueryTagStore extendedQueryTagStore)
        {
            EnsureArg.IsNotNull(extendedQueryTagStore, nameof(extendedQueryTagStore));

            _extendedQueryTagStore = extendedQueryTagStore;
        }

        public async Task DeleteExtendedQueryTagAsync(string tagPath, CancellationToken cancellationToken)
        {
            DicomTag tag = ExtendedQueryTagValidator.ValidateTagPath(tagPath);
            string normalizedPath = tag.GetPath();
            IReadOnlyList<ExtendedQueryTagStoreEntry> extendedQueryTagEntries = await _extendedQueryTagStore.GetExtendedQueryTagsAsync(normalizedPath, cancellationToken);

            if (extendedQueryTagEntries.Count > 0)
            {
                await _extendedQueryTagStore.DeleteExtendedQueryTagAsync(normalizedPath, extendedQueryTagEntries[0].VR, cancellationToken);
            }
            else
            {
                throw new ExtendedQueryTagNotFoundException(string.Format(DicomCoreResource.ExtendedQueryTagNotFound, tagPath));
            }
        }
    }
}
