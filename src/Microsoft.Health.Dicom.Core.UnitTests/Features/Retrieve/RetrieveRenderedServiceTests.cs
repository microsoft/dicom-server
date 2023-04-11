// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.IO;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Retrieve;
public class RetrieveRenderedServiceTests
{
    private readonly RetrieveRenderedService _retrieveRenderedService;
    private readonly IInstanceStore _instanceStore;
    private readonly IFileStore _fileStore;
    private readonly IDicomRequestContextAccessor _dicomRequestContextAccessor;
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
    private readonly ILogger<RetrieveRenderedService> _logger;

    private readonly string _studyInstanceUid = TestUidGenerator.Generate();
    private readonly string _firstSeriesInstanceUid = TestUidGenerator.Generate();
    private readonly string _secondSeriesInstanceUid = TestUidGenerator.Generate();
    private readonly string _sopInstanceUid = TestUidGenerator.Generate();
    private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;


    public RetrieveRenderedServiceTests()
    {
        _instanceStore = Substitute.For<IInstanceStore>();
        _fileStore = Substitute.For<IFileStore>();
        _logger = NullLogger<RetrieveRenderedService>.Instance;
        _dicomRequestContextAccessor = Substitute.For<IDicomRequestContextAccessor>();
        _dicomRequestContextAccessor.RequestContext.DataPartitionEntry = PartitionEntry.Default;
        var retrieveConfigurationSnapshot = Substitute.For<IOptionsSnapshot<RetrieveConfiguration>>();
        retrieveConfigurationSnapshot.Value.Returns(new RetrieveConfiguration());
        _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
        _retrieveRenderedService = new RetrieveRenderedService(
            _instanceStore,
            _fileStore,
            _dicomRequestContextAccessor,
            retrieveConfigurationSnapshot,
            _recyclableMemoryStreamManager,
            _logger
            );
    }

    [Fact]
    public async Task GivenNoStoredInstances_RenderForInstance_ThenNotFoundIsThrown()
    {
        _instanceStore.GetInstanceIdentifiersInStudyAsync(DefaultPartition.Key, _studyInstanceUid).Returns(new List<VersionedInstanceIdentifier>());

        await Assert.ThrowsAsync<InstanceNotFoundException>(() => _retrieveRenderedService.RetrieveRenderedImageAsync(
            new RetrieveRenderedRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, Core.Messages.ResourceType.Instance, 0, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetStudy() }),
            DefaultCancellationToken));
    }


}
