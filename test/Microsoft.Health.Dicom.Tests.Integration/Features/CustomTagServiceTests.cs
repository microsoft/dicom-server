// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.Core.Messages.CustomTag;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Microsoft.Health.Dicom.Tests.Integration.Persistence;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Features
{
    public class CustomTagServiceTests : IClassFixture<CustomTagServiceTestsFixture>
    {
        private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;

        private readonly CustomTagServiceTestsFixture _customTagServiceTestsFixture;

        public CustomTagServiceTests(CustomTagServiceTestsFixture customTagServiceTestsFixture)
        {
            EnsureArg.IsNotNull(customTagServiceTestsFixture, nameof(customTagServiceTestsFixture));
            _customTagServiceTestsFixture = customTagServiceTestsFixture;
        }

        [Fact(Skip = "Feature not ready")]
        public async Task GivenValidCustomTags_WhenAddCustomTag_ThenTagShouldBeAdded()
        {
            DicomTag tag1 = DicomTag.DeviceSerialNumber;
            DicomTag tag2 = DicomTag.ApprovalStatusDateTime;
            CustomTagEntry customTagEntry1 = tag1.BuildCustomTagEntry();
            CustomTagEntry customTagEntry2 = tag1.BuildCustomTagEntry();
            AddCustomTagResponse response = await _customTagServiceTestsFixture.AddCustomTagService.AddCustomTagAsync(new CustomTagEntry[] { customTagEntry1, customTagEntry2 }, DefaultCancellationToken);

            try
            {
                // Able to retrieve the tag
                await VerifyTagIsAdded(customTagEntry1);
                await VerifyTagIsAdded(customTagEntry2);
            }
            finally
            {
                // Delete custom tag
                await _customTagServiceTestsFixture.DeleteCustomTagService.DeleteCustomTagAsync(tag1.GetPath(), DefaultCancellationToken);
                await _customTagServiceTestsFixture.DeleteCustomTagService.DeleteCustomTagAsync(tag2.GetPath(), DefaultCancellationToken);
            }
        }

        [Fact(Skip = "Feature not ready")]
        public async Task GivenExistingCustomTag_WhenAddCustomTag_ThenShouldThrowException()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            CustomTagEntry customTagEntry = tag.BuildCustomTagEntry();
            AddCustomTagResponse response = await _customTagServiceTestsFixture.AddCustomTagService.AddCustomTagAsync(new CustomTagEntry[] { customTagEntry }, DefaultCancellationToken);
            try
            {
                await Assert.ThrowsAsync<CustomTagsAlreadyExistsException>(() => _customTagServiceTestsFixture.AddCustomTagService.AddCustomTagAsync(new CustomTagEntry[] { customTagEntry }, DefaultCancellationToken));
            }
            finally
            {
                await _customTagServiceTestsFixture.DeleteCustomTagService.DeleteCustomTagAsync(tag.GetPath(), DefaultCancellationToken);
            }
        }

        [Fact(Skip = "Feature not ready")]
        public async Task GivenExistingCustomTag_WhenDeleteCustomTag_ThenTagShouldBeRemoved()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            CustomTagEntry customTagEntry = tag.BuildCustomTagEntry();
            AddCustomTagResponse response = await _customTagServiceTestsFixture.AddCustomTagService.AddCustomTagAsync(new CustomTagEntry[] { customTagEntry }, DefaultCancellationToken);
            await _customTagServiceTestsFixture.DeleteCustomTagService.DeleteCustomTagAsync(customTagEntry.Path, DefaultCancellationToken);
            await VerifyTagNotExist(customTagEntry.Path);
        }

        [Fact(Skip = "Feature not ready")]
        public async Task GivenNonExistingCustomTag_WhenDeleteCustomTag_ThenShouldThrowException()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            CustomTagEntry customTagEntry = tag.BuildCustomTagEntry();
            await _customTagServiceTestsFixture.DeleteCustomTagService.DeleteCustomTagAsync(customTagEntry.Path, DefaultCancellationToken);
            await VerifyTagNotExist(customTagEntry.Path);
        }

        private async Task VerifyTagIsAdded(CustomTagEntry customTagEntry)
        {
            GetCustomTagResponse response = await _customTagServiceTestsFixture.GetCustomTagsService.GetCustomTagAsync(customTagEntry.Path, DefaultCancellationToken);
            Assert.Equal(customTagEntry.Path, response.CustomTag.Path);
            Assert.Equal(customTagEntry.VR, response.CustomTag.VR);
            Assert.Equal(customTagEntry.Level, response.CustomTag.Level);
            Assert.Equal(CustomTagStatus.Added, response.CustomTag.Status);
        }

        private async Task VerifyTagNotExist(string tagPath)
        {
            GetAllCustomTagsResponse response = await _customTagServiceTestsFixture.GetCustomTagsService.GetAllCustomTagsAsync(DefaultCancellationToken);
            Assert.DoesNotContain(response.CustomTags, item => item.Path.Equals(tagPath));
        }
    }
}
