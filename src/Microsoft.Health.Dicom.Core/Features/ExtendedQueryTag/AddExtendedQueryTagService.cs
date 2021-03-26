// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    public class AddExtendedQueryTagService : IAddExtendedQueryTagService
    {
        private readonly IExtendedQueryTagStore _extendedQueryTagStore;
        private readonly IExtendedQueryTagEntryValidator _extendedQueryTagEntryValidator;

        public AddExtendedQueryTagService(IExtendedQueryTagStore extendedQueryTagStore, IExtendedQueryTagEntryValidator extendedQueryTagEntryValidator)
        {
            EnsureArg.IsNotNull(extendedQueryTagStore, nameof(extendedQueryTagStore));
            EnsureArg.IsNotNull(extendedQueryTagEntryValidator, nameof(extendedQueryTagEntryValidator));

            _extendedQueryTagStore = extendedQueryTagStore;
            _extendedQueryTagEntryValidator = extendedQueryTagEntryValidator;
        }

        public async Task<AddExtendedQueryTagResponse> AddExtendedQueryTagAsync(IEnumerable<ExtendedQueryTagEntry> extendedQueryTags, CancellationToken cancellationToken)
        {
            _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(extendedQueryTags);

            IEnumerable<ExtendedQueryTagEntry> result = extendedQueryTags.Select(item => item.Normalize(ExtendedQueryTagStatus.Ready));

            await _extendedQueryTagStore.AddExtendedQueryTagsAsync(result, cancellationToken);

            // Current solution is synchronouse, no job uri is generated, so always return emtpy.
            return new AddExtendedQueryTagResponse(job: string.Empty);
        }
    }
}
