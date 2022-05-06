// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.SqlServer.Features.Schema.Model;
using NSubstitute;
using Xunit;

namespace DicomSchemaManager.Core.UnitTests;

public class DicomSqlSchemaManagerTests
{
    //the thing to test
    private readonly DicomSqlSchemaManager _dicomSqlSchemaManager;

    //dependencies to mock
    private readonly ISchemaDataStore _schemaDataStore = Substitute.For<ISchemaDataStore>();
    private readonly ISchemaManagerDataStore _schemaManagerDataStore = Substitute.For<ISchemaManagerDataStore>();
    private readonly IScriptProvider _scriptProvider = Substitute.For<IScriptProvider>();
    private readonly IBaseSchemaRunner _baseSchemaRunner = Substitute.For<IBaseSchemaRunner>();

    //other dependencies
    private readonly string _connectionString = "localhost";
    private readonly CancellationToken _cancellationToken = new CancellationToken();
    private readonly CompatibleVersions _fullCompatibility = new CompatibleVersions((int)SchemaVersion.V1, SchemaVersionConstants.Max);

    public DicomSqlSchemaManagerTests()
    {
        _dicomSqlSchemaManager = new(_scriptProvider, _schemaDataStore, _baseSchemaRunner, _schemaManagerDataStore);
    }

    [Theory(Skip = "Currently not implemented")]
    [InlineData((int)SchemaVersion.V1, SchemaVersionConstants.Max)]
    [InlineData((int)SchemaVersion.V10, (int)SchemaVersion.V11)]
    public async void GivenSchemaWithOldVersion_WhenApplyingNewCompatibleVersion_ThenReturnSuccess(int lowVersionNumber, int highVersionNumber)
    {
        //Arrange
        _schemaManagerDataStore.GetCurrentSchemaVersionAsync(_cancellationToken).Returns(lowVersionNumber);
        _schemaDataStore.GetLatestCompatibleVersionsAsync(_cancellationToken).Returns(_fullCompatibility);

        //Act
        ApplyCommandResult result = await _dicomSqlSchemaManager.ApplySchema(_connectionString, highVersionNumber, _cancellationToken);

        //Assert
        Assert.Equal(ApplyCommandResult.Successful, result);
    }

    [Theory]
    [InlineData(SchemaVersionConstants.Max, (int)SchemaVersion.V1)]
    [InlineData((int)SchemaVersion.V11, (int)SchemaVersion.V10)]
    [InlineData(SchemaVersionConstants.Max, SchemaVersionConstants.Max)]
    [InlineData((int)SchemaVersion.V1, (int)SchemaVersion.V1)]
    public async void GivenSchemaWithAnyVersion_WhenApplyingSameOrOlderCompatibleVersion_ThenReturnUnnecessary(int originalVersion, int lowerVersionNumber)
    {
        //Arrange
        _schemaManagerDataStore.GetCurrentSchemaVersionAsync(_cancellationToken).Returns(originalVersion);
        _schemaDataStore.GetLatestCompatibleVersionsAsync(_cancellationToken).Returns(_fullCompatibility);

        //Act
        ApplyCommandResult result = await _dicomSqlSchemaManager.ApplySchema(_connectionString, lowerVersionNumber, _cancellationToken);

        //Assert
        Assert.Equal(ApplyCommandResult.Unnecessary, result);
    }

    [Theory(Skip = "Currently not implemented")]
    [InlineData((int)SchemaVersion.V1, (int)SchemaVersion.V12, (int)SchemaVersion.V10)]
    public async void GivenSchemaWithAnyVersion_WhenApplyingIncompatibleVersion_ThenReturnIncompatible(int originalVersion, int versionToApply, int latestCompatibleVersion)
    {
        //Arrange
        _schemaManagerDataStore.GetCurrentSchemaVersionAsync(_cancellationToken).Returns(originalVersion);
        var reducedCompatibility = new CompatibleVersions(originalVersion, latestCompatibleVersion);
        _schemaDataStore.GetLatestCompatibleVersionsAsync(_cancellationToken).Returns(reducedCompatibility);

        //Act
        ApplyCommandResult result = await _dicomSqlSchemaManager.ApplySchema(_connectionString, versionToApply, _cancellationToken);

        //Assert
        Assert.Equal(ApplyCommandResult.Incompatible, result);
    }
}
