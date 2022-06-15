// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Client;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Common;

internal class DicomInstancesManager : IAsyncDisposable
{

    private readonly IDicomWebClient _dicomWebClient;
    private readonly ConcurrentBag<DicomInstanceId> _instanceIds;

    public DicomInstancesManager(IDicomWebClient dicomWebClient)
    {
        _dicomWebClient = EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));
        _instanceIds = new ConcurrentBag<DicomInstanceId>();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var id in _instanceIds)
        {
            try
            {
                await _dicomWebClient.DeleteInstanceAsync(id.StudyInstanceUid, id.SeriesInstanceUid, id.SopInstanceUid, id.PartitionName);
            }
            catch (DicomWebException)
            {

            }
        }
    }

    public async Task<DicomWebResponse<DicomDataset>> StoreAsync(DicomFile dicomFile, string studyInstanceUid = default, string partitionName = default, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(dicomFile, nameof(dicomFile));
        _instanceIds.Add(DicomInstanceId.FromDicomFile(dicomFile, partitionName));
        return await _dicomWebClient.StoreAsync(dicomFile, studyInstanceUid, partitionName, cancellationToken);
    }

    public async Task<DicomWebResponse<DicomDataset>> StoreAsync(HttpContent content, string partitionName = default, CancellationToken cancellationToken = default, DicomInstanceId instanceId = default)
    {
        EnsureArg.IsNotNull(content, nameof(content));
        // Null instanceId indiates Store will fail
        if (instanceId != null)
        {
            _instanceIds.Add(instanceId);
        }
        return await _dicomWebClient.StoreAsync(content, partitionName, cancellationToken);
    }

    public async Task<DicomWebResponse<DicomDataset>> StoreAsync(IEnumerable<DicomFile> dicomFiles, string studyInstanceUid = default, string partitionName = default, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(dicomFiles, nameof(dicomFiles));
        foreach (var file in dicomFiles)
        {
            _instanceIds.Add(DicomInstanceId.FromDicomFile(file, partitionName));
        }

        return await _dicomWebClient.StoreAsync(dicomFiles, studyInstanceUid, partitionName, cancellationToken);
    }

    public async Task<DicomWebResponse<DicomDataset>> StoreAsync(Stream stream, string studyInstanceUid = default, string partitionName = default, CancellationToken cancellationToken = default, DicomInstanceId instanceId = default)
    {
        EnsureArg.IsNotNull(stream, nameof(stream));

        // Null instanceId indiates Store will fail
        if (instanceId != null)
        {
            _instanceIds.Add(instanceId);
        }
        return await _dicomWebClient.StoreAsync(stream, studyInstanceUid, partitionName, cancellationToken);
    }

    public async Task<DicomWebResponse> AddWorkitemAsync(IEnumerable<DicomDataset> dicomDatasets,
        string workitemInstanceUid,
        string partitionName = default,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(dicomDatasets, nameof(dicomDatasets));

        return await _dicomWebClient
            .AddWorkitemAsync(dicomDatasets, workitemInstanceUid, partitionName, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<DicomWebResponse> QueryWorkitemAsync(string queryString, string partitionName = default, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotEmptyOrWhiteSpace(queryString, nameof(queryString));

        return await _dicomWebClient
            .QueryWorkitemAsync(queryString, partitionName, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<DicomWebResponse> CancelWorkitemAsync(
        IEnumerable<DicomDataset> datasets,
        string workitemUid,
        string partitionName = default,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(datasets, nameof(datasets));
        EnsureArg.IsNotEmptyOrWhiteSpace(workitemUid, nameof(workitemUid));

        return await _dicomWebClient
            .CancelWorkitemAsync(datasets, workitemUid, partitionName, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<DicomWebResponse> RetrieveWorkitemAsync(
        string workitemUid,
        string partitionName = default,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotEmptyOrWhiteSpace(workitemUid, nameof(workitemUid));

        return await _dicomWebClient
            .RetrieveWorkitemAsync(workitemUid, partitionName, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<DicomWebResponse> ChangeWorkitemStateAsync(
        DicomDataset requestDataset,
        string workitemUid,
        string partitionName = default,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(requestDataset, nameof(requestDataset));
        EnsureArg.IsNotEmptyOrWhiteSpace(workitemUid, nameof(workitemUid));

        return await _dicomWebClient
            .ChangeWorkitemStateAsync(Enumerable.Repeat(requestDataset, 1), workitemUid, partitionName, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<DicomWebResponse> UpdateWorkitemAsync(
        DicomDataset requestDataset,
        string workitemUid,
        string transactionUid = default,
        string partitionName = default,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(requestDataset, nameof(requestDataset));
        EnsureArg.IsNotEmptyOrWhiteSpace(workitemUid, nameof(workitemUid));

        return await _dicomWebClient
            .UpdateWorkitemAsync(Enumerable.Repeat(requestDataset, 1), workitemUid, transactionUid, partitionName, cancellationToken)
            .ConfigureAwait(false);
    }
}
