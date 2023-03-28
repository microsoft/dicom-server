// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Features.Common;

public interface IFeatureConfigurationService
{
    Task<bool> IsFeatureEnabled(string featureName);
}
