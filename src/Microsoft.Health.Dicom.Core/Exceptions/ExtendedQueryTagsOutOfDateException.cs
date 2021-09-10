// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when the client is missing one or more extended query tags from the server.
    /// </summary>
    public class ExtendedQueryTagsOutOfDateException
        : DicomServerException
    {
        public ExtendedQueryTagsOutOfDateException()
            : base(DicomCoreResource.ExtendedQueryTagsOutOfDate)
        {
        }
    }
}
