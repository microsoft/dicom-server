// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------


using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve;
public static class RetrieveHelpers
{
    public static async Task<FileProperties> CheckFileSize(IFileStore blobDataStore, long maxDicomFileSize, InstanceMetadata instance, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(blobDataStore, nameof(blobDataStore));
        EnsureArg.IsNotNull(instance, nameof(instance));

        FileProperties fileProperties = await blobDataStore.GetFilePropertiesAsync(instance.VersionedInstanceIdentifier, cancellationToken);

        // limit the file size that can be read in memory
        if (fileProperties.ContentLength > maxDicomFileSize)
        {
            throw new NotAcceptableException(string.Format(CultureInfo.CurrentCulture, DicomCoreResource.RetrieveServiceFileTooBig, maxDicomFileSize));
        }

        return fileProperties;
    }
}
