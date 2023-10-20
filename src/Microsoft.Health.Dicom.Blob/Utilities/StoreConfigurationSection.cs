// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Blob.Utilities;

/// <summary>
/// 
/// </summary>
internal sealed class BlobStoreConfigurationSection : StoreConfigurationSection
{
    public BlobStoreConfigurationSection()
        : base(BlobConstants.BlobStoreConfigurationSection, BlobConstants.BlobContainerConfigurationName)
    {
    }
}

/// <summary>
/// 
/// </summary>
internal sealed class MetadataStoreConfigurationSection : StoreConfigurationSection
{
    public MetadataStoreConfigurationSection()
        : base(BlobConstants.MetadataStoreConfigurationSection, BlobConstants.MetadataContainerConfigurationName)
    {
    }
}

/// <summary>
/// 
/// </summary>
internal sealed class WorkitemStoreConfigurationSection : StoreConfigurationSection
{
    public WorkitemStoreConfigurationSection()
        : base(BlobConstants.WorkitemStoreConfigurationSection, BlobConstants.WorkitemContainerConfigurationName)
    {
    }
}

/// <summary>
/// 
/// </summary>
internal class StoreConfigurationSection : IStoreConfigurationSection
{
    internal StoreConfigurationSection(string sectionName, string name)
    {
        ConfigurationSectionName = sectionName;
        ContainerConfigurationName = name;
    }

    public string ContainerConfigurationName { get; }

    public string ConfigurationSectionName { get; }
}
