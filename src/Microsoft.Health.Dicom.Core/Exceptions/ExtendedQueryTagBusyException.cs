// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when extended query tag is busy. (e.g: trying to delete extended query tag when it's reindexing)
    /// </summary>
    public class ExtendedQueryTagBusyException : ValidationException
    {
        public ExtendedQueryTagBusyException(string message)
            : base(message)
        {
        }
    }
}
