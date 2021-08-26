// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DateShiftScope
    {
        [EnumMember(Value = "StudyInstance")]
        StudyInstance,
        [EnumMember(Value = "SeriesInstance")]
        SeriesInstance,
        [EnumMember(Value = "SopInstance")]
        SopInstance,
    }
}
