// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.Json;
using Dicom;

namespace Microsoft.Health.Dicom.Json.Serialization.Parsers
{
    public interface IDicomItemParser
    {
        DicomItem Parse(DicomTag tag, JsonElement element);
    }
}
