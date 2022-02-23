// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Api.Controllers;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Tests.Common;
using NSubstitute;

namespace Microsoft.Health.Dicom.Api.UnitTests.Features.Filters
{
    public static class FilterTestsHelper
    {
        public static ChangeFeedController CreateMockChangeFeedController()
        {
            return Mock.TypeWithArguments<ChangeFeedController>(Options.Create(new FeatureConfiguration()));
        }

        public static DeleteController CreateMockDeleteController()
        {
            return Mock.TypeWithArguments<DeleteController>(Options.Create(new FeatureConfiguration()));
        }

        public static QueryController CreateMockQueryController()
        {
            return Mock.TypeWithArguments<QueryController>(Options.Create(new FeatureConfiguration()));
        }

        public static RetrieveController CreateMockRetrieveController()
        {
            var retrieveConfigSnapshot = Substitute.For<IOptionsSnapshot<RetrieveConfiguration>>();
            retrieveConfigSnapshot.Value.Returns(new RetrieveConfiguration());
            return Mock.TypeWithArguments<RetrieveController>(Options.Create(new FeatureConfiguration()), retrieveConfigSnapshot);
        }

        public static StoreController CreateMockStoreController()
        {
            return Mock.TypeWithArguments<StoreController>(Options.Create(new FeatureConfiguration()));
        }
    }
}
