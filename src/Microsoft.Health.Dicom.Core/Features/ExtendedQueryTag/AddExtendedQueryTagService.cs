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
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    public class AddExtendedQueryTagService : IAddExtendedQueryTagService
    {
        private readonly IExtendedQueryTagEntryValidator _tagValidator;
        private readonly IDicomOperationsClient _client;

        public AddExtendedQueryTagService(IExtendedQueryTagEntryValidator tagValidator, IDicomOperationsClient client)
        {
            EnsureArg.IsNotNull(tagValidator, nameof(tagValidator));
            EnsureArg.IsNotNull(client, nameof(client));

            _tagValidator = tagValidator;
            _client = client;
        }

        public async Task<AddExtendedQueryTagResponse> AddExtendedQueryTagsAsync(IEnumerable<AddExtendedQueryTagEntry> extendedQueryTags, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(extendedQueryTags, nameof(extendedQueryTags));

            _tagValidator.ValidateExtendedQueryTags(extendedQueryTags);
            List<AddExtendedQueryTagEntry> normalized = extendedQueryTags
                .Select(item => item.Normalize())
                .ToList();

            EnsureArg.HasItems(normalized, nameof(extendedQueryTags));

            return new AddExtendedQueryTagResponse(await _client.StartExtendedQueryTagAdditionAsync(normalized, cancellationToken));
        }
    }
}
