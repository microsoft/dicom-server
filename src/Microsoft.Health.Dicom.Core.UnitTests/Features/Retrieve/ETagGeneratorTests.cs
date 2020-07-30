// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Retrieve
{
    public class ETagGeneratorTests
    {
        private readonly IETagGenerator _eTagGenerator;

        private readonly string _studyInstanceUid = TestUidGenerator.Generate();
        private readonly string _seriesInstanceUid = TestUidGenerator.Generate();
        private readonly string _sopInstanceUid = TestUidGenerator.Generate();

        public ETagGeneratorTests()
        {
            _eTagGenerator = new ETagGenerator();
        }

        [Fact]
        public void GivenETagGenerationRequestForStudy_ExpectedETagIsReturned()
        {
            List<VersionedInstanceIdentifier> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Study);
            string eTag = _eTagGenerator.GetETag(ResourceType.Study, versionedInstanceIdentifiers);
            string expectedETag = GetExpectedETag(ResourceType.Study, versionedInstanceIdentifiers);

            Assert.Equal(expectedETag, eTag);
        }

        [Fact]
        public void GivenETagGenerationRequestForSeries_ExpectedETagIsReturned()
        {
            List<VersionedInstanceIdentifier> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Series);
            string eTag = _eTagGenerator.GetETag(ResourceType.Series, versionedInstanceIdentifiers);
            string expectedETag = GetExpectedETag(ResourceType.Series, versionedInstanceIdentifiers);

            Assert.Equal(expectedETag, eTag);
        }

        [Fact]
        public void GivenETagGenerationRequestForInstance_ExpectedETagIsReturned()
        {
            List<VersionedInstanceIdentifier> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Instance);
            string eTag = _eTagGenerator.GetETag(ResourceType.Instance, versionedInstanceIdentifiers);
            string expectedETag = GetExpectedETag(ResourceType.Instance, versionedInstanceIdentifiers);

            Assert.Equal(expectedETag, eTag);
        }

        private string GetExpectedETag(ResourceType resourceType, List<VersionedInstanceIdentifier> versionedInstanceIdentifiers)
        {
            string eTag = string.Empty;

            if (versionedInstanceIdentifiers != null && versionedInstanceIdentifiers.Count > 0)
            {
                long maxWatermark = versionedInstanceIdentifiers.Max(vii => vii.Version);

                switch (resourceType)
                {
                    case ResourceType.Study:
                    case ResourceType.Series:
                        int countInstances = versionedInstanceIdentifiers.Count;
                        eTag = $"{maxWatermark}-{countInstances}";
                        break;
                    case ResourceType.Instance:
                        eTag = maxWatermark.ToString();
                        break;
                    default:
                        break;
                }
            }

            return eTag;
        }

        private List<VersionedInstanceIdentifier> SetupInstanceIdentifiersList(ResourceType resourceType)
        {
            List<VersionedInstanceIdentifier> dicomInstanceIdentifiersList = new List<VersionedInstanceIdentifier>();

            switch (resourceType)
            {
                case ResourceType.Study:
                    dicomInstanceIdentifiersList.Add(new VersionedInstanceIdentifier(_studyInstanceUid, TestUidGenerator.Generate(), TestUidGenerator.Generate(), version: 0));
                    dicomInstanceIdentifiersList.Add(new VersionedInstanceIdentifier(_studyInstanceUid, TestUidGenerator.Generate(), TestUidGenerator.Generate(), version: 1));
                    break;
                case ResourceType.Series:
                    dicomInstanceIdentifiersList.Add(new VersionedInstanceIdentifier(_studyInstanceUid, _seriesInstanceUid, TestUidGenerator.Generate(), version: 0));
                    dicomInstanceIdentifiersList.Add(new VersionedInstanceIdentifier(_studyInstanceUid, _seriesInstanceUid, TestUidGenerator.Generate(), version: 1));
                    break;
                case ResourceType.Instance:
                    dicomInstanceIdentifiersList.Add(new VersionedInstanceIdentifier(_studyInstanceUid, _seriesInstanceUid, _sopInstanceUid, version: 0));
                    break;
            }

            return dicomInstanceIdentifiersList;
        }
    }
}
