// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Operations.Indexing.Models;
using Microsoft.Health.Dicom.Operations.Serialization;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Health.Dicom.Operations.UnitTests.Serialization
{
    public class MessageSerializerSettingsFactoryTests
    {
        private readonly static JsonSerializerSettings JsonSerializerSettings = new MessageSerializerSettingsFactory().CreateJsonSerializerSettings();

        [Fact]
        public void GivenPreviouslySerializedMessage_WhenDeserializingWithNewSettings_ThenDeserializeSuccessfully()
        {
            var queryTags = new List<ExtendedQueryTagStoreEntry>
            {
                new ExtendedQueryTagStoreEntry(1, "01", "DT", "foo", QueryTagLevel.Instance, ExtendedQueryTagStatus.Adding, QueryStatus.Enabled, 0),
                new ExtendedQueryTagStoreEntry(2, "02", "DT", "bar", QueryTagLevel.Study, ExtendedQueryTagStatus.Adding, QueryStatus.Enabled, 0),
            };
            var range = new WatermarkRange(5, 10);
            const int threadCount = 7;

            var before = new ReindexBatchArguments(queryTags, range, threadCount);
            string json = JsonConvert.SerializeObject(
                before,
                new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Objects,
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                });

            ReindexBatchArguments actual = JsonConvert.DeserializeObject<ReindexBatchArguments>(json, JsonSerializerSettings);

            Assert.Equal(before.QueryTags, actual.QueryTags, new TagEntryComparer());
            Assert.Equal(before.ThreadCount, actual.ThreadCount);
            Assert.Equal(before.WatermarkRange, actual.WatermarkRange);
        }

        private sealed class TagEntryComparer : IEqualityComparer<ExtendedQueryTagStoreEntry>
        {
            public bool Equals(ExtendedQueryTagStoreEntry x, ExtendedQueryTagStoreEntry y)
                => x.ErrorCount == y.ErrorCount
                || x.Key == y.Key
                || x.Level == y.Level
                || x.Path == y.Path
                || x.PrivateCreator == y.PrivateCreator
                || x.QueryStatus == y.QueryStatus
                || x.Status == y.Status
                || x.VR == y.VR;

            public int GetHashCode(ExtendedQueryTagStoreEntry obj)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
