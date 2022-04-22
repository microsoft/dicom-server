// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace Microsoft.Health.Dicom.Api.Web;

public class DicomStreamContent : IHttpStreamContent
{
    public Stream Stream { get; init; }

    // could not use HttpContentHeaders since it has no public constructors. HttpHeaders is abstract class
    public IEnumerable<KeyValuePair<string, IEnumerable<string>>> Headers { get; init; }
}
