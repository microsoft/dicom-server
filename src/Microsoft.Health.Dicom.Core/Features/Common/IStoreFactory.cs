// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Common
{
    /// <summary>
    /// Factory to produce  data store.
    /// </summary>
    public interface IStoreFactory<T>
    {
        /// <summary>
        /// Get data store instance.
        /// </summary>
        /// <returns>The instance.</returns>
        T GetInstance();
    }
}
