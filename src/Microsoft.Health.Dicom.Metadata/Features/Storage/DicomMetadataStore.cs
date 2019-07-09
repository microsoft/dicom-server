// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Features.Persistence.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Metadata.Config;
using Microsoft.Health.Dicom.Metadata.Features.Storage.Models;
using Newtonsoft.Json;
using Polly;

namespace Microsoft.Health.Dicom.Metadata.Features.Storage
{
    public class DicomMetadataStore : IDicomMetadataStore
    {
        private readonly CloudBlobContainer _container;
        private readonly DicomMetadataConfiguration _metadataConfiguration;
        private readonly ILogger<DicomMetadataStore> _logger;
        private readonly Encoding _metadataEncoding;

        public DicomMetadataStore(
            CloudBlobClient client,
            IOptionsMonitor<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor,
            DicomMetadataConfiguration metadataConfiguration,
            ILogger<DicomMetadataStore> logger)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(namedBlobContainerConfigurationAccessor, nameof(namedBlobContainerConfigurationAccessor));
            EnsureArg.IsNotNull(metadataConfiguration, nameof(metadataConfiguration));
            EnsureArg.IsNotNull(logger, nameof(logger));

            BlobContainerConfiguration containerConfiguration = namedBlobContainerConfigurationAccessor.Get(Constants.ContainerConfigurationName);

            _container = client.GetContainerReference(containerConfiguration.ContainerName);
            _metadataConfiguration = metadataConfiguration;
            _logger = logger;
            _metadataEncoding = Encoding.Unicode;
        }

        /// <inheritdoc />
        public async Task AddStudySeriesDicomMetadataAsync(DicomDataset instance, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(instance, nameof(instance));

            var identity = DicomInstance.Create(instance);
            var metadata = new DicomStudyMetadata(identity.StudyInstanceUID);

            CloudBlockBlob cloudBlockBlob = GetStudyMetadataBlockBlobAndValidateId(identity.StudyInstanceUID);

            // Retry when the pre-condition fails on replace (ETag check).
            IAsyncPolicy retryPolicy = CreatePreConditionFailedRetryPolicy();
            await cloudBlockBlob.CatchStorageExceptionAndThrowDataStoreException(
                async (blockBlob) =>
                {
                    _logger.LogDebug($"Storing Instance: {identity.StudyInstanceUID}.{identity.SopInstanceUID}.{identity.SopInstanceUID}");

                    if (await blockBlob.ExistsAsync())
                    {
                        metadata = await ReadMetadataAsync(blockBlob, cancellationToken);
                    }

                    metadata.AddDicomInstance(instance, _metadataConfiguration.StudySeriesMetadataAttributes);
                    await UpdateMetadataAsync(blockBlob, metadata, cancellationToken);
                },
                retryPolicy);
        }

        /// <inheritdoc />
        public async Task<DicomDataset> GetStudyDicomMetadataAsync(
            string studyInstanceUID,
            HashSet<DicomAttributeId> optionalAttributes = null,
            CancellationToken cancellationToken = default)
        {
            var result = new DicomDataset();
            CloudBlockBlob cloudBlockBlob = GetStudyMetadataBlockBlobAndValidateId(studyInstanceUID);

            return await cloudBlockBlob.CatchStorageExceptionAndThrowDataStoreException(
                async (blockBlob) =>
                {
                    _logger.LogDebug($"Reading Study Metadata for Study: {studyInstanceUID}");

                    DicomStudyMetadata studyMetadata = await ReadMetadataAsync(blockBlob, cancellationToken);

                    // Add the Study specific Query Transaction attributes
                    result.Add(DicomTag.NumberOfStudyRelatedSeries, studyMetadata.SeriesMetadata.Count);
                    result.Add(DicomTag.NumberOfStudyRelatedInstances, studyMetadata.SeriesMetadata.Sum(x => x.Value.Instances.Count));
                    result.Add(DicomTag.StudyInstanceUID, studyInstanceUID);

                    // Now append the fetched metadata attributes.
                    HashSet<DicomAttributeId> attributes = _metadataConfiguration.StudyRequiredMetadataAttributes;
                    if (optionalAttributes != null)
                    {
                        optionalAttributes.Each(x => attributes.Add(x));
                    }

                    foreach (DicomAttributeId seriesAttributeId in attributes)
                    {
                        foreach (DicomSeriesMetadata seriesMetadata in studyMetadata.SeriesMetadata.Values)
                        {
                            // Just add the first DICOM item; we might want to do something more clever in the future.
                            AttributeValue attributeValue = seriesMetadata.AttributeValues.FirstOrDefault(x => x.DicomItem.Tag == seriesAttributeId.InstanceDicomTag);
                            if (attributeValue != null)
                            {
                                result.Add(attributeValue.DicomItem);
                                break;
                            }
                        }
                    }

                    return result;
                });
        }

        public async Task<DicomDataset> GetSeriesDicomMetadataWithAllOptionalAsync(
            string studyInstanceUID,
            string seriesInstanceUID,
            CancellationToken cancellationToken = default)
        {
            return await GetSeriesDicomMetadataAsync(
                studyInstanceUID, seriesInstanceUID, _metadataConfiguration.SeriesOptionalMetadataAttributes, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<DicomDataset> GetSeriesDicomMetadataAsync(
            string studyInstanceUID,
            string seriesInstanceUID,
            HashSet<DicomAttributeId> optionalAttributes = null,
            CancellationToken cancellationToken = default)
        {
            var result = new DicomDataset();
            CloudBlockBlob cloudBlockBlob = GetStudyMetadataBlockBlobAndValidateId(studyInstanceUID);

            return await cloudBlockBlob.CatchStorageExceptionAndThrowDataStoreException(
                async (blockBlob) =>
                {
                    _logger.LogDebug($"Reading Series Metadata for Study: {studyInstanceUID} and Series: {seriesInstanceUID}");
                    DicomStudyMetadata metadata = await ReadMetadataAsync(blockBlob, cancellationToken);

                    if (!metadata.SeriesMetadata.ContainsKey(seriesInstanceUID))
                    {
                        throw new DataStoreException(HttpStatusCode.NotFound);
                    }

                    DicomSeriesMetadata seriesMetadata = metadata.SeriesMetadata[seriesInstanceUID];

                    // Add the Series specific Query Transaction attributes
                    result.Add(DicomTag.NumberOfSeriesRelatedInstances, seriesMetadata.Instances.Count);
                    result.Add(DicomTag.SeriesInstanceUID, seriesInstanceUID);

                    // Now append the fetched metadata attributes.
                    HashSet<DicomAttributeId> attributes = _metadataConfiguration.SeriesRequiredMetadataAttributes;
                    if (optionalAttributes != null)
                    {
                        optionalAttributes.Each(x => attributes.Add(x));
                    }

                    foreach (DicomAttributeId seriesAttributeId in attributes)
                    {
                        // Just add the first DICOM item; we might want to do something more clever in the future.
                        AttributeValue attributeValue = seriesMetadata.AttributeValues.FirstOrDefault(x => x.DicomItem.Tag == seriesAttributeId.InstanceDicomTag);
                        if (attributeValue != null)
                        {
                            result.Add(attributeValue.DicomItem);
                            break;
                        }
                    }

                    return result;
                });
        }

        /// <inheritdoc />
        public async Task<IEnumerable<DicomInstance>> GetInstancesInStudyAsync(string studyInstanceUID, CancellationToken cancellationToken = default)
        {
            CloudBlockBlob cloudBlockBlob = GetStudyMetadataBlockBlobAndValidateId(studyInstanceUID);

            return await cloudBlockBlob.CatchStorageExceptionAndThrowDataStoreException(
                async (blockBlob) =>
                {
                    _logger.LogDebug($"Getting Instances in Study: {studyInstanceUID}");
                    DicomStudyMetadata metadata = await ReadMetadataAsync(blockBlob, cancellationToken);
                    return metadata.GetDicomInstances();
                });
        }

        /// <inheritdoc />
        public async Task<IEnumerable<DicomInstance>> GetInstancesInSeriesAsync(string studyInstanceUID, string seriesInstanceUID, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUID, nameof(seriesInstanceUID));
            CloudBlockBlob cloudBlockBlob = GetStudyMetadataBlockBlobAndValidateId(studyInstanceUID);

            return await cloudBlockBlob.CatchStorageExceptionAndThrowDataStoreException(
                async (blockBlob) =>
                {
                    _logger.LogDebug($"Getting Instances in Series: {seriesInstanceUID}, Study: {studyInstanceUID}");
                    DicomStudyMetadata metadata = await ReadMetadataAsync(blockBlob, cancellationToken);

                    if (!metadata.SeriesMetadata.ContainsKey(seriesInstanceUID))
                    {
                        throw new DataStoreException(HttpStatusCode.NotFound);
                    }

                    return metadata.GetDicomInstances(seriesInstanceUID);
                });
        }

        /// <inheritdoc />
        public async Task<IEnumerable<DicomInstance>> DeleteStudyAsync(string studyInstanceUID, CancellationToken cancellationToken = default)
        {
            CloudBlockBlob cloudBlockBlob = GetStudyMetadataBlockBlobAndValidateId(studyInstanceUID);

            // Retry when the pre-condition fails on replace (ETag check).
            IAsyncPolicy retryPolicy = CreatePreConditionFailedRetryPolicy();
            return await cloudBlockBlob.CatchStorageExceptionAndThrowDataStoreException(
                async (blockBlob) =>
                {
                    DicomStudyMetadata metadata = await ReadMetadataAsync(blockBlob, cancellationToken);

                    _logger.LogDebug($"Deleting Study Metadata: {studyInstanceUID}");

                    // Attempt to delete, validating ETag
                    await DeleteCloudBlockBlobAsync(blockBlob, cancellationToken);

                    // Now return the instances that have been deleted.
                    return metadata.GetDicomInstances();
                },
                retryPolicy);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<DicomInstance>> DeleteSeriesAsync(string studyInstanceUID, string seriesInstanceUID, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUID, nameof(seriesInstanceUID));
            CloudBlockBlob cloudBlockBlob = GetStudyMetadataBlockBlobAndValidateId(studyInstanceUID);

            // Retry when the pre-condition fails on replace (ETag check).
            IAsyncPolicy retryPolicy = CreatePreConditionFailedRetryPolicy();
            return await cloudBlockBlob.CatchStorageExceptionAndThrowDataStoreException(
                async (blockBlob) =>
                {
                    DicomStudyMetadata metadata = await ReadMetadataAsync(blockBlob, cancellationToken);

                    if (!metadata.SeriesMetadata.ContainsKey(seriesInstanceUID))
                    {
                        throw new DataStoreException(HttpStatusCode.NotFound);
                    }

                    _logger.LogDebug($"Deleting Series Metadata: {seriesInstanceUID}");

                    // Fetch the instances about to removed in the series, then remove from the dictionary.
                    IEnumerable<DicomInstance> removedInstances = metadata.GetDicomInstances(seriesInstanceUID);
                    metadata.SeriesMetadata.Remove(seriesInstanceUID);

                    // If no more series in the study, lets delete the file, otherwise update.
                    if (metadata.SeriesMetadata.Count == 0)
                    {
                        await DeleteCloudBlockBlobAsync(blockBlob, cancellationToken);
                    }
                    else
                    {
                        await UpdateMetadataAsync(blockBlob, metadata, cancellationToken);
                    }

                    return removedInstances;
                },
                retryPolicy);
        }

        /// <inheritdoc />
        public async Task DeleteInstanceAsync(string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUID, nameof(seriesInstanceUID));
            EnsureArg.IsNotNullOrWhiteSpace(sopInstanceUID, nameof(sopInstanceUID));
            CloudBlockBlob cloudBlockBlob = GetStudyMetadataBlockBlobAndValidateId(studyInstanceUID);

            // Retry when the pre-condition fails on replace (ETag check).
            IAsyncPolicy retryPolicy = CreatePreConditionFailedRetryPolicy();
            await cloudBlockBlob.CatchStorageExceptionAndThrowDataStoreException(
                async (blockBlob) =>
                {
                    DicomStudyMetadata metadata = await ReadMetadataAsync(blockBlob, cancellationToken);

                    // Validate the Series & Study is in the fetched metadata.
                    if (!metadata.TryRemoveInstance(new DicomInstance(studyInstanceUID, seriesInstanceUID, sopInstanceUID)))
                    {
                        throw new DataStoreException(HttpStatusCode.NotFound);
                    }

                    _logger.LogDebug($"Deleting Instance Metadata for SOP instance '{sopInstanceUID}'");

                    // If this instance was also the last instance in the entire study, we should delete the file, otherwise update.
                    if (metadata.SeriesMetadata.Count == 0)
                    {
                        await DeleteCloudBlockBlobAsync(blockBlob, cancellationToken);
                    }
                    else
                    {
                        await UpdateMetadataAsync(blockBlob, metadata, cancellationToken);
                    }
                },
                retryPolicy);
        }

        private static IAsyncPolicy CreatePreConditionFailedRetryPolicy()
           => Policy
                   .Handle<StorageException>(ex => ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.PreconditionFailed || ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.TooManyRequests)
                   .RetryForeverAsync();

        private CloudBlockBlob GetStudyMetadataBlockBlobAndValidateId(string studyInstanceUID)
        {
            EnsureArg.IsTrue(DicomIdentifierValidator.IdentifierRegex.IsMatch(studyInstanceUID), nameof(studyInstanceUID));

            var blobName = $"{studyInstanceUID}_metadata";

            // Use the Azure storage SDK to validate the blob name; only specific values are allowed here.
            // Check here for more information: https://blogs.msdn.microsoft.com/jmstall/2014/06/12/azure-storage-naming-rules/
            NameValidator.ValidateBlobName(blobName);

            return _container.GetBlockBlobReference(blobName);
        }

        private async Task<DicomStudyMetadata> ReadMetadataAsync(CloudBlockBlob cloudBlockBlob, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(cloudBlockBlob, nameof(cloudBlockBlob));

            var json = await cloudBlockBlob.DownloadTextAsync(
                                            _metadataEncoding,
                                            new AccessCondition(),
                                            new BlobRequestOptions(),
                                            new OperationContext(),
                                            cancellationToken);
            return JsonConvert.DeserializeObject<DicomStudyMetadata>(json);
        }

        private async Task UpdateMetadataAsync(CloudBlockBlob cloudBlockBlob, DicomStudyMetadata metadata, CancellationToken cancellationToken)
        {
            // Validate nulls and check the metadata has at least one series in it, otherwise we should be deleting this.
            EnsureArg.IsNotNull(cloudBlockBlob, nameof(cloudBlockBlob));
            EnsureArg.IsNotNull(metadata, nameof(metadata));
            EnsureArg.IsGt(metadata.SeriesMetadata.Count, 0, nameof(metadata));

            var json = JsonConvert.SerializeObject(metadata);
            await cloudBlockBlob.UploadTextAsync(
                json,
                _metadataEncoding,
                accessCondition: AccessCondition.GenerateIfMatchCondition(cloudBlockBlob.Properties.ETag),
                new BlobRequestOptions(),
                new OperationContext(),
                cancellationToken);
        }

        private async Task DeleteCloudBlockBlobAsync(CloudBlockBlob cloudBlockBlob, CancellationToken cancellationToken)
        {
            // Attempt to delete, validating ETag
            await cloudBlockBlob.DeleteAsync(
                DeleteSnapshotsOption.IncludeSnapshots,
                accessCondition: AccessCondition.GenerateIfMatchCondition(cloudBlockBlob.Properties.ETag),
                new BlobRequestOptions(),
                new OperationContext(),
                cancellationToken);
        }
    }
}
