// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Api.Models.Binding;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Models.Binding;

public class Iso8601BinderTests
{
    private const string ModelName = "Timestamp";

    private readonly Iso8601Binder _binder = new Iso8601Binder();
    private readonly DefaultModelBindingContext _bindingContext = new DefaultModelBindingContext();
    private readonly IValueProvider _valueProvider = Substitute.For<IValueProvider>();

    public Iso8601BinderTests()
    {
        _bindingContext.ModelState = new ModelStateDictionary();
        _bindingContext.ValueProvider = _valueProvider;
    }

    [Fact]
    public async Task GivenNoInput_WhenBinding_ThenSkip()
    {
        _bindingContext.ModelName = ModelName;
        _valueProvider.GetValue(ModelName).Returns(ValueProviderResult.None);

        await _binder.BindModelAsync(_bindingContext);
        Assert.False(_bindingContext.Result.IsModelSet);
        Assert.True(_bindingContext.ModelState.IsValid);
        Assert.Null(_bindingContext.Result.Model);

        _valueProvider.Received(1).GetValue(ModelName);
    }

    [Theory]
    [InlineData("2023-04-26T11:23:40.9025193-07:00")]
    [InlineData("2023-04-26T11:23:40.902519-07:00")]
    [InlineData("2023-04-26T11:23:40.90251-07:00")]
    [InlineData("2023-04-26T11:23:40.9025-07:00")]
    [InlineData("2023-04-26T11:23:40.902-07:00")]
    [InlineData("2023-04-26T11:23:40.90-07:00")]
    [InlineData("2023-04-26T11:23:40.9-07:00")]
    [InlineData("2023-04-26T11:23:40-07:00")]
    [InlineData("2023-04-26T11:23:40.9025193-07")]
    [InlineData("2023-04-26T11:23:40.902519-07")]
    [InlineData("2023-04-26T11:23:40.90251-07")]
    [InlineData("2023-04-26T11:23:40.9025-07")]
    [InlineData("2023-04-26T11:23:40.902-07")]
    [InlineData("2023-04-26T11:23:40.90-07")]
    [InlineData("2023-04-26T11:23:40.9-07")]
    [InlineData("2023-04-26T11:23:40-07")]
    [InlineData("2023-04-26T11:23:40.9025193Z")]
    [InlineData("2023-04-26T11:23:40.902519Z")]
    [InlineData("2023-04-26T11:23:40.90251Z")]
    [InlineData("2023-04-26T11:23:40.9025Z")]
    [InlineData("2023-04-26T11:23:40.902Z")]
    [InlineData("2023-04-26T11:23:40.90Z")]
    [InlineData("2023-04-26T11:23:40.9Z")]
    [InlineData("2023-04-26T11:23:40Z")]
    public async Task GivenValidString_WhenBinding_ThenSucceed(string input)
    {
        _bindingContext.ModelName = ModelName;
        _valueProvider.GetValue(ModelName).Returns(new ValueProviderResult(new StringValues(input)));

        await _binder.BindModelAsync(_bindingContext);
        Assert.True(_bindingContext.Result.IsModelSet);
        Assert.Equal(0, _bindingContext.ModelState.ErrorCount);
        Assert.Equal(DateTimeOffset.Parse(input, CultureInfo.InvariantCulture), _bindingContext.Result.Model);

        _valueProvider.Received(1).GetValue(ModelName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("foo")]
    [InlineData("4/26/2023 5:38:06 PM")]
    [InlineData("2023-04-26T11:23:40.9025193")]
    [InlineData("2023-04-26T11:23:40.902519")]
    [InlineData("2023-04-26T11:23:40.90251")]
    [InlineData("2023-04-26T11:23:40.9025")]
    [InlineData("2023-04-26T11:23:40.902")]
    [InlineData("2023-04-26T11:23:40.90")]
    [InlineData("2023-04-26T11:23:40.9")]
    [InlineData("2023-04-26T11:23:40")]
    public async Task GivenInvalidString_WhenBinding_ThenFail(string input)
    {
        _bindingContext.ModelName = ModelName;
        _valueProvider.GetValue(ModelName).Returns(new ValueProviderResult(new StringValues(input)));

        await _binder.BindModelAsync(_bindingContext);
        Assert.False(_bindingContext.Result.IsModelSet);
        Assert.Equal(1, _bindingContext.ModelState.ErrorCount);

        _valueProvider.Received(1).GetValue(ModelName);
    }
}
