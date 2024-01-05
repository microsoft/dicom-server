// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;
using EnsureThat;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Functions.ContentLengthBackFill.Models;

namespace Microsoft.Health.Dicom.Functions.ContentLengthBackFill;

public partial class ContentLengthBackFillDurableFunction
{
    internal static int CorruptedAndProcessed = -1;
    private const int ExpectedMinValue = 1;

    ///<summary>
    /// Asynchronously retrieves the next set of instance batches based on the configured options and whatever
    /// instances meet criteria of needing content length backfilled on file properties
    /// </summary>
    /// <param name="arguments">The options for configuring the batches.</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the asynchronous get operation. The value of its <see cref="Task{TResult}.Result"/>
    /// property contains a list of batches as defined by their smallest and largest watermark.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="arguments"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    [FunctionName(nameof(GetContentLengthBackFillInstanceBatches))]
    public Task<IReadOnlyList<WatermarkRange>> GetContentLengthBackFillInstanceBatches([ActivityTrigger] BatchCreationArguments arguments, ILogger logger)
    {
        EnsureArg.IsNotNull(arguments, nameof(arguments));
        EnsureArg.IsNotNull(logger, nameof(logger));

        logger.LogInformation("Dividing up the instances into batches starting from the end.");

        return _instanceStore.GetContentLengthBackFillInstanceBatches(
            arguments.BatchSize,
            arguments.MaxParallelBatches,
            CancellationToken.None);
    }

    /// <summary>
    /// Asynchronously retrieve content length from blob store and update content length on file Properties for the
    /// instances in the given watermark range.
    /// </summary>
    /// <param name="watermarkRange">The options that include the instances to clean up</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>A task representing the <see cref="BackFillContentLengthRangeDataAsync"/> operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    [FunctionName(nameof(BackFillContentLengthRangeDataAsync))]
    public async Task BackFillContentLengthRangeDataAsync([ActivityTrigger] WatermarkRange watermarkRange, ILogger logger)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));

        IReadOnlyList<VersionedInstanceIdentifier> instanceIdentifiers =
            await _instanceStore.GetContentLengthBackFillInstanceIdentifiersByWatermarkRangeAsync(watermarkRange);

        logger.LogInformation("Getting content length for the instances in the range {Range}.", watermarkRange);

        var propertiesByWatermark = new ConcurrentDictionary<long, FileProperties>();
        await Parallel.ForEachAsync(
            instanceIdentifiers,
            new ParallelOptions
            {
                CancellationToken = default,
                MaxDegreeOfParallelism = _options.MaxParallelThreads,
            },
            async (instanceIdentifier, token) =>
            {
                FileProperties blobStoreFileProperties;
                try
                {
                    blobStoreFileProperties = await _fileStore.GetFilePropertiesAsync(
                        instanceIdentifier.Version,
                        instanceIdentifier.Partition,
                        fileProperties: null, token);
                    if (blobStoreFileProperties.ContentLength < ExpectedMinValue)
                    {
                        blobStoreFileProperties = new FileProperties { ContentLength = CorruptedAndProcessed };
                        logger.LogWarning(
                            "Content length for the instance with watermark {Watermark} in partition {Partition} appears to be corrupted. Value should be {ExpectedMin} or greater, but it was {Length}. Will store as {Value} to mark as processed.",
                            instanceIdentifier.Version,
                            instanceIdentifier.Partition.Key,
                            ExpectedMinValue,
                            blobStoreFileProperties.ContentLength,
                            CorruptedAndProcessed);
                    }
                    propertiesByWatermark.TryAdd(instanceIdentifier.Version, blobStoreFileProperties);
                }
                catch (Exception e) when (e is DataStoreException or DataStoreRequestFailedException)
                {
                    if (e is DataStoreRequestFailedException &&
                        e.Message.Contains(BlobErrorCode.ConditionNotMet.ToString(), StringComparison.InvariantCulture))
                    {
                        propertiesByWatermark.TryAdd(instanceIdentifier.Version, new FileProperties { ContentLength = CorruptedAndProcessed });
                        logger.LogWarning(
                            "Could not get content length from blob store for the instance with watermark {Watermark} in partition {Partition} due to data corruption. The file may be missing or etags mismatch. Will store as {Value} to mark as processed.",
                            instanceIdentifier.Version,
                            instanceIdentifier.Partition.Key,
                            CorruptedAndProcessed);
                    }
                    else
                    {
                        // try to reprocess later by leaving content length 0, but allow other instances to attempt to update
                        logger.LogInformation(
                            "Could not get content length from blob store for the instance with watermark {Watermark} in partition {Partition}. Will leave length as 0 to allow for reprocessing as data does not appear to be corrupted.",
                            instanceIdentifier.Version,
                            instanceIdentifier.Partition.Key);
                    }
                }
            });

        logger.LogInformation("Completed getting content length for the instances in the range {Range}.", watermarkRange);

        await _indexDataStore.UpdateFilePropertiesContentLengthAsync(propertiesByWatermark);

        logger.LogInformation(
            "Complete updating content length for the instances in the range {Range}, with total instances updated count of {TotalInstanceUpdated}.",
            watermarkRange,
            propertiesByWatermark.Count);
    }
}
