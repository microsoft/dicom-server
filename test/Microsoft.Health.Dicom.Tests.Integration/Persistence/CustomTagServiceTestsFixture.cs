// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class CustomTagServiceTestsFixture : IAsyncLifetime
    {
        private readonly SqlDataStoreTestsFixture _sqlDataStoreTestsFixture;

        public CustomTagServiceTestsFixture()
        {
            _sqlDataStoreTestsFixture = new SqlDataStoreTestsFixture();
            AddCustomTagService = new AddCustomTagService(CustomTagStore, new CustomTagEntryValidator(new DicomTagParser()), NullLogger<AddCustomTagService>.Instance);
            GetCustomTagsService = new GetCustomTagsService(CustomTagStore, new DicomTagParser());
            DeleteCustomTagService = new DeleteCustomTagService(CustomTagStore, new DicomTagParser(), NullLogger<DeleteCustomTagService>.Instance);
        }

        public IAddCustomTagService AddCustomTagService { get; private set; }

        public IGetCustomTagsService GetCustomTagsService { get; private set; }

        public IDeleteCustomTagService DeleteCustomTagService { get; private set; }

        public ICustomTagStore CustomTagStore => _sqlDataStoreTestsFixture.CustomTagStore;

        public async Task InitializeAsync()
        {
            await _sqlDataStoreTestsFixture.InitializeAsync();
        }

        public async Task DisposeAsync()
        {
            await _sqlDataStoreTestsFixture.DisposeAsync();
        }
    }
}
