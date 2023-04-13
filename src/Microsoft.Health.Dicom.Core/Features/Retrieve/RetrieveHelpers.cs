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

namespace Microsoft.Health.Dicom.Core.Features.Retrieve;
internal static class RetrieveHelpers
{
    public static async Task<FileProperties> CheckFileSize(IFileStore blobDataStore, long maxDicomFileSize, long version, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(blobDataStore, nameof(blobDataStore));

        FileProperties fileProperties = await blobDataStore.GetFilePropertiesAsync(version, cancellationToken);

        // limit the file size that can be read in memory
        if (fileProperties.ContentLength > maxDicomFileSize)
        {
            throw new NotAcceptableException(string.Format(CultureInfo.CurrentCulture, DicomCoreResource.RetrieveServiceFileTooBig, maxDicomFileSize));
        }

        return fileProperties;
    }
}
