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

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    public class DeleteExtendedQueryTagService : IDeleteExtendedQueryTagService
    {
        private readonly IStoreFactory<IExtendedQueryTagStore> _extendedQueryTagStoreFactory;
        private readonly IDicomTagParser _dicomTagParser;

        public DeleteExtendedQueryTagService(IStoreFactory<IExtendedQueryTagStore> extendedQueryTagStoreFactory, IDicomTagParser dicomTagParser)
        {
            EnsureArg.IsNotNull(extendedQueryTagStoreFactory, nameof(extendedQueryTagStoreFactory));
            EnsureArg.IsNotNull(dicomTagParser, nameof(dicomTagParser));

            _extendedQueryTagStoreFactory = extendedQueryTagStoreFactory;
            _dicomTagParser = dicomTagParser;
        }

        public async Task DeleteExtendedQueryTagAsync(string tagPath, CancellationToken cancellationToken)
        {
            DicomTag[] tags;
            if (!_dicomTagParser.TryParse(tagPath, out tags, supportMultiple: false))
            {
                throw new InvalidExtendedQueryTagPathException(
                    string.Format(CultureInfo.InvariantCulture, DicomCoreResource.InvalidExtendedQueryTag, tagPath ?? string.Empty));
            }

            IExtendedQueryTagStore extendedQueryTagStore = await _extendedQueryTagStoreFactory.GetInstanceAsync(cancellationToken);

            string normalizedPath = tags[0].GetPath();
            IReadOnlyList<ExtendedQueryTagStoreEntry> extendedQueryTagEntries = await extendedQueryTagStore.GetExtendedQueryTagsAsync(normalizedPath, cancellationToken);

            if (extendedQueryTagEntries.Count > 0)
            {
                await extendedQueryTagStore.DeleteExtendedQueryTagAsync(normalizedPath, extendedQueryTagEntries[0].VR, false, cancellationToken);
            }
            else
            {
                throw new ExtendedQueryTagNotFoundException(string.Format(DicomCoreResource.ExtendedQueryTagNotFound, tagPath));
            }
        }
    }
}
