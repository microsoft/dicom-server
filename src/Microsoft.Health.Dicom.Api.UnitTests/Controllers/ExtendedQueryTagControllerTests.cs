// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Dicom;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Api.Controllers;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Extensions
{
    public class ExtendedQueryTagControllerTests
    {

        [Fact]
        public async Task GivenFeatureIsDisabled_WhenCallingApi_ThenShouldThrowException()
        {
            IMediator _mediator = Substitute.For<IMediator>();
            var featureConfig = Options.Create(new Core.Configs.FeatureConfiguration() { EnableExtendedQueryTags = false });
            ExtendedQueryTagController controller = new ExtendedQueryTagController(_mediator, NullLogger<ExtendedQueryTagController>.Instance, featureConfig);
            await Assert.ThrowsAsync<ExtendedQueryTagFeatureDisabledException>(() => controller.GetTagAsync(DicomTag.PageNumberVector.GetPath()));
            await Assert.ThrowsAsync<ExtendedQueryTagFeatureDisabledException>(() => controller.GetAllTagsAsync());
            await Assert.ThrowsAsync<ExtendedQueryTagFeatureDisabledException>(() => controller.PostAsync(Array.Empty<ExtendedQueryTagEntry>()));
            await Assert.ThrowsAsync<ExtendedQueryTagFeatureDisabledException>(() => controller.DeleteAsync(DicomTag.PageNumberVector.GetPath()));
        }
    }
}
