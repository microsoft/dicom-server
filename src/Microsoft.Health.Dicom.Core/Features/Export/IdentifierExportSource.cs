// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Core.Features.Export;

// TODO: add unit test
internal class IdentifierExportSource : IExportSource
{
    public event EventHandler<ReadFailureEventArgs> ReadFailure;

    public SourceManifest Remaining => _index < _identifiers.Count
        ? new SourceManifest { Type = ExportSourceType.Identifiers, Input = GetRemaining() }
        : null;

    private int _index;
    private readonly IReadOnlyList<PartitionedDicomIdentifier> _identifiers;
    private readonly IInstanceStore _instanceStore;

    public IdentifierExportSource(IReadOnlyList<PartitionedDicomIdentifier> identifiers, IInstanceStore instanceStore)
    {
        _identifiers = EnsureArg.IsNotNull(identifiers, nameof(identifiers));
        _instanceStore = EnsureArg.IsNotNull(instanceStore, nameof(instanceStore));
    }

    public async IAsyncEnumerator<SourceElement> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        for (int i = _index; i < _identifiers.Count; i++)
        {
            IEnumerable<SourceElement> results = null;
            PartitionedDicomIdentifier identifier = _identifiers[i];

            try
            {
                IReadOnlyList<VersionedInstanceIdentifier> instances = identifier.Type switch
                {
                    ResourceType.Study => await _instanceStore.GetInstanceIdentifiersInStudyAsync(identifier.PartitionKey, identifier.StudyInstanceUid, cancellationToken),
                    ResourceType.Series => await _instanceStore.GetInstanceIdentifiersInSeriesAsync(identifier.PartitionKey, identifier.StudyInstanceUid, identifier.SeriesInstanceUid, cancellationToken),
                    _ => await _instanceStore.GetInstanceIdentifierAsync(identifier.PartitionKey, identifier.StudyInstanceUid, identifier.SeriesInstanceUid, identifier.SopInstanceUid, cancellationToken),
                };

                if (instances.Count == 0)
                    throw new FileNotFoundException("Cannot find any matching instances");

                results = instances.Select(x => SourceElement.ForIdentifier(x));
            }
            catch (Exception ex)
            {
                var args = new ReadFailureEventArgs(identifier, ex);
                ReadFailure?.Invoke(this, args);
                results = new SourceElement[] { SourceElement.ForFailure(args) };
            }

            foreach (SourceElement result in results)
                yield return result;
        }
    }

    public SourceManifest TakeNextBatch(int size)
    {
        int count = Math.Min(size, _identifiers.Count - _index);
        if (count == 0)
            return null;

        var batch = new List<PartitionedDicomIdentifier>();
        for (int i = 0; i < count; i++)
        {
            batch.Add(_identifiers[_index + i]);
        }

        _index += count;
        return new SourceManifest { Type = ExportSourceType.Identifiers, Input = batch };
    }

    private IReadOnlyList<PartitionedDicomIdentifier> GetRemaining()
        => _identifiers.Skip(_index).ToList();

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;
}
