// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Api.Features.ModelBinders;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Models.Binding;

public class MandatoryTimeZoneBinderTests
{
    private const string ModelName = "Timestamp";

    private readonly MandatoryTimeZoneBinder _binder = new MandatoryTimeZoneBinder();
    private readonly DefaultModelBindingContext _bindingContext = new DefaultModelBindingContext();
    private readonly IValueProvider _valueProvider = Substitute.For<IValueProvider>();

    public MandatoryTimeZoneBinderTests()
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
    [InlineData("2023-04-26T11:23:40.902519-07:0")]
    [InlineData("2023-04-26T11:23:40.90251-07")]
    [InlineData("2023-04-26T11:23:40.9025+7:00")]
    [InlineData("2023-04-26T11:23:40.902+7:0")]
    [InlineData("2023-04-26T11:23:40.90+7")]
    [InlineData("2023-04-26T11:23:40.9Z")]
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
