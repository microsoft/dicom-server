// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Retrieve;

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
        List<InstanceMetadata> instanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Study);
        string eTag = _eTagGenerator.GetETag(ResourceType.Study, instanceIdentifiers);
        string expectedETag = GetExpectedETag(ResourceType.Study, instanceIdentifiers);

        Assert.Equal(expectedETag, eTag);
    }

    [Fact]
    public void GivenETagGenerationRequestForSeries_ExpectedETagIsReturned()
    {
        List<InstanceMetadata> instanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Series);
        string eTag = _eTagGenerator.GetETag(ResourceType.Series, instanceIdentifiers);
        string expectedETag = GetExpectedETag(ResourceType.Series, instanceIdentifiers);

        Assert.Equal(expectedETag, eTag);
    }

    [Fact]
    public void GivenETagGenerationRequestForInstance_ExpectedETagIsReturned()
    {
        List<InstanceMetadata> instanceIdentifiers = SetupInstanceIdentifiersList(ResourceType.Instance);
        string eTag = _eTagGenerator.GetETag(ResourceType.Instance, instanceIdentifiers);
        string expectedETag = GetExpectedETag(ResourceType.Instance, instanceIdentifiers);

        Assert.Equal(expectedETag, eTag);
    }

    private static string GetExpectedETag(ResourceType resourceType, List<InstanceMetadata> instanceIdentifiers)
    {
        string eTag = string.Empty;

        if (instanceIdentifiers != null && instanceIdentifiers.Count > 0)
        {
            long maxWatermark = instanceIdentifiers.Max(vii => vii.VersionedInstanceIdentifier.Version);

            switch (resourceType)
            {
                case ResourceType.Study:
                case ResourceType.Series:
                    int countInstances = instanceIdentifiers.Count;
                    eTag = $"{maxWatermark}-{countInstances}";
                    break;
                case ResourceType.Instance:
                    eTag = maxWatermark.ToString(CultureInfo.InvariantCulture);
                    break;
                default:
                    break;
            }
        }

        return eTag;
    }

    private List<InstanceMetadata> SetupInstanceIdentifiersList(ResourceType resourceType)
    {
        List<InstanceMetadata> dicomInstanceIdentifiersList = new List<InstanceMetadata>();
        switch (resourceType)
        {
            case ResourceType.Study:
                dicomInstanceIdentifiersList.Add(new InstanceMetadata(new VersionedInstanceIdentifier(_studyInstanceUid, TestUidGenerator.Generate(), TestUidGenerator.Generate(), version: 0), new InstanceProperties()));
                dicomInstanceIdentifiersList.Add(new InstanceMetadata(new VersionedInstanceIdentifier(_studyInstanceUid, TestUidGenerator.Generate(), TestUidGenerator.Generate(), version: 1), new InstanceProperties()));
                break;
            case ResourceType.Series:
                dicomInstanceIdentifiersList.Add(new InstanceMetadata(new VersionedInstanceIdentifier(_studyInstanceUid, _seriesInstanceUid, TestUidGenerator.Generate(), version: 0), new InstanceProperties()));
                dicomInstanceIdentifiersList.Add(new InstanceMetadata(new VersionedInstanceIdentifier(_studyInstanceUid, _seriesInstanceUid, TestUidGenerator.Generate(), version: 1), new InstanceProperties()));
                break;
            case ResourceType.Instance:
                dicomInstanceIdentifiersList.Add(new InstanceMetadata(new VersionedInstanceIdentifier(_studyInstanceUid, _seriesInstanceUid, _sopInstanceUid, version: 0), new InstanceProperties()));
                break;
        }

        return dicomInstanceIdentifiersList;
    }
}
