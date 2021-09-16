// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Net.Http;

namespace Microsoft.Health.Dicom.Client
{
    public partial interface IDicomWebClient
    {
        Func<MemoryStream> GetMemoryStream { get; set; }
        HttpClient HttpClient { get; }
    }
}
