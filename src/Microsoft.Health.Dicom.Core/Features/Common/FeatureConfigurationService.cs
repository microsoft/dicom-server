// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using EnsureThat;
using Microsoft.FeatureManagement;

namespace Microsoft.Health.Dicom.Core.Features.Common;

public class FeatureConfigurationService : IFeatureConfigurationService
{

    private readonly IFeatureManager _featureManager;
    public FeatureConfigurationService(IFeatureManager featureManager)
    {
        _featureManager = EnsureArg.IsNotNull(featureManager, nameof(featureManager));
    }

    public async Task<bool> IsFeatureEnabled(string featureName)
    {
        EnsureArg.IsNotNullOrWhiteSpace(featureName, nameof(featureName));
        return await _featureManager.IsEnabledAsync(featureName);
    }
}

public static class FeatureConstants
{
    public const string EnableOhifViewer = "EnableOhifViewer";
    public const string EnableFullDicomItemValidation = "EnableFullDicomItemValidation";
    public const string EnableDataPartitions = "EnableDataPartitions";
    public const string EnableExport = "EnableExport";
    public const string EnableLatestApiVersion = "EnableLatestApiVersion";
}
