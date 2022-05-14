// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Health.Dicom.Client.Models.Export;

namespace Microsoft.Health.Dicom.Client.Models;

/// <summary>
/// Represents the destination for DICOM files copied by an export operation.
/// </summary>
public sealed class ExportDestination
{
    /// <summary>
    /// Gets the type of destination this instance represents.
    /// </summary>
    /// <value>A type denoting the kind of destination.</value>
    public ExportDestinationType Type { get; }

    internal object Configuration { get; }

    private ExportDestination(ExportDestinationType type, object configuration)
    {
        Type = type;
        Configuration = EnsureArg.IsNotNull(configuration, nameof(configuration));
    }

    /// <summary>
    /// Creates an export destination for Azure Blob storage based on a URI.
    /// </summary>
    /// <remarks>
    /// The <paramref name="containerUri"/> may contain a SAS token for authentication.
    /// </remarks>
    /// <param name="containerUri">A URI specifying the Azure Blob container.</param>
    /// <returns>The corresponding export destination value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="containerUri"/> is <see langword="null"/>.</exception>
    public static ExportDestination ForAzureBlobStorage(Uri containerUri)
    {
        EnsureArg.IsNotNull(containerUri, nameof(containerUri));
        return new ExportDestination(ExportDestinationType.AzureBlob, new AzureBlobExportOptions { ContainerUri = containerUri });
    }

    /// <summary>
    /// Creates an export destination for Azure Blob storage based on a connection string and container name.
    /// </summary>
    /// <remarks>
    /// The <paramref name="connectionString"/> may contain a SAS token for authentication.
    /// </remarks>
    /// <param name="connectionString">A connection string for the Azure Blob Storage account.</param>
    /// <param name="containerName">The name of the blob container.</param>
    /// <returns>The corresponding export destination value.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="connectionString"/> or <paramref name="containerName"/> is empty or
    /// consists of white space characters.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="connectionString"/> or <paramref name="containerName"/> is <see langword="null"/>.
    /// </exception>
    public static ExportDestination ForAzureBlobStorage(string connectionString, string containerName)
    {
        EnsureArg.IsNotNullOrWhiteSpace(connectionString, nameof(connectionString));
        EnsureArg.IsNotNullOrWhiteSpace(containerName, nameof(containerName));
        return new ExportDestination(
            ExportDestinationType.AzureBlob,
            new AzureBlobExportOptions
            {
                ConnectionString = connectionString,
                ContainerName = containerName,
            });
    }
}
