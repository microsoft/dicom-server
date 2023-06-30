// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Blob.Utilities;

internal sealed class BlobStoreConfigurationSection : StoreConfigurationSection
{
    public BlobStoreConfigurationSection()
        : base(Constants.BlobStoreConfigurationSection, Constants.BlobContainerConfigurationName)
    {
    }
}

internal sealed class MetadataStoreConfigurationSection : StoreConfigurationSection
{
    public MetadataStoreConfigurationSection()
        : base(Constants.MetadataStoreConfigurationSection, Constants.MetadataContainerConfigurationName)
    {
    }
}

internal sealed class SystemStoreConfigurationSection : StoreConfigurationSection
{
    public SystemStoreConfigurationSection()
        : base(Constants.SystemStoreConfigurationSection, Constants.SystemContainerConfigurationName)
    {
    }
}

internal sealed class WorkitemStoreConfigurationSection : StoreConfigurationSection
{
    public WorkitemStoreConfigurationSection()
        : base(Constants.WorkitemStoreConfigurationSection, Constants.WorkitemContainerConfigurationName)
    {
    }
}

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
