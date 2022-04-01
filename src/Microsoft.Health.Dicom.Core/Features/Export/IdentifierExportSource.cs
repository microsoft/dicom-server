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
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Core.Features.Export;

internal class IdentifierExportSource : IExportSource
{
    public SourceManifest Remaining => _index < _identifiers.Count
        ? new SourceManifest { Type = ExportSourceType.Identifiers, Input = GetRemaining() }
        : null;

    private int _index;
    private readonly IReadOnlyList<DicomIdentifier> _identifiers;
    private readonly IInstanceStore _instanceStore;

    public IdentifierExportSource(IReadOnlyList<DicomIdentifier> identifiers, IInstanceStore instanceStore)
    {
        _identifiers = EnsureArg.IsNotNull(identifiers, nameof(identifiers));
        _instanceStore = EnsureArg.IsNotNull(instanceStore, nameof(instanceStore));
    }

    public async IAsyncEnumerator<VersionedInstanceIdentifier> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        // TODO: Partition
        for (int i = _index; i < _identifiers.Count; i++)
        {
            DicomIdentifier identifier = _identifiers[i];
            IEnumerable<VersionedInstanceIdentifier> results = identifier.Type switch
            {
                ResourceType.Study => await _instanceStore.GetInstanceIdentifiersInStudyAsync(DefaultPartition.Key, identifier.StudyInstanceUid, cancellationToken),
                ResourceType.Series => await _instanceStore.GetInstanceIdentifiersInSeriesAsync(DefaultPartition.Key, identifier.StudyInstanceUid, identifier.SeriesInstanceUid, cancellationToken),
                _ => await _instanceStore.GetInstanceIdentifierAsync(DefaultPartition.Key, identifier.StudyInstanceUid, identifier.SeriesInstanceUid, identifier.SopInstanceUid, cancellationToken),
            };

            foreach (VersionedInstanceIdentifier result in results)
                yield return result;
        }
    }

    public SourceManifest TakeNextBatch(int size)
    {
        int count = Math.Min(size, _identifiers.Count - _index);
        if (count == 0)
            return null;

        var batch = new List<DicomIdentifier>();
        for (int i = 0; i < count; i++)
        {
            batch.Add(_identifiers[i]);
        }

        _index += count;
        return new SourceManifest { Type = ExportSourceType.Identifiers, Input = batch };
    }

    private IReadOnlyList<DicomIdentifier> GetRemaining()
        => _identifiers.Skip(_index).ToList();

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;
}
