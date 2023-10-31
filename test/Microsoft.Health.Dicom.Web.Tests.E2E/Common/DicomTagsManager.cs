// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.Dicom.Web.Tests.E2E.Extensions;
using Microsoft.Health.Operations;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Common;

internal class DicomTagsManager : IAsyncDisposable
{
    private readonly IDicomWebClient _dicomWebClient;
    private readonly HashSet<string> _tags;

    public DicomTagsManager(IDicomWebClient dicomWebClient)
    {
        _dicomWebClient = EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));
        _tags = new HashSet<string>();
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Do not throw on test clean up.")]
    public async ValueTask DisposeAsync()
    {
        foreach (string tag in _tags)
        {
            try
            {
                await DeleteExtendedQueryTagAsync(tag);
            }
            catch (Exception)
            {
                // Dispose should not throw for test clean up
            }
        }
    }

    public Task<OperationStatus> AddTagsAsync(params AddExtendedQueryTagEntry[] entries)
        => AddTagsAsync(entries, CancellationToken.None);

    public async Task<OperationStatus> AddTagsAsync(IEnumerable<AddExtendedQueryTagEntry> entries, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(entries, nameof(entries));
        foreach (AddExtendedQueryTagEntry entry in entries)
        {
            _tags.Add(entry.Path);
        }

        DicomWebResponse<DicomOperationReference> response = await _dicomWebClient.AddExtendedQueryTagAsync(entries, cancellationToken);
        DicomOperationReference operation = await response.GetValueAsync();

        IOperationState<DicomOperation> result = await _dicomWebClient.WaitForCompletionAsync(operation.Id);

        // Check reference
        DicomWebResponse<IOperationState<DicomOperation>> actualResponse = await _dicomWebClient.ResolveReferenceAsync(operation, cancellationToken);
        IOperationState<DicomOperation> actual = await actualResponse.GetValueAsync();
        Assert.Equal(result.OperationId, actual.OperationId);
        Assert.Equal(result.Status, actual.Status);

        return result.Status;
    }

    public async Task<GetExtendedQueryTagEntry> GetTagAsync(string tagPath, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(tagPath, nameof(tagPath));

        var response = await _dicomWebClient.GetExtendedQueryTagAsync(tagPath, cancellationToken);
        return await response.GetValueAsync();
    }

    public async Task<IReadOnlyList<GetExtendedQueryTagEntry>> GetTagsAsync(int limit, long offset, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsGte(limit, 1, nameof(limit));
        EnsureArg.IsGte(offset, 0, nameof(offset));

        var response = await _dicomWebClient.GetExtendedQueryTagsAsync(limit, offset, cancellationToken);
        return await response.GetValueAsync();
    }

    public async Task<IReadOnlyList<ExtendedQueryTagError>> GetTagErrorsAsync(string tagPath, int limit, long offset, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(tagPath, nameof(tagPath));
        EnsureArg.IsGte(limit, 1, nameof(limit));
        EnsureArg.IsGte(offset, 0, nameof(offset));

        var response = await _dicomWebClient.GetExtendedQueryTagErrorsAsync(tagPath, limit, offset, cancellationToken);
        return await response.GetValueAsync();
    }

    public async IAsyncEnumerable<ExtendedQueryTagError> GetTagErrorsAsync(string tagPath, int pageSize = 100, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(tagPath, nameof(tagPath));
        EnsureArg.IsGte(pageSize, 1, nameof(pageSize));

        int offset = 0;
        IReadOnlyList<ExtendedQueryTagError> page;
        do
        {
            page = await GetTagErrorsAsync(tagPath, pageSize, offset, cancellationToken);
            offset += page.Count;

            foreach (ExtendedQueryTagError error in page)
            {
                yield return error;
            }
        } while (page.Count > 0);
    }

    public async Task<GetExtendedQueryTagEntry> UpdateExtendedQueryTagAsync(string tagPath, UpdateExtendedQueryTagEntry newValue, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(tagPath, nameof(tagPath));

        var response = await _dicomWebClient.UpdateExtendedQueryTagAsync(tagPath, newValue, cancellationToken);
        return await response.GetValueAsync();
    }

    public async Task<bool> DeleteExtendedQueryTagAsync(string tagPath, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(tagPath, nameof(tagPath));
        _tags.Remove(tagPath);

        try
        {
            var response = await _dicomWebClient.DeleteExtendedQueryTagAsync(tagPath, cancellationToken);
            return response.StatusCode == System.Net.HttpStatusCode.NoContent;
        }
        catch (DicomWebException dwe) when (dwe.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}
