// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Health.Dicom.Client.Models.Export;

namespace Microsoft.Health.Dicom.Client.Models;

/// <summary>
/// A collection of <see langword="static"/> utilities for creating export destination options
/// that specify to where data should be copied from the DICOM server.
/// </summary>
public static class ExportDestination
{
    /// <summary>
    /// Creates export destination options for Azure Blob storage based on a URI.
    /// </summary>
    /// <remarks>
    /// The <paramref name="blobContainerUri"/> may contain a SAS token for authentication.
    /// </remarks>
    /// <param name="blobContainerUri">A URI specifying the Azure Blob container.</param>
    /// <returns>The corresponding export destination options.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="blobContainerUri"/> is <see langword="null"/>.</exception>
    public static ExportDataOptions<ExportDestinationType> ForAzureBlobStorage(Uri blobContainerUri)
    {
        EnsureArg.IsNotNull(blobContainerUri, nameof(blobContainerUri));
        return new ExportDataOptions<ExportDestinationType>(
            ExportDestinationType.AzureBlob,
            new AzureBlobExportOptions { BlobContainerUri = blobContainerUri });
    }

    /// <summary>
    /// Creates export destination options for Azure Blob storage based on a connection string and container name.
    /// </summary>
    /// <remarks>
    /// The <paramref name="connectionString"/> may contain a SAS token for authentication.
    /// </remarks>
    /// <param name="connectionString">A connection string for the Azure Blob Storage account.</param>
    /// <param name="blobContainerName">The name of the blob container.</param>
    /// <returns>The corresponding export destination options.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="connectionString"/> or <paramref name="blobContainerName"/> is empty or
    /// consists of white space characters.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="connectionString"/> or <paramref name="blobContainerName"/> is <see langword="null"/>.
    /// </exception>
    public static ExportDataOptions<ExportDestinationType> ForAzureBlobStorage(string connectionString, string blobContainerName)
    {
        EnsureArg.IsNotNullOrWhiteSpace(connectionString, nameof(connectionString));
        EnsureArg.IsNotNullOrWhiteSpace(blobContainerName, nameof(blobContainerName));
        return new ExportDataOptions<ExportDestinationType>(
            ExportDestinationType.AzureBlob,
            new AzureBlobExportOptions
            {
                ConnectionString = connectionString,
                BlobContainerName = blobContainerName,
            });
    }
}
