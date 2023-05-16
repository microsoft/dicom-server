// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Microsoft.Health.Operations;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.ExtendedQueryTag;

public class AddExtendedQueryTagServiceTests
{
    private readonly IExtendedQueryTagStore _extendedQueryTagStore;
    private readonly IGuidFactory _guidFactory;
    private readonly IDicomOperationsClient _client;
    private readonly IExtendedQueryTagEntryValidator _extendedQueryTagEntryValidator;
    private readonly AddExtendedQueryTagService _extendedQueryTagService;
    private readonly CancellationTokenSource _tokenSource;

    public AddExtendedQueryTagServiceTests()
    {
        _extendedQueryTagStore = Substitute.For<IExtendedQueryTagStore>();
        _guidFactory = Substitute.For<IGuidFactory>();
        _client = Substitute.For<IDicomOperationsClient>();
        _extendedQueryTagEntryValidator = Substitute.For<IExtendedQueryTagEntryValidator>();
        _extendedQueryTagService = new AddExtendedQueryTagService(
            _extendedQueryTagStore,
            _guidFactory,
            _client,
            _extendedQueryTagEntryValidator,
            Options.Create(new ExtendedQueryTagConfiguration { MaxAllowedCount = 128 }));

        _tokenSource = new CancellationTokenSource();
    }

    [Fact]
    public async Task GivenExistingOperation_WhenAddingExtendedQueryTag_ThenThrow()
    {
        DicomTag tag = DicomTag.DeviceSerialNumber;
        AddExtendedQueryTagEntry entry = tag.BuildAddExtendedQueryTagEntry();

        Guid id = Guid.NewGuid();
        var expected = new OperationReference(id, new Uri("https://dicom.contoso.io/unit/test/Operations/" + id, UriKind.Absolute));

        var input = new AddExtendedQueryTagEntry[] { entry };
        _client
            .FindOperationsAsync(Arg.Is(GetOperationPredicate()), _tokenSource.Token)
            .Returns(new OperationReference[] { expected }.ToAsyncEnumerable());

        await Assert.ThrowsAsync<ExistingOperationException>(
            () => _extendedQueryTagService.AddExtendedQueryTagsAsync(input, _tokenSource.Token));

        _client
            .Received(1)
            .FindOperationsAsync(Arg.Is(GetOperationPredicate()), _tokenSource.Token);
        _extendedQueryTagEntryValidator.DidNotReceiveWithAnyArgs().ValidateExtendedQueryTags(default);
        await _client.DidNotReceiveWithAnyArgs().StartReindexingInstancesAsync(default, default);
    }

    [Fact]
    public async Task GivenInvalidInput_WhenAddingExtendedQueryTag_ThenStopAfterValidation()
    {
        DicomTag tag = DicomTag.DeviceSerialNumber;
        AddExtendedQueryTagEntry entry = tag.BuildAddExtendedQueryTagEntry();
        var exception = new ExtendedQueryTagEntryValidationException(string.Empty);

        _client
            .FindOperationsAsync(Arg.Is(GetOperationPredicate()), _tokenSource.Token)
            .Returns(AsyncEnumerable.Empty<OperationReference>());

        var input = new AddExtendedQueryTagEntry[] { entry };
        _extendedQueryTagEntryValidator.WhenForAnyArgs(v => v.ValidateExtendedQueryTags(input)).Throw(exception);

        await Assert.ThrowsAsync<ExtendedQueryTagEntryValidationException>(
            () => _extendedQueryTagService.AddExtendedQueryTagsAsync(input, _tokenSource.Token));

        _client
            .Received(1)
            .FindOperationsAsync(Arg.Is(GetOperationPredicate()), _tokenSource.Token);
        _extendedQueryTagEntryValidator.Received(1).ValidateExtendedQueryTags(input);
        await _client.DidNotReceiveWithAnyArgs().StartReindexingInstancesAsync(default, default);
    }

    [Fact]
    public async Task GivenValidInput_WhenAddingExtendedQueryTag_ThenShouldSucceed()
    {
        DicomTag tag = DicomTag.DeviceSerialNumber;
        AddExtendedQueryTagEntry entry = tag.BuildAddExtendedQueryTagEntry();
        ExtendedQueryTagStoreEntry storeEntry = tag.BuildExtendedQueryTagStoreEntry();

        var input = new AddExtendedQueryTagEntry[] { entry };
        var operationId = Guid.NewGuid();
        var expected = new OperationReference(
            operationId,
            new Uri("https://dicom.contoso.io/unit/test/Operations/" + operationId, UriKind.Absolute));

        _client
            .FindOperationsAsync(Arg.Is(GetOperationPredicate()), _tokenSource.Token)
            .Returns(AsyncEnumerable.Empty<OperationReference>());
        _extendedQueryTagStore
            .AddExtendedQueryTagsAsync(
                Arg.Is<IReadOnlyList<AddExtendedQueryTagEntry>>(x => x.Single().Path == entry.Path),
                128,
                false,
                _tokenSource.Token)
            .Returns(new List<ExtendedQueryTagStoreEntry> { storeEntry });
        _guidFactory.Create().Returns(operationId);
        _client
            .StartReindexingInstancesAsync(
                operationId,
                Arg.Is<IReadOnlyList<int>>(x => x.Single() == storeEntry.Key),
                _tokenSource.Token)
            .Returns(expected);
        _extendedQueryTagStore
            .AssignReindexingOperationAsync(
                Arg.Is<IReadOnlyCollection<int>>(x => x.Single() == storeEntry.Key),
                operationId,
                true,
                _tokenSource.Token)
            .Returns(new List<ExtendedQueryTagStoreEntry> { storeEntry });

        OperationReference actual = await _extendedQueryTagService.AddExtendedQueryTagsAsync(input, _tokenSource.Token);
        Assert.Same(expected, actual);

        _client
            .Received(1)
            .FindOperationsAsync(Arg.Is(GetOperationPredicate()), _tokenSource.Token);
        _extendedQueryTagEntryValidator.Received(1).ValidateExtendedQueryTags(input);
        await _extendedQueryTagStore
            .Received(1)
            .AddExtendedQueryTagsAsync(
                Arg.Is<IReadOnlyList<AddExtendedQueryTagEntry>>(x => x.Single().Path == entry.Path),
                128,
                false,
                _tokenSource.Token);
        _guidFactory.Received(1).Create();
        await _client
            .Received(1)
            .StartReindexingInstancesAsync(
                operationId,
                Arg.Is<IReadOnlyList<int>>(x => x.Single() == storeEntry.Key),
                _tokenSource.Token);
        await _extendedQueryTagStore
            .Received(1)
            .AssignReindexingOperationAsync(
                Arg.Is<IReadOnlyCollection<int>>(x => x.Single() == storeEntry.Key),
                operationId,
                true,
                _tokenSource.Token);
    }

    private static Expression<Predicate<OperationQueryCondition<DicomOperation>>> GetOperationPredicate()
        => (OperationQueryCondition<DicomOperation> x) =>
            x.CreatedTimeFrom == DateTime.MinValue &&
            x.CreatedTimeTo == DateTime.MaxValue &&
            x.Operations.Single() == DicomOperation.Reindex &&
            x.Statuses.SequenceEqual(new OperationStatus[] { OperationStatus.NotStarted, OperationStatus.Running });
}
