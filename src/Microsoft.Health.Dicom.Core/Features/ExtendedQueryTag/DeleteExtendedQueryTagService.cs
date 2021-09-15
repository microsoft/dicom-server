// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;

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
            DicomTag tag;
            if (!DicomTagParser.TryParse(tagPath, out tag))
            {
                throw new InvalidExtendedQueryTagPathException(
                    string.Format(CultureInfo.InvariantCulture, DicomCoreResource.InvalidExtendedQueryTag, tagPath ?? string.Empty));
            }

            string normalizedPath = tag.GetPath();
            ExtendedQueryTagStoreEntry extendedQueryTagEntry = await _extendedQueryTagStore.GetExtendedQueryTagAsync(normalizedPath, cancellationToken);
            await _extendedQueryTagStore.DeleteExtendedQueryTagAsync(normalizedPath, extendedQueryTagEntry.VR, cancellationToken);
        }
    }
}
