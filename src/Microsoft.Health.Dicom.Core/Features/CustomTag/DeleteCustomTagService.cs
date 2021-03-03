// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public class DeleteCustomTagService : IDeleteCustomTagService
    {
        private readonly ICustomTagStore _customTagStore;
        private readonly IDicomTagParser _dicomTagParser;

        public DeleteCustomTagService(ICustomTagStore customTagStore, IDicomTagParser dicomTagParser)
        {
            EnsureArg.IsNotNull(customTagStore, nameof(customTagStore));
            EnsureArg.IsNotNull(dicomTagParser, nameof(dicomTagParser));

            _customTagStore = customTagStore;
            _dicomTagParser = dicomTagParser;
        }

        public async Task DeleteCustomTagAsync(string tagPath, CancellationToken cancellationToken)
        {
            DicomTag[] tags;
            if (!_dicomTagParser.TryParse(tagPath, out tags, supportMultiple: false))
            {
                throw new InvalidCustomTagPathException(
                    string.Format(CultureInfo.InvariantCulture, DicomCoreResource.InvalidCustomTag, tagPath ?? string.Empty));
            }

            string normalizedPath = tags[0].GetPath();

            IReadOnlyList<CustomTagStoreEntry> customTagEntries = await _customTagStore.GetCustomTagsAsync(normalizedPath, cancellationToken);

            if (customTagEntries.Count > 0)
            {
                await _customTagStore.DeleteCustomTagAsync(normalizedPath, customTagEntries[0].VR, cancellationToken);
            }
        }
    }
}
