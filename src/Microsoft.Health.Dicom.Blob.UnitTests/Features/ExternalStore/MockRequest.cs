// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Azure.Core;

namespace Microsoft.Health.Dicom.Blob.UnitTests.Features.ExternalStore;

internal class MockRequest : Request
{
    public MockRequest()
    {
        MockHeaders = new List<HttpHeader>();
    }

    public List<HttpHeader> MockHeaders { get; }

    public override string ClientRequestId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public override void Dispose() => throw new NotImplementedException();

    protected override void AddHeader(string name, string value)
    {
        MockHeaders.Add(new HttpHeader(name, value));
    }

    protected override bool ContainsHeader(string name)
    {
        return MockHeaders.Any(h => h.Name == name);
    }

    protected override IEnumerable<HttpHeader> EnumerateHeaders()
    {
        return MockHeaders;
    }

    protected override bool RemoveHeader(string name) => throw new NotImplementedException();
    protected override bool TryGetHeader(string name, [NotNullWhen(true)] out string value) => throw new NotImplementedException();
    protected override bool TryGetHeaderValues(string name, [NotNullWhen(true)] out IEnumerable<string> values) => throw new NotImplementedException();
}
