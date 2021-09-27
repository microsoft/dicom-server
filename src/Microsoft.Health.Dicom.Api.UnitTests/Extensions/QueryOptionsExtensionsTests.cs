// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Api.Extensions;
using Microsoft.Health.Dicom.Api.Models;
using Microsoft.Health.Dicom.Core.Features.Query;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Extensions
{
    public class QueryOptionsExtensionsTests
    {
        [Fact]
        public void GivenDuplicateValues_WhenCreateParameters_ThrowQueryParseException()
        {
            var qsp = new List<KeyValuePair<string, StringValues>>
            {
                KeyValuePair.Create("PatientName", new StringValues(new string[] { "Foo", "Bar", "Baz" })),
            };

            Assert.Throws<QueryParseException>(() => new QueryOptions().ToParameters(qsp, QueryResource.AllStudies));
        }

        [Fact]
        public void GivenQueryString_WhenCreateParameters_IgnoreKnownParameters()
        {
            var qsp = new List<KeyValuePair<string, StringValues>>
            {
                KeyValuePair.Create("PatientName", new StringValues("Joe")),
                KeyValuePair.Create("offset", new StringValues("10")),
                KeyValuePair.Create("PatientBirthDate", new StringValues("18000101-19010101")),
                KeyValuePair.Create("limit", new StringValues("50")),
                KeyValuePair.Create("fuzzymatching", new StringValues("true")),
                KeyValuePair.Create("ReferringPhysicianName", new StringValues("dr")),
                KeyValuePair.Create("IncludeField", new StringValues("ManufacturerModelName")),
            };

            IReadOnlyDictionary<string, string> actual = new QueryOptions().ToParameters(qsp, QueryResource.AllStudies).Filters;

            Assert.Equal(3, actual.Count);
            Assert.Equal("Joe", actual["PatientName"]);
            Assert.Equal("18000101-19010101", actual["PatientBirthDate"]);
            Assert.Equal("dr", actual["ReferringPhysicianName"]);
        }

        [Fact]
        public void GivenInput_WhenCreatingParameters_ThenAssignAppropriateValues()
        {
            var options = new QueryOptions
            {
                FuzzyMatching = true,
                IncludeField = new List<string> { "Modality" },
                Limit = 25,
                Offset = 100,
            };

            string study = "foo";
            string series = "bar";

            var qsp = new List<KeyValuePair<string, StringValues>>
            {
                KeyValuePair.Create("  PatientName ", new StringValues("   Will\t")),
                KeyValuePair.Create("ReferringPhysicianName\r\n", new StringValues("dr")),
            };

            QueryParameters actual = options.ToParameters(qsp, QueryResource.StudySeriesInstances, study, series);
            Assert.Equal(2, actual.Filters.Count);
            Assert.Equal("Will", actual.Filters["PatientName"]);
            Assert.Equal("dr", actual.Filters["ReferringPhysicianName"]);
            Assert.Equal(options.FuzzyMatching, actual.FuzzyMatching);
            Assert.Same(options.IncludeField, actual.IncludeField);
            Assert.Equal(options.Limit, actual.Limit);
            Assert.Equal(options.Offset, actual.Offset);
            Assert.Equal(QueryResource.StudySeriesInstances, actual.QueryResourceType);
            Assert.Equal(series, actual.SeriesInstanceUid);
            Assert.Equal(study, actual.StudyInstanceUid);
        }
    }
}
