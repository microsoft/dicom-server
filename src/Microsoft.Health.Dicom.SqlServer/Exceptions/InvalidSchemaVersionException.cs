// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Exceptions;

namespace Microsoft.Health.Dicom.SqlServer.Exceptions
{
    internal class InvalidSchemaVersionException : DicomServerException
    {
        public InvalidSchemaVersionException(string message)
            : base(message)
        {
        }
    }
}
