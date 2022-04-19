// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace Microsoft.Health.Dicom.Api.Web;

public interface IHttpStreamContent
{
    IEnumerable<KeyValuePair<string, IEnumerable<string>>> Headers { get; init; }
    Stream Stream { get; init; }
}