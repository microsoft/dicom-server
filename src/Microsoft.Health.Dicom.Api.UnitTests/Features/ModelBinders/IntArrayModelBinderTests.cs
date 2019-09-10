// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Api.Features.ModelBinders;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Features.ModelBinders
{
    public class IntArrayModelBinderTests
    {
        [Theory]
        [InlineData("", new int[0])]
        [InlineData(null, new int[0])]
        [InlineData("1, -234, 34", new int[] { 1, -234, 34 })]
        public async Task GivenStringContent_WhenBindingIntArrayData_ModelIsSetAndExpectedResultIsParsed(string contextValue, int[] expectedResult)
        {
            ModelBindingContext bindingContext = Substitute.For<ModelBindingContext>();
            bindingContext.ValueProvider.GetValue(bindingContext.ModelName).Returns(new ValueProviderResult(new StringValues(contextValue)));

            IModelBinder modelBinder = new IntArrayModelBinder();
            await modelBinder.BindModelAsync(bindingContext);

            Assert.True(bindingContext.Result.IsModelSet);

            var actualResult = bindingContext.Result.Model as int[];
            Assert.Equal(expectedResult.Length, actualResult.Length);

            for (var i = 0; i < expectedResult.Length; i++)
            {
                Assert.Equal(expectedResult[i], actualResult[i]);
            }
        }

        [Theory]
        [InlineData("1, 2, helloworld")]
        [InlineData("1, #5$, 3")]
        public async Task GivenInvalidStringContent_WhenBindingIntArrayData_ModelIsNotSet(string contextValue)
        {
            ModelBindingContext bindingContext = Substitute.For<ModelBindingContext>();
            bindingContext.ValueProvider.GetValue(bindingContext.ModelName).Returns(new ValueProviderResult(new StringValues(contextValue)));

            IModelBinder modelBinder = new IntArrayModelBinder();
            await modelBinder.BindModelAsync(bindingContext);

            Assert.Equal(bindingContext.Result, ModelBindingResult.Failed());
        }
    }
}
