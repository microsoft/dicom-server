// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    /// <summary>
    /// Exception that is thrown when partition Id is missing in the route values.
    /// </summary>
    public class DataPartitionsMissingPartitionException : BadRequestException
    {
        public DataPartitionsMissingPartitionException()
            : base(string.Format(CultureInfo.InvariantCulture, DicomCoreResource.DataPartitionsMissingPartitions))
        {
        }
    }
}
