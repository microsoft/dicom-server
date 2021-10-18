// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Api.Features.ModelBinders;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Features.ModelBinders
{
    public class AggregateCsvModelBinderTests
    {
        [Fact]
        public async Task GivenNoValues_WhenBindingModel_ThenReturnNoValue()
        {
            ModelBindingContext context = Substitute.For<ModelBindingContext>();
            context.ModelName = "Example";
            context.ValueProvider.GetValue(context.ModelName).Returns(new ValueProviderResult(new StringValues()));

            IModelBinder binder = new AggregateCsvModelBinder();
            await binder.BindModelAsync(context);

            Assert.True(context.Result.IsModelSet);
            Assert.Empty(context.Result.Model as IEnumerable<string>);
        }

        [Theory]
        [InlineData("foo", "foo")]
        [InlineData("1,2", "1", "2")]
        [InlineData(" a  , b,c\t", "a", "b", "c")]
        public async Task GivenValues_WhenBindingModel_ThenSplitByComma(string input, params string[] expected)
        {
            ModelBindingContext context = Substitute.For<ModelBindingContext>();
            context.ModelName = "Example";
            context.ValueProvider.GetValue(context.ModelName).Returns(new ValueProviderResult(new StringValues(input)));

            IModelBinder binder = new AggregateCsvModelBinder();
            await binder.BindModelAsync(context);

            Assert.True(context.Result.IsModelSet);
            Assert.True((context.Result.Model as IEnumerable<string>).SequenceEqual(expected));
        }
    }
}
