// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when custom tag is busy. (e.g: trying to delete custom tag when it's reindexing)
    /// </summary>
    public class CustomTagBusyException : ValidationException
    {
        public CustomTagBusyException(string message)
            : base(message)
        {
        }
    }
}
