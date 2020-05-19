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
    public class TransferSyntaxModelBinderTests
    {
        [Theory]
        [InlineData(new[] { "application/dicom; traNSFer-sYNTAx=*" }, "*")]
        [InlineData(new[] { "application/dicom", "traNSFer-sYNTAx=*" }, "*")]
        [InlineData(new[] { "application/dicom, traNSFer-sYNTAx=  LittleEndian " }, "LittleEndian")]
        [InlineData(new[] { "application/dicom", "   traNSFer-sYNTAx=  *" }, "*")]
        public async Task GivenStringContent_WhenBindingTransferSyntax_ModelIsSetAndExpectedResultIsParsed(string[] contextValue, string expectedResult)
        {
            ModelBindingContext bindingContext = Substitute.For<ModelBindingContext>();
            bindingContext.HttpContext.Request.Headers["Accept"].Returns(new StringValues(contextValue));

            IModelBinder modelBinder = new TransferSyntaxModelBinder();
            await modelBinder.BindModelAsync(bindingContext);

            Assert.True(bindingContext.Result.IsModelSet);

            var actualResult = bindingContext.Result.Model as string;
            Assert.Equal(expectedResult, actualResult);
        }

        [Theory]
        [InlineData("helloworld")]
        [InlineData("application/dicom,  traNSFer  -sYNTAx=  *")]
        [InlineData("application/dicom", "   traNSFer  -sYNTAx=  *")]
        public async Task GivenInvalidStringContent_WhenBindingTransferSyntax_ModelIsNotSet(params string[] contextValue)
        {
            ModelBindingContext bindingContext = Substitute.For<ModelBindingContext>();
            bindingContext.HttpContext.Request.Headers["Accept"].Returns(new StringValues(contextValue));

            IModelBinder modelBinder = new TransferSyntaxModelBinder();
            await modelBinder.BindModelAsync(bindingContext);

            Assert.Equal(ModelBindingResult.Success(null), bindingContext.Result);
        }
    }
}
