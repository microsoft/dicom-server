// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Tests.Common;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Common;

public class TestApplication : IEquatable<TestApplication>
{
    public TestApplication(string id)
    {
        Id = id;
        ClientId = TestEnvironment.Variables[$"app_{Id}_id"] ?? Id;
        ClientSecret = TestEnvironment.Variables[$"app_{Id}_secret"] ?? Id;
        GrantType = TestEnvironment.Variables[$"app_{Id}_grant_type"] ?? "client_credentials";
    }

    private string Id { get; }

    public string ClientId { get; }

    public string ClientSecret { get; }

    public string GrantType { get; }

    public bool Equals(TestApplication other)
    {
        if (other is null)
        {
            return false;
        }

        return ReferenceEquals(this, other) || Id == other.Id;
    }

    public override bool Equals(object obj)
        => obj is TestApplication other && Equals(other);

    public override int GetHashCode()
        => (Id?.GetHashCode()).GetValueOrDefault();
}
