// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Extensions;
using Microsoft.Health.Operations;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Common;

internal class DicomInstancesManager : IAsyncDisposable
{
    private readonly IDicomWebClient _dicomWebClient;
    private readonly ConcurrentDictionary<DicomInstanceId, object> _instanceIds;
    private readonly bool _isDataPartitionEnabled;

    public DicomInstancesManager(IDicomWebClient dicomWebClient)
    {
        _dicomWebClient = EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));
        _instanceIds = new ConcurrentDictionary<DicomInstanceId, object>();
        _isDataPartitionEnabled = bool.Parse(TestEnvironment.Variables["EnableDataPartitions"] ?? "false");
    }

    public async ValueTask DisposeAsync()
    {
        foreach (DicomInstanceId id in _instanceIds.Keys)
        {
            try
            {
                await _dicomWebClient.DeleteInstanceAsync(id.StudyInstanceUid, id.SeriesInstanceUid, id.SopInstanceUid, GetPartition(id.Partition));
            }
            catch (DicomWebException)
            { }
        }
    }

    public Task<DicomWebResponse<DicomDataset>> StoreAsync(
        HttpContent content,
        DicomInstanceId instanceId,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(content, nameof(content));
        EnsureArg.IsNotNull(instanceId, nameof(instanceId));

        _instanceIds.TryAdd(instanceId, null);
        return _dicomWebClient.StoreAsync(content, cancellationToken: cancellationToken);
    }

    public Task<DicomWebResponse<DicomDataset>> StoreAsync(
        IReadOnlyCollection<DicomFile> dicomFiles,
        Partition partition = null,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.HasItems(dicomFiles, nameof(dicomFiles));

        foreach (DicomFile file in dicomFiles)
        {
            DicomInstanceId instanceId = DicomInstanceId.FromDicomFile(file, partition);
            _instanceIds.TryAdd(instanceId, null);
        }

        return _dicomWebClient.StoreAsync(dicomFiles, partitionName: partition?.Name, cancellationToken: cancellationToken);
    }

    public Task<DicomWebResponse<DicomDataset>> StoreAsync(
        DicomFile dicomFile,
        Partition partition = null,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(dicomFile, nameof(dicomFile));

        DicomInstanceId instanceId = DicomInstanceId.FromDicomFile(dicomFile, partition);
        _instanceIds.TryAdd(instanceId, null);

        return _dicomWebClient.StoreAsync(dicomFile, partitionName: partition?.Name, cancellationToken: cancellationToken);
    }

    public Task<DicomWebResponse<DicomDataset>> StoreStudyAsync(
        IReadOnlyCollection<DicomFile> dicomFiles,
        Partition partition = null,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.HasItems(dicomFiles, nameof(dicomFiles));

        string studyInstanceUid = null;
        foreach (DicomFile file in dicomFiles)
        {
            DicomInstanceId instanceId = DicomInstanceId.FromDicomFile(file, partition);
            _instanceIds.TryAdd(instanceId, null);

            studyInstanceUid ??= instanceId.StudyInstanceUid;
            if (studyInstanceUid != instanceId.StudyInstanceUid)
                throw new InvalidOperationException("SOP instances must be associated with the same study");
        }

        return _dicomWebClient.StoreAsync(dicomFiles, studyInstanceUid, partition?.Name, cancellationToken);
    }

    public async Task StoreIfNotExistsAsync(
        DicomFile dicomFile,
        Partition partition = null,
        bool doNotDelete = false,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(dicomFile, nameof(dicomFile));

        DicomInstanceId instanceId = DicomInstanceId.FromDicomFile(dicomFile, partition);

        if (!doNotDelete)
            _instanceIds.TryAdd(instanceId, null);

        try
        {
            await _dicomWebClient.StoreAsync(dicomFile, instanceId.StudyInstanceUid, partition?.Name, cancellationToken);
        }
        catch (DicomWebException e) when (e.StatusCode == HttpStatusCode.Conflict)
        { }
    }

    public async Task<IOperationState<DicomOperation>> UpdateStudyAsync(
        List<string> studyInstanceUids,
        DicomDataset dicomDataset,
        Partition partition = null,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(studyInstanceUids, nameof(studyInstanceUids));
        EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

        DicomWebResponse<DicomOperationReference> response = await _dicomWebClient.UpdateStudyAsync(studyInstanceUids, dicomDataset, partition?.Name, cancellationToken);
        DicomOperationReference operation = await response.GetValueAsync();

        IOperationState<DicomOperation> result = await _dicomWebClient.WaitForCompletionAsync(operation.Id);

        // Check reference
        DicomWebResponse<IOperationState<DicomOperation>> actualResponse = await _dicomWebClient.ResolveReferenceAsync(operation, cancellationToken);
        IOperationState<DicomOperation> actual = await actualResponse.GetValueAsync();
        Assert.Equal(result.OperationId, actual.OperationId);
        Assert.Equal(result.Status, actual.Status);

        return result;
    }

    public async Task<DicomWebResponse> AddWorkitemAsync(
        IEnumerable<DicomDataset> dicomDatasets,
        string workitemInstanceUid,
        Partition partition = null,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(dicomDatasets, nameof(dicomDatasets));

        return await _dicomWebClient
            .AddWorkitemAsync(dicomDatasets, workitemInstanceUid, partition?.Name, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<DicomWebResponse> QueryWorkitemAsync(
        string queryString,
        Partition partition = null,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotEmptyOrWhiteSpace(queryString, nameof(queryString));

        return await _dicomWebClient
            .QueryWorkitemAsync(queryString, partition?.Name, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<DicomWebResponse> CancelWorkitemAsync(
        IEnumerable<DicomDataset> datasets,
        string workitemUid,
        Partition partition = null,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(datasets, nameof(datasets));
        EnsureArg.IsNotEmptyOrWhiteSpace(workitemUid, nameof(workitemUid));

        return await _dicomWebClient
            .CancelWorkitemAsync(datasets, workitemUid, partition?.Name, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<DicomWebResponse> RetrieveWorkitemAsync(
        string workitemUid,
        Partition partition = null,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotEmptyOrWhiteSpace(workitemUid, nameof(workitemUid));

        return await _dicomWebClient
            .RetrieveWorkitemAsync(workitemUid, partition?.Name, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<DicomWebResponse> ChangeWorkitemStateAsync(
        DicomDataset requestDataset,
        string workitemUid,
        Partition partition = null,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(requestDataset, nameof(requestDataset));
        EnsureArg.IsNotEmptyOrWhiteSpace(workitemUid, nameof(workitemUid));

        return await _dicomWebClient
            .ChangeWorkitemStateAsync(Enumerable.Repeat(requestDataset, 1), workitemUid, partition?.Name, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<DicomWebResponse> UpdateWorkitemAsync(
        DicomDataset requestDataset,
        string workitemUid,
        string transactionUid = default,
        Partition partition = null,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(requestDataset, nameof(requestDataset));
        EnsureArg.IsNotEmptyOrWhiteSpace(workitemUid, nameof(workitemUid));

        return await _dicomWebClient
            .UpdateWorkitemAsync(Enumerable.Repeat(requestDataset, 1), workitemUid, transactionUid, partition?.Name, cancellationToken)
            .ConfigureAwait(false);
    }

    private string GetPartition(Partition partition)
        => _isDataPartitionEnabled ? partition?.Name : null;
}
