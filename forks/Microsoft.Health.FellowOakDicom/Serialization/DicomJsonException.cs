// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Runtime.Serialization;
using System.Text.Json;

namespace Microsoft.Health.FellowOakDicom.Serialization;

[System.Serializable]
public class DicomJsonException: JsonException
{
    /// <summary>
    /// Creates a new exception object to relay error information to the user.
    /// </summary>
    /// <param name="message">The context specific error message.</param>
    public DicomJsonException(string message) : base(message)
    {
    }

    protected DicomJsonException(
        SerializationInfo serializationInfo,
        StreamingContext streamingContext) : base(serializationInfo, streamingContext)
    {
    }
}
