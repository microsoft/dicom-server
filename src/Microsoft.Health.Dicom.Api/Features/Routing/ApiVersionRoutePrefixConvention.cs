// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Microsoft.Health.Dicom.Api.Features.Routing;

public class ApiVersionRoutePrefixConvention : IApplicationModelConvention
{
    private readonly string _versionConstraintTemplate;

    public ApiVersionRoutePrefixConvention()
    {
        _versionConstraintTemplate = "v{version:apiVersion}";
    }

    public void Apply(ApplicationModel application)
    {
        EnsureArg.IsNotNull(application, nameof(application));

        foreach (var applicationController in application.Controllers)
        {
            foreach (var action in applicationController.Actions)
            {
                foreach (var selector in action.Selectors)
                {
                    var versionedConstraintRouteModel = new AttributeRouteModel
                    {
                        Template = _versionConstraintTemplate,
                    };

                    // Prefix version to the existing route.
                    selector.AttributeRouteModel = AttributeRouteModel.CombineAttributeRouteModel(versionedConstraintRouteModel, selector.AttributeRouteModel);
                }
            }
        }
    }
}
