// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;

namespace Microsoft.Health.Dicom.Client;

public class DicomWebResponse<T> : DicomWebResponse
{
    private readonly Func<HttpContent, CancellationToken, Task<T>> _valueFactory;

    public DicomWebResponse(HttpResponseMessage response, JsonSerializerOptions jsonSerializerOptions)
        : this(response, (c, t) => c.ReadFromJsonAsync<T>(jsonSerializerOptions, t))
        => EnsureArg.IsNotNull(jsonSerializerOptions, nameof(jsonSerializerOptions));

    public DicomWebResponse(HttpResponseMessage response, Func<HttpContent, CancellationToken, Task<T>> valueFactory)
        : base(response)
        => _valueFactory = EnsureArg.IsNotNull(valueFactory, nameof(valueFactory));

    public Task<T> GetValueAsync(CancellationToken cancellationToken = default)
        => _valueFactory(Content, cancellationToken);
}
