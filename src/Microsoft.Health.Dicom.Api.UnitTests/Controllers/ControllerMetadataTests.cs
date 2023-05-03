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
using Microsoft.Health.Dicom.Api.Controllers;
using Microsoft.Health.Dicom.Api.Features.Conventions;
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

    [Fact]
    public void GivenExportControllers_WhenQueryingApiVersion_ThenSupportedfromV1()
    {
        var expectedStartedVersion = 1;
        int? actualStartedVersion = Attribute.GetCustomAttributes(typeof(ExportController), typeof(IntroducedInApiVersionAttribute))
             .Select(a => ((IntroducedInApiVersionAttribute)a).Version)
             .SingleOrDefault();
        Assert.Equal(expectedStartedVersion, actualStartedVersion);
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
