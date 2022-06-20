// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.SqlServer.Features.Schema.Manager.Model;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.SchemaManager.UnitTests;

public class DicomSchemaClientTests
{
    private readonly IScriptProvider _scriptProvider = Substitute.For<IScriptProvider>();
    private readonly ISchemaDataStore _schemaDataStore = Substitute.For<ISchemaDataStore>();
    private readonly ISchemaManagerDataStore _schemaManagerDataStore = Substitute.For<ISchemaManagerDataStore>();

    [Fact]
    public async void GivenCurrentVersionAboveOne_GetAvailableVersions_ShouldReturnCorrectVersionsAsync()
    {
        //Arrange
        int currentVersion = 5;
        _schemaManagerDataStore.GetCurrentSchemaVersionAsync(default).Returns(currentVersion);

        var dicomSchemaClient = new DicomSchemaClient(_scriptProvider, _schemaDataStore, _schemaManagerDataStore);

        //Act
        List<AvailableVersion>? actualVersions = await dicomSchemaClient.GetAvailabilityAsync();

        var expectedVersions = new List<AvailableVersion>();
        int numberOfAvailableVersions = SchemaVersionConstants.Max - currentVersion + 1;
        foreach (int version in Enumerable.Range(currentVersion, numberOfAvailableVersions))
        {
            expectedVersions.Add(new AvailableVersion(version, string.Empty, string.Empty));
        }

        //Assert
        Assert.Equal(
            expectedVersions.Select(x => x.Id),
            actualVersions.Select(x => x.Id));
    }

    [Fact]
    public async void GivenCurrentVersionOfMax_GetAvailableVersionsShouldReturnOneVersion()
    {
        //Arrange
        _schemaManagerDataStore.GetCurrentSchemaVersionAsync(default).Returns(SchemaVersionConstants.Max);
        var dicomSchemaClient = new DicomSchemaClient(_scriptProvider, _schemaDataStore, _schemaManagerDataStore);

        //Act
        var actualVersions = await dicomSchemaClient.GetAvailabilityAsync();
        var expectedVersions = new List<AvailableVersion>()
        {
            new AvailableVersion(SchemaVersionConstants.Max, string.Empty, string.Empty),
        };

        //Assert
        Assert.Equal(
            expectedVersions.Select(x => x.Id),
            actualVersions.Select(x => x.Id));
    }
}
