// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.DicomCast.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when the server is too busy.
    /// </summary>
    public class ServerTooBusyException : RetryableException
    {
        public ServerTooBusyException()
        {
        }
    }
}
