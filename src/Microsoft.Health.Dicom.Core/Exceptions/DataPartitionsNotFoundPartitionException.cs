// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    /// <summary>
    /// Exception that is thrown when partition name is not found
    /// </summary>
    public class DataPartitionsNotFoundPartitionException : BadRequestException
    {
        public DataPartitionsNotFoundPartitionException(string partitionName)
            : base(string.Format(CultureInfo.InvariantCulture, DicomCoreResource.DataPartitionNotFound, partitionName))
        {
        }
    }
}
