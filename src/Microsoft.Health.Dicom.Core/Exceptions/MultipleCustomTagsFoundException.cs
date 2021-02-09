// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when two custom tags are found for a given tag path.
    /// </summary>
    public class MultipleCustomTagsFoundException : DicomServerException
    {
        public MultipleCustomTagsFoundException(string message)
            : base(message)
        {
        }
}
}
