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
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public class DeleteCustomTagService : IDeleteCustomTagService
    {
        private readonly ICustomTagStore _customTagStore;
        private readonly IDicomTagParser _dicomTagParser;
        private readonly ILogger<DeleteCustomTagService> _logger;

        public DeleteCustomTagService(ICustomTagStore customTagStore, IDicomTagParser dicomTagParser, ILogger<DeleteCustomTagService> logger)
        {
            EnsureArg.IsNotNull(customTagStore, nameof(customTagStore));
            EnsureArg.IsNotNull(dicomTagParser, nameof(dicomTagParser));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _customTagStore = customTagStore;
            _dicomTagParser = dicomTagParser;
            _logger = logger;
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

            IEnumerable<CustomTagStoreEntry> customTagEntries = await _customTagStore.GetCustomTagsAsync(normalizedPath, cancellationToken);

            await _customTagStore.DeleteCustomTagAsync(normalizedPath, customTagEntries.First().VR, cancellationToken);
        }
    }
}
