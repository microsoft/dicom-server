// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Dicom.Core.Models.Update;
using Microsoft.Health.Operations;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Store;
public class UpdateInstanceServiceTests
{
    private readonly IUpdateInstanceService _updateInstanceService;
    private readonly IGuidFactory _guidFactory;
    private readonly IDicomOperationsClient _client;
    private readonly IUrlResolver _urlResolver;

    public UpdateInstanceServiceTests()
    {
        _guidFactory = Substitute.For<IGuidFactory>();
        _client = Substitute.For<IDicomOperationsClient>();
        _urlResolver = Substitute.For<IUrlResolver>();
        _updateInstanceService = new UpdateInstanceService(_guidFactory, _client, _urlResolver);
    }

    [Fact]
    public async Task WhenExistingOperationQueued_ThenExistingUpdateOperationExceptionShouldBeThrown()
    {
        UpdateSpecification updateSpec = new UpdateSpecification();
        Guid id = Guid.NewGuid();
        var expected = new OperationReference(id, new Uri("https://dicom.contoso.io/unit/test/Operations/" + id, UriKind.Absolute));

        _client.FindOperationsAsync(Arg.Is(GetOperationPredicate()), CancellationToken.None)
            .Returns(new OperationReference[] { expected }.ToAsyncEnumerable());
        await Assert.ThrowsAsync<ExistingUpdateOperationException>(() =>
            _updateInstanceService.UpdateInstanceAsync(updateSpec, CancellationToken.None));
    }

    [Fact]
    public async Task GivenValidInput_WhenNoExistingOperationQueued_Then()
    {
        UpdateSpecification updateSpec = new UpdateSpecification();
        _urlResolver.ResolveOperationStatusUri(Arg.Any<Guid>()).Returns(new Uri("/operation", UriKind.Relative));
        _client.FindOperationsAsync(Arg.Is(GetOperationPredicate()), CancellationToken.None)
            .Returns(AsyncEnumerable.Empty<OperationReference>());
        var response = await _updateInstanceService.UpdateInstanceAsync(updateSpec, CancellationToken.None);

        Assert.NotNull(response);
    }

    private static Expression<Predicate<OperationQueryCondition<DicomOperation>>> GetOperationPredicate()
    => (OperationQueryCondition<DicomOperation> x) =>
        x.CreatedTimeFrom == DateTime.MinValue &&
        x.CreatedTimeTo == DateTime.MaxValue &&
        x.Operations.Single() == DicomOperation.Update &&
        x.Statuses.SequenceEqual(new OperationStatus[] { OperationStatus.NotStarted, OperationStatus.Running });
}
