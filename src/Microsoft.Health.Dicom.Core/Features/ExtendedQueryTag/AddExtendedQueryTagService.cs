// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

public class AddExtendedQueryTagService : IAddExtendedQueryTagService
{
    private readonly IExtendedQueryTagStore _extendedQueryTagStore;
    private readonly IGuidFactory _guidFactory;
    private readonly IDicomOperationsClient _client;
    private readonly IExtendedQueryTagEntryValidator _extendedQueryTagEntryValidator;
    private readonly int _maxAllowedCount;

    private static readonly OperationQueryCondition<DicomOperation> ReindexQuery = new OperationQueryCondition<DicomOperation>
    {
        Operations = new DicomOperation[] { DicomOperation.Reindex },
        Statuses = new OperationStatus[]
        {
            OperationStatus.NotStarted,
            OperationStatus.Running,
        }
    };

    public AddExtendedQueryTagService(
        IExtendedQueryTagStore extendedQueryTagStore,
        IGuidFactory guidFactory,
        IDicomOperationsClient client,
        IExtendedQueryTagEntryValidator extendedQueryTagEntryValidator,
        IOptions<ExtendedQueryTagConfiguration> extendedQueryTagConfiguration)
    {
        EnsureArg.IsNotNull(extendedQueryTagStore, nameof(extendedQueryTagStore));
        EnsureArg.IsNotNull(guidFactory, nameof(guidFactory));
        EnsureArg.IsNotNull(client, nameof(client));
        EnsureArg.IsNotNull(extendedQueryTagEntryValidator, nameof(extendedQueryTagEntryValidator));
        EnsureArg.IsNotNull(extendedQueryTagConfiguration?.Value, nameof(extendedQueryTagConfiguration));

        _extendedQueryTagStore = extendedQueryTagStore;
        _guidFactory = guidFactory;
        _client = client;
        _extendedQueryTagEntryValidator = extendedQueryTagEntryValidator;
        _maxAllowedCount = extendedQueryTagConfiguration.Value.MaxAllowedCount;
    }

    public async Task<OperationReference> AddExtendedQueryTagsAsync(
        IEnumerable<AddExtendedQueryTagEntry> extendedQueryTags,
        CancellationToken cancellationToken = default)
    {
        // Check if any extended query tag operation is ongoing
        OperationReference activeReindex = await _client
            .FindOperationsAsync(ReindexQuery, cancellationToken)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeReindex != null)
            throw new ExistingOperationException(activeReindex, "re-index");

        _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(extendedQueryTags);
        var normalized = extendedQueryTags
            .Select(item => item.Normalize())
            .ToList();

        // Add the extended query tags to the DB
        IReadOnlyList<ExtendedQueryTagStoreEntry> added = await _extendedQueryTagStore.AddExtendedQueryTagsAsync(
            normalized,
            _maxAllowedCount,
            ready: false,
            cancellationToken: cancellationToken);

        // Start re-indexing
        var tagKeys = added.Select(x => x.Key).ToList();
        OperationReference operation = await _client.StartReindexingInstancesAsync(_guidFactory.Create(), tagKeys, cancellationToken);

        // Associate the tags to the operation and confirm their processing
        IReadOnlyList<ExtendedQueryTagStoreEntry> confirmedTags = await _extendedQueryTagStore.AssignReindexingOperationAsync(
            tagKeys,
            operation.Id,
            returnIfCompleted: true,
            cancellationToken: cancellationToken);

        return confirmedTags.Count > 0 ? operation : throw new ExtendedQueryTagsAlreadyExistsException();
    }
}
