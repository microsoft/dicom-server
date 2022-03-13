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
using FellowOakDicom;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Features.Common;

namespace Microsoft.Health.Dicom.Core.Features.Workitem;

public sealed class WorkitemQueryTagService : IWorkitemQueryTagService, IDisposable
{
    private readonly IIndexWorkitemStore _indexWorkitemStore;
    private readonly AsyncCache<IReadOnlyCollection<QueryTag>> _queryTagCache;
    private readonly IDicomTagParser _dicomTagParser;
    private readonly ILogger _logger;

    public WorkitemQueryTagService(IIndexWorkitemStore indexWorkitemStore, IDicomTagParser dicomTagParser, ILogger<WorkitemQueryTagService> logger)
    {
        _indexWorkitemStore = EnsureArg.IsNotNull(indexWorkitemStore, nameof(indexWorkitemStore));
        _queryTagCache = new AsyncCache<IReadOnlyCollection<QueryTag>>(ResolveQueryTagsAsync);
        _dicomTagParser = EnsureArg.IsNotNull(dicomTagParser, nameof(dicomTagParser));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    public void Dispose()
    {
        _queryTagCache.Dispose();
        GC.SuppressFinalize(this);
    }

    public async Task<IReadOnlyCollection<QueryTag>> GetQueryTagsAsync(CancellationToken cancellationToken = default)
    {
        return await _queryTagCache.GetAsync(cancellationToken: cancellationToken);
    }

    private async Task<IReadOnlyCollection<QueryTag>> ResolveQueryTagsAsync(CancellationToken cancellationToken)
    {
        var workitemQueryTags = await _indexWorkitemStore.GetWorkitemQueryTagsAsync(cancellationToken);

        foreach (var tag in workitemQueryTags)
        {
            if (_dicomTagParser.TryParse(tag.Path, out DicomTag[] dicomTags, true))
            {
                tag.PathTags = Array.AsReadOnly(dicomTags);
            }
            else
            {
                _logger.LogError("Failed to parse dicom path '{TagPath}' to dicom tags.", tag.Path);
                throw new DataStoreException(DicomCoreResource.DataStoreOperationFailed);
            }
        }

        return workitemQueryTags.Select(x => new QueryTag(x)).ToList();
    }
}
