// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Health.Dicom.Blob.Utilities;

internal class ExternalBlobDataStoreConfiguration
{
    public const string SectionName = "ExternalBlobStore";

    public Uri BlobContainerUri { get; set; }

    // use for local testing with Azurite
    public string ConnectionString { get; set; }

    // use for local testing with Azurite
    public string ContainerName { get; set; }

    public string HealthCheckFilePath { get; set; } = "healthCheck/health";

    [Range(typeof(TimeSpan), "00:01:00", "1.00:00:00", ConvertValueInInvariantCulture = true, ParseLimitsInInvariantCulture = true)]
    public TimeSpan HealthCheckFileExpiry { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// A path which is used to store blobs along a specific path in a container, serving as a prefix to the
    /// full blob name and providing a logical hierarchy when segments used though use of forward slashes (/).
    /// DICOM allows any alphanumeric characters, dashes(-). periods(.) and forward slashes (/) in the service store path.
    /// This path will be supplied externally when DICOM is a managed service.
    /// Max of 1024 characters total and max of 254 forward slashes allowed.
    /// See https://learn.microsoft.com/en-us/rest/api/storageservices/naming-and-referencing-containers--blobs--and-metadata#blob-names
    /// </summary>
    [RegularExpression(@"^[a-zA-Z0-9\-\.]*(\/[a-zA-Z0-9\-\.]*){0,254}$")]
    [StringLength(1024)]
    public string StorageDirectory { get; set; }
}
