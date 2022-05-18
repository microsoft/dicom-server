// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.Core.Features.Workitem.Model;
using Microsoft.Health.Dicom.Core.Messages.Workitem;

namespace Microsoft.Health.Dicom.Core.Features.Workitem;

/// <summary>
/// Provides functionality to orchestrate the DICOM workitem instance add, retrieve, cancel, and update.
/// </summary>
public class WorkitemOrchestrator : IWorkitemOrchestrator
{
    private readonly IDicomRequestContextAccessor _contextAccessor;
    private readonly IIndexWorkitemStore _indexWorkitemStore;
    private readonly IWorkitemStore _workitemStore;
    private readonly IWorkitemQueryTagService _workitemQueryTagService;
    private readonly ILogger<WorkitemOrchestrator> _logger;
    private readonly IQueryParser<BaseQueryExpression, BaseQueryParameters> _queryParser;

    public WorkitemOrchestrator(
        IDicomRequestContextAccessor contextAccessor,
        IWorkitemStore workitemStore,
        IIndexWorkitemStore indexWorkitemStore,
        IWorkitemQueryTagService workitemQueryTagService,
        IQueryParser<BaseQueryExpression, BaseQueryParameters> queryParser,
        ILogger<WorkitemOrchestrator> logger)
    {
        _contextAccessor = EnsureArg.IsNotNull(contextAccessor, nameof(contextAccessor));
        _indexWorkitemStore = EnsureArg.IsNotNull(indexWorkitemStore, nameof(indexWorkitemStore));
        _workitemStore = EnsureArg.IsNotNull(workitemStore, nameof(workitemStore));
        _queryParser = EnsureArg.IsNotNull(queryParser, nameof(queryParser));
        _workitemQueryTagService = EnsureArg.IsNotNull(workitemQueryTagService, nameof(workitemQueryTagService));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    /// <inheritdoc />
    public async Task<WorkitemMetadataStoreEntry> GetWorkitemMetadataAsync(string workitemUid, CancellationToken cancellationToken = default)
    {
        var partitionKey = _contextAccessor.RequestContext.GetPartitionKey();

        var workitemMetadata = await _indexWorkitemStore
            .GetWorkitemMetadataAsync(partitionKey, workitemUid, cancellationToken)
            .ConfigureAwait(false);

        return workitemMetadata;
    }

    /// <inheritdoc />
    public async Task AddWorkitemAsync(DicomDataset dataset, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(dataset, nameof(dataset));

        WorkitemInstanceIdentifier identifier = null;

        try
        {
            var partitionKey = _contextAccessor.RequestContext.GetPartitionKey();
            var queryTags = await _workitemQueryTagService.GetQueryTagsAsync(cancellationToken).ConfigureAwait(false);

            identifier = await _indexWorkitemStore
                .BeginAddWorkitemAsync(partitionKey, dataset, queryTags, cancellationToken)
                .ConfigureAwait(false);

            // We have successfully created the index, store the file.
            await StoreWorkitemBlobAsync(identifier, dataset, null, cancellationToken)
                .ConfigureAwait(false);

            await _indexWorkitemStore
                .EndAddWorkitemAsync(identifier.PartitionKey, identifier.WorkitemKey, cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            await TryAddWorkitemCleanupAsync(identifier, cancellationToken)
                .ConfigureAwait(false);

            throw;
        }
    }

    /// <inheritdoc />
    public async Task UpdateWorkitemStateAsync(DicomDataset dataset, WorkitemMetadataStoreEntry workitemMetadata, ProcedureStepState targetProcedureStepState, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(dataset, nameof(dataset));
        EnsureArg.IsNotNull(workitemMetadata, nameof(workitemMetadata));

        if (workitemMetadata.Status != WorkitemStoreStatus.ReadWrite)
        {
            throw new DataStoreException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    DicomCoreResource.WorkitemUpdateIsNotAllowed,
                    workitemMetadata.WorkitemUid,
                    workitemMetadata.ProcedureStepState.GetStringValue()));
        }

        (long CurrentWatermark, long NextWatermark)? watermarkEntry = null;

        try
        {
            // Get the current and next watermarks for the workitem instance
            watermarkEntry = await _indexWorkitemStore
                .GetCurrentAndNextWorkitemWatermarkAsync(workitemMetadata.WorkitemKey, cancellationToken)
                .ConfigureAwait(false);

            if (!watermarkEntry.HasValue)
            {
                throw new DataStoreException(DicomCoreResource.DataStoreOperationFailed);
            }

            // store the blob with the new watermark
            await StoreWorkitemBlobAsync(workitemMetadata, dataset, watermarkEntry.Value.NextWatermark, cancellationToken)
                .ConfigureAwait(false);

            // Update the workitem procedure step state in the store
            await _indexWorkitemStore
                .UpdateWorkitemProcedureStepStateAsync(
                    workitemMetadata,
                    watermarkEntry.Value.NextWatermark,
                    targetProcedureStepState.GetStringValue(),
                    cancellationToken)
                .ConfigureAwait(false);

            // Delete the blob with the old watermark
            await TryDeleteWorkitemBlobAsync(workitemMetadata, watermarkEntry.Value.CurrentWatermark, cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            // attempt to delete the blob with proposed watermark
            if (watermarkEntry.HasValue)
            {
                await TryDeleteWorkitemBlobAsync(workitemMetadata, watermarkEntry.Value.NextWatermark, cancellationToken)
                    .ConfigureAwait(false);
            }

            throw;
        }
    }

    /// <inheritdoc />
    public async Task UpdateWorkitemAsync(DicomDataset dataset, WorkitemMetadataStoreEntry workitemMetadata, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(dataset, nameof(dataset));
        EnsureArg.IsNotNull(workitemMetadata, nameof(workitemMetadata));

        if (workitemMetadata.Status != WorkitemStoreStatus.ReadWrite)
        {
            throw new DataStoreException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    DicomCoreResource.WorkitemUpdateIsNotAllowed,
                    workitemMetadata.WorkitemUid));
        }

        (long CurrentWatermark, long NextWatermark)? watermarkEntry = null;

        try
        {
            // Get the current and next watermarks for the workitem instance
            watermarkEntry = await _indexWorkitemStore
                .GetCurrentAndNextWorkitemWatermarkAsync(workitemMetadata.WorkitemKey, cancellationToken)
                .ConfigureAwait(false);

            if (!watermarkEntry.HasValue)
            {
                throw new DataStoreException(DicomCoreResource.DataStoreOperationFailed);
            }

            // Retrieve existing dataset.
            var existingDataset = await RetrieveWorkitemAsync(workitemMetadata, cancellationToken).ConfigureAwait(false);

            if (existingDataset == null)
            {
                throw new WorkitemNotFoundException(workitemMetadata.WorkitemUid);
            }

            var queryTags = await _workitemQueryTagService.GetQueryTagsAsync(cancellationToken).ConfigureAwait(false);

            // Update existingDatabase.
            MergeDatasets(existingDataset, dataset, queryTags);

            // store the blob with the new watermark
            await StoreWorkitemBlobAsync(workitemMetadata, existingDataset, watermarkEntry.Value.NextWatermark, cancellationToken)
                .ConfigureAwait(false);

            // TODO Ali: Update details in Sql Server.
            // Update the workitem watermark in the store

            // Delete the blob with the old watermark
            await TryDeleteWorkitemBlobAsync(workitemMetadata, watermarkEntry.Value.CurrentWatermark, cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            // attempt to delete the blob with proposed watermark
            if (watermarkEntry.HasValue)
            {
                await TryDeleteWorkitemBlobAsync(workitemMetadata, watermarkEntry.Value.NextWatermark, cancellationToken)
                    .ConfigureAwait(false);
            }

            throw;
        }
    }

    private static void MergeDatasets(DicomDataset existingDataset, DicomDataset newDataset, IReadOnlyCollection<QueryTag> queryTags)
    {
        // TODO Ali: See if sequence attributes are to be treated differently.
        foreach (QueryTag queryTag in queryTags)
        {
            // Update value only if the value already existed in the existing dataset.
            // Addition of attributes is not allowed in Update.
            if (existingDataset.Contains(queryTag.Tag))
            {
                ExtendedQueryTagDataType dataType = ExtendedQueryTagLimit.ExtendedQueryTagVRAndDataTypeMapping[queryTag.VR.Code];

                switch (dataType)
                {
                    case ExtendedQueryTagDataType.PersonNameData:
                    case ExtendedQueryTagDataType.StringData:
                        string newString = newDataset.GetString(queryTag.Tag);
                        existingDataset.AddOrUpdate<string>(queryTag.Tag, newString);
                        break;
                    case ExtendedQueryTagDataType.LongData:
                        long? newLong = newDataset.GetStringTimeAsLong(queryTag.Tag, queryTag.VR);
                        if (newLong.HasValue)
                        {
                            existingDataset.AddOrUpdate<long>(queryTag.Tag, newLong.Value);
                        }
                        break;
                    case ExtendedQueryTagDataType.DoubleData:
                        double newDouble = newDataset.GetFirstValueOrDefault<double>(queryTag.Tag, queryTag.VR);
                        existingDataset.AddOrUpdate<double>(queryTag.Tag, newDouble);
                        break;
                    case ExtendedQueryTagDataType.DateTimeData:
                        switch (queryTag.VR.Code)
                        {
                            case "DT":
                                Tuple<DateTime?, DateTime?> newDateTimeLocalAndUtc = newDataset.GetStringDateTimeAsLiteralAndUtcDateTimes(queryTag.Tag, queryTag.VR);
                                if (newDateTimeLocalAndUtc.Item1.HasValue)
                                {
                                    existingDataset.AddOrUpdate<DateTime>(queryTag.Tag, newDateTimeLocalAndUtc.Item1.Value);
                                }
                                break;
                            case "DA":
                                DateTime? newDate = newDataset.GetStringDateAsDate(queryTag.Tag, queryTag.VR);
                                if (newDate.HasValue)
                                {
                                    existingDataset.AddOrUpdate<DateTime>(queryTag.Tag, newDate.Value);
                                }
                                break;
                        }
                        break;
                }
            }
            else
            {
                // TODO Ali: Prepare a list of tags that were passed in the new dataset but were not present in the existing dataset.
            }
        }
    }

    /// <inheritdoc />
    public async Task<QueryWorkitemResourceResponse> QueryAsync(BaseQueryParameters parameters, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(parameters);

        var queryTags = await _workitemQueryTagService.GetQueryTagsAsync(cancellationToken: cancellationToken);

        BaseQueryExpression queryExpression = _queryParser.Parse(parameters, queryTags);

        var partitionKey = _contextAccessor.RequestContext.GetPartitionKey();

        WorkitemQueryResult queryResult = await _indexWorkitemStore
            .QueryAsync(partitionKey, queryExpression, cancellationToken);

        var workitemTasks = queryResult.WorkitemInstances
                .Select(x => TryGetWorkitemBlobAsync(x, cancellationToken));

        IEnumerable<DicomDataset> workitems = await Task.WhenAll(workitemTasks);

        return WorkitemQueryResponseBuilder.BuildWorkitemQueryResponse(workitems.ToList(), queryExpression);
    }

    /// <inheritdoc />
    public async Task<DicomDataset> GetWorkitemBlobAsync(WorkitemInstanceIdentifier identifier, CancellationToken cancellationToken = default)
    {
        if (null == identifier)
        {
            return null;
        }

        return await _workitemStore
            .GetWorkitemAsync(identifier, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<DicomDataset> RetrieveWorkitemAsync(WorkitemInstanceIdentifier workitemInstanceIdentifier, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(workitemInstanceIdentifier, nameof(workitemInstanceIdentifier));

        return await _workitemStore
            .GetWorkitemAsync(workitemInstanceIdentifier, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<DicomDataset> TryGetWorkitemBlobAsync(WorkitemInstanceIdentifier identifier, CancellationToken cancellationToken)
    {
        try
        {
            return await GetWorkitemBlobAsync(identifier, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (ItemNotFoundException ex)
        {
            _logger.LogWarning(ex, "Workitem [{Identifier}] blob doesn't exist due to simultaneous GET and UPDATE request or it could just be missing.", identifier);
            return null;
        }
    }

    /// <inheritdoc />
    private async Task TryAddWorkitemCleanupAsync(WorkitemInstanceIdentifier identifier, CancellationToken cancellationToken)
    {
        if (null == identifier)
        {
            return;
        }

        try
        {
            // Cleanup workitem data store
            await _indexWorkitemStore
                .DeleteWorkitemAsync(identifier, cancellationToken)
                .ConfigureAwait(false);

            // Cleanup Blob store
            await TryDeleteWorkitemBlobAsync(identifier, null, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, @"Failed to cleanup workitem [{Identifier}].", identifier);
        }
    }

    private async Task StoreWorkitemBlobAsync(
        WorkitemInstanceIdentifier identifier,
        DicomDataset dicomDataset,
        long? proposedWatermark = default,
        CancellationToken cancellationToken = default)
    {
        if (null == identifier || null == dicomDataset)
        {
            return;
        }

        await _workitemStore
            .AddWorkitemAsync(identifier, dicomDataset, proposedWatermark, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task TryDeleteWorkitemBlobAsync(WorkitemInstanceIdentifier identifier, long? proposedWatermark = default, CancellationToken cancellationToken = default)
    {
        if (null == identifier)
        {
            return;
        }

        try
        {
            await _workitemStore
                .DeleteWorkitemAsync(identifier, proposedWatermark, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, @"Failed to delete workitem blob for [{Identifier}].", identifier);
        }
    }
}
