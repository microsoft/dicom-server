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
using Microsoft.Health.Dicom.Core.Features.Routing;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    public class UpdateExtendedQueryTagService : IUpdateExtendedQueryTagService
    {
        private readonly IExtendedQueryTagStore _extendedQueryTagStore;
        private readonly IDicomTagParser _dicomTagParser;
        private readonly IUrlResolver _urlResolver;

        public UpdateExtendedQueryTagService(IExtendedQueryTagStore extendedQueryTagStore, IDicomTagParser dicomTagParser, IUrlResolver urlResolver)
        {
            _extendedQueryTagStore = EnsureArg.IsNotNull(extendedQueryTagStore, nameof(extendedQueryTagStore));
            _dicomTagParser = EnsureArg.IsNotNull(dicomTagParser, nameof(dicomTagParser));
            _urlResolver = EnsureArg.IsNotNull(urlResolver, nameof(urlResolver));
        }

        public async Task<GetExtendedQueryTagEntry> UpdateExtendedQueryTagAsync(string tagPath, UpdateExtendedQueryTagEntry tagEntry, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(tagPath, nameof(tagPath));
            EnsureArg.IsNotNull(tagEntry, nameof(tagEntry));
            EnsureArg.EnumIsDefined(tagEntry.QueryStatus, nameof(UpdateExtendedQueryTagEntry.QueryStatus));
            DicomTag[] tags;
            if (!_dicomTagParser.TryParse(tagPath, out tags, supportMultiple: false))
            {
                throw new InvalidExtendedQueryTagPathException(
                    string.Format(CultureInfo.InvariantCulture, DicomCoreResource.InvalidExtendedQueryTag, tagPath ?? string.Empty));
            }
            string normalizedPath = tags[0].GetPath();
            var entry = await _extendedQueryTagStore.UpdateQueryStatusAsync(normalizedPath, tagEntry.QueryStatus, cancellationToken);
            return entry.ToGetExtendedQueryTagEntry(_urlResolver);
        }
    }
}
