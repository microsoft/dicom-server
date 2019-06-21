// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.CosmosDb.Features.Storage.Documents;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Health.Dicom.CosmosDb.UnitTests.Features.Storage.Documents
{
    public class AttributeValuesTests
    {
        [Fact]
        public void GivenACollectionOfDifferentTypes_WhenAddedToAttributeValues_AreStoredAndMinMaxDateTimeValuesCalculated()
        {
            const int numberOfItemsToAdd = 300;
            var minDateTime = new DateTime(2019, 6, 21);
            var maxDateTime = new DateTime(2020, 6, 21);
            Assert.True((maxDateTime - minDateTime).Days > numberOfItemsToAdd);

            var attributeValues = new AttributeValues();

            for (int i = 0; i < numberOfItemsToAdd; i++)
            {
                // Add DateTime, int, and string
                Assert.True(attributeValues.Add(minDateTime.AddDays(i)));
                Assert.True(attributeValues.Add(i));
                Assert.True(attributeValues.Add(new string('a', i)));
            }

            Assert.True(attributeValues.Add(maxDateTime));

            Assert.Equal(minDateTime, attributeValues.MinDateTimeValue);
            Assert.Equal(maxDateTime, attributeValues.MaxDateTimeValue);
        }

        [Fact]
        public void GivenExistingItem_WhenAddedToAttributeValues_IsNotAdded()
        {
            var attributeValues = new AttributeValues();

            Assert.True(attributeValues.Add(1));
            Assert.False(attributeValues.Add(1));

            Assert.True(attributeValues.Add("HelloWorld"));
            Assert.False(attributeValues.Add("HelloWorld"));

            Assert.Null(new AttributeValues().MinDateTimeValue);
            Assert.Null(new AttributeValues().MaxDateTimeValue);

            Assert.True(attributeValues.Add(new DateTime(2019, 6, 22)));
            Assert.False(attributeValues.Add(new DateTime(2019, 6, 22)));

            Assert.NotNull(attributeValues.MinDateTimeValue);
            Assert.NotNull(attributeValues.MaxDateTimeValue);

            Assert.False(attributeValues.Add(null));
        }

        [Fact]
        public void GivenAttributeValues_WhenCreatedWithInvalidParameters_ArgumentExceptionThrown()
        {
            Assert.Throws<ArgumentNullException>(() => new AttributeValues(null));
            Assert.NotNull(new AttributeValues().Values);
            Assert.Null(new AttributeValues().MinDateTimeValue);
            Assert.Null(new AttributeValues().MaxDateTimeValue);
        }

        [Fact]
        public void GivenAttributeValues_WhenSerialized_DeserializedCorrectly()
        {
            const int numberOfItemsToAdd = 300;
            var dateTime = new DateTime(2019, 6, 20);
            var attributeValues = new AttributeValues();

            for (int i = 0; i < numberOfItemsToAdd; i++)
            {
                // Add DateTime, int, and string
                Assert.True(attributeValues.Add(dateTime.AddDays(i)));
                Assert.True(attributeValues.Add(i));
                Assert.True(attributeValues.Add(new string('a', i)));
            }

            var json = JsonConvert.SerializeObject(attributeValues);
            AttributeValues deserialized = JsonConvert.DeserializeObject<AttributeValues>(json);

            Assert.Equal(attributeValues.MinDateTimeValue, deserialized.MinDateTimeValue);
            Assert.Equal(attributeValues.MaxDateTimeValue, deserialized.MaxDateTimeValue);
        }
    }
}
