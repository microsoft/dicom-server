// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Core.Models.Common;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Core.Features.Export;

internal sealed class IdentifierExportSource : IExportSource
{
    public event EventHandler<ReadFailureEventArgs> ReadFailure;

    public TypedConfiguration<ExportSourceType> Configuration => throw new NotImplementedException();

    private readonly IInstanceStore _instanceStore;
    private readonly PartitionEntry _partition;
    private readonly IdentifierExportOptions _options;

    private int _startIndex;

    public IdentifierExportSource(IInstanceStore instanceStore, PartitionEntry partition, IdentifierExportOptions options)
    {
        _instanceStore = EnsureArg.IsNotNull(instanceStore, nameof(instanceStore));
        _partition = EnsureArg.IsNotNull(partition, nameof(partition));
        _options = EnsureArg.IsNotNull(options, nameof(options));
    }

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    public async IAsyncEnumerator<ReadResult> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        IEnumerable<ReadResult> results = Enumerable.Empty<ReadResult>();
        for (int i = _startIndex; i < _options.Values.Count; i++)
        {
            // Identifier has already been validated
            var identifier = DicomIdentifier.Parse(_options.Values[i]);

            try
            {
                // Attempt to read the data
                IReadOnlyList<VersionedInstanceIdentifier> instances = identifier.Type switch
                {
                    ResourceType.Study => await _instanceStore.GetInstanceIdentifiersInStudyAsync(_partition.PartitionKey, identifier.StudyInstanceUid, cancellationToken),
                    ResourceType.Series => await _instanceStore.GetInstanceIdentifiersInSeriesAsync(_partition.PartitionKey, identifier.StudyInstanceUid, identifier.SeriesInstanceUid, cancellationToken),
                    _ => await _instanceStore.GetInstanceIdentifierAsync(_partition.PartitionKey, identifier.StudyInstanceUid, identifier.SeriesInstanceUid, identifier.SopInstanceUid, cancellationToken),
                };

                if (instances.Count == 0)
                    throw new FileNotFoundException("Cannot find any matching instances");

                results = instances.Select(ReadResult.ForIdentifier);
            }
            catch (Exception ex)
            {
                var args = new ReadFailureEventArgs(identifier, ex);
                ReadFailure?.Invoke(this, args);
                results = new ReadResult[] { ReadResult.ForFailure(args) };
            }
        }

        // Actual yield the results (either resolved identifiers or errors)
        foreach (ReadResult result in results)
            yield return result;
    }

    public bool TryDequeueBatch(int size, out TypedConfiguration<ExportSourceType> batch)
    {
        EnsureArg.IsGt(size, 0, nameof(size));

        int count = Math.Min(size, _options.Values.Count - _startIndex);
        if (count > 0)
        {
            batch = new TypedConfiguration<ExportSourceType>
            {
                Configuration = new ConfigurationBuilder().AddInMemoryCollection().Build(),
                Type = ExportSourceType.Identifiers,
            };

            // Add the subset of data
            for (int i = _startIndex; i < _startIndex + count; i++)
                batch.Configuration[nameof(IdentifierExportOptions.Values) + ':' + i.ToString(CultureInfo.InvariantCulture)] = _options.Values[i];

            _startIndex += count;
            return true;
        }

        batch = default;
        return false;
    }
}
