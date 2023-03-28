// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.FeatureManagement;

namespace Microsoft.Health.Dicom.Api.Web;

[FilterAlias("CustomFilter")]
public class CustomFeatureFilter : IFeatureFilter
{
    public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
    {
        EnsureArg.IsNotNull(context, nameof(context));

        string serviceName = context.Parameters.GetSection("ServiceName").Value;
        if (serviceName.Contains("k1kdicom1", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }
}
