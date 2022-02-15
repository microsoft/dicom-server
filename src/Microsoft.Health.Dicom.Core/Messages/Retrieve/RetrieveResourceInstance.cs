// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;

namespace Microsoft.Health.Dicom.Core.Messages.Retrieve
{
    public class RetrieveResourceInstance
    {
        public RetrieveResourceInstance(Stream stream, string transferSyntaxUid = null)
        {
            Stream = stream;
            TransferSyntaxUid = transferSyntaxUid;
        }

        public Stream Stream { get; }
        public string TransferSyntaxUid { get; }
    }
}
