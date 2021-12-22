// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Blob.Utilities
{
    /// <summary>
    /// 
    /// </summary>
    internal abstract class StoreConfigurationAware : IStoreConfigurationAware
    {
        public StoreConfigurationAware(string sectionName, string name)
        {
            SectionName = sectionName;
            Name = name;
        }

        public string Name { get; }

        public string SectionName { get; }
    }
}
