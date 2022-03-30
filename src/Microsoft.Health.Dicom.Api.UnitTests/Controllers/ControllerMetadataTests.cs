// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Api.Modules;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Controllers;

public class ControllerMetadataTests
{
    private readonly List<Type> _controllerTypes = typeof(WebModule).Assembly.ExportedTypes
        .Where(t => t.IsSubclassOf(typeof(ControllerBase)))
        .Where(t => !t.IsAbstract)
        .ToList();

    [Fact]
    public void GivenApi_WhenCountingControllers_ThenFindExpectedNumber()
        => Assert.Equal(10, _controllerTypes.Count);

    [Theory]
    [InlineData("1.0-prerelease")]
    [InlineData("1")]
    public void GivenControllers_WhenQueryingApiVersion_ThenFindCorrectValue(string version)
    {
        var expected = ApiVersion.Parse(version);
        foreach (Type controllerType in _controllerTypes)
        {
            Attribute[] apiVersions = Attribute.GetCustomAttributes(controllerType, typeof(ApiVersionAttribute));
            Assert.Contains(apiVersions.Cast<ApiVersionAttribute>(), x => x.Versions.Single() == expected);
        }
    }

    [Fact]
    public void GivenControllerActions_WhenQueryingAttributes_ThenFindAuditEventType()
    {
        foreach (Type controllerType in _controllerTypes)
        {
            // By convention, all of our actions are annotated by an HttpMethodAttribute
            List<MethodInfo> actions = controllerType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(t => Attribute.GetCustomAttribute(t, typeof(HttpMethodAttribute)) != null)
                .ToList();

            Assert.True(actions.Count > 0);
            foreach (MethodInfo a in actions)
            {
                Assert.NotNull(Attribute.GetCustomAttribute(a, typeof(AuditEventTypeAttribute)));
            }
        }
    }
}
