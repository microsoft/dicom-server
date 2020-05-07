// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Web;

namespace Microsoft.Health.Dicom.Blob.Features.Storage
{
    /// <summary>
    /// Provides functionality for managing the DICOM files using the Azure Blob storage.
    /// </summary>
    public class BlobFileStore : IFileStore
    {
        private readonly CloudBlobContainer _container;

        public BlobFileStore(
            CloudBlobClient client,
            IOptionsMonitor<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(namedBlobContainerConfigurationAccessor, nameof(namedBlobContainerConfigurationAccessor));

            BlobContainerConfiguration containerConfiguration = namedBlobContainerConfigurationAccessor.Get(Constants.ContainerConfigurationName);

            _container = client.GetContainerReference(containerConfiguration.ContainerName);
        }

        /// <inheritdoc />
        public async Task<Uri> AddFileAsync(
            VersionedInstanceIdentifier versionedInstanceIdentifier,
            Stream stream,
            bool overwriteIfExists,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));
            EnsureArg.IsNotNull(stream, nameof(stream));

            CloudBlockBlob blob = GetBlockBlobReference(versionedInstanceIdentifier);

            blob.Properties.ContentType = KnownContentTypes.ApplicationDicom;

            try
            {
                // Will throw if the provided resource identifier already exists.
                await blob.UploadFromStreamAsync(
                    stream,
                    overwriteIfExists ? AccessCondition.GenerateEmptyCondition() : AccessCondition.GenerateIfNotExistsCondition(),
                    new BlobRequestOptions(),
                    new OperationContext(),
                    cancellationToken);

                return blob.Uri;
            }
            catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.Conflict)
            {
                throw new InstanceAlreadyExistsException();
            }
            catch (Exception ex)
            {
                throw new DataStoreException(ex);
            }
        }

        /// <inheritdoc />
        public async Task DeleteFileIfExistsAsync(
            VersionedInstanceIdentifier versionedInstanceIdentifier,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));

            CloudBlockBlob blob = GetBlockBlobReference(versionedInstanceIdentifier);

            await ExecuteAsync(() => blob.DeleteIfExistsAsync(cancellationToken));
        }

        /// <inheritdoc />
        public async Task<Stream> GetFileAsync(
            VersionedInstanceIdentifier versionedInstanceIdentifier,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));

            CloudBlockBlob blob = GetBlockBlobReference(versionedInstanceIdentifier);

            Stream stream = null;

            await ExecuteAsync(async () => stream = await blob.OpenReadAsync(cancellationToken));

            return stream;
        }

        private CloudBlockBlob GetBlockBlobReference(VersionedInstanceIdentifier versionedInstanceIdentifier)
        {
            string blobName = $"{versionedInstanceIdentifier.StudyInstanceUid}/{versionedInstanceIdentifier.SeriesInstanceUid}/{versionedInstanceIdentifier.SopInstanceUid}_{versionedInstanceIdentifier.Version}";

            // Use the Azure storage SDK to validate the blob name; only specific values are allowed here.
            // Check here for more information: https://blogs.msdn.microsoft.com/jmstall/2014/06/12/azure-storage-naming-rules/
            NameValidator.ValidateBlobName(blobName);

            return _container.GetBlockBlobReference(blobName);
        }

        private async Task ExecuteAsync(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
            {
                throw new InstanceNotFoundException();
            }
            catch (Exception ex)
            {
                throw new DataStoreException(ex);
            }
        }
    }
}
