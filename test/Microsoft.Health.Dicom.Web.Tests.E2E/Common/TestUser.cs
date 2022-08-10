// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Health.Dicom.Tests.Common;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Common;

public class TestUser : IEquatable<TestUser>
{
    public TestUser(string id)
    {
        Id = id;
        UserId = TestEnvironment.Variables[$"user_{Id}_id"] ?? Id;
        Password = TestEnvironment.Variables[$"user_{Id}_secret"] ?? Id;
    }

    private string Id { get; }

    public string UserId { get; }

    public string Password { get; }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Instance member for consistency.")]
    public string GrantType => "password";

    public bool Equals(TestUser other)
    {
        if (other is null)
        {
            return false;
        }

        return ReferenceEquals(this, other) || Id == other.Id;
    }

    public override bool Equals(object obj)
        => obj is TestUser other && Equals(other);

    public override int GetHashCode()
        => (Id?.GetHashCode()).GetValueOrDefault();
}
