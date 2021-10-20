// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace Microsoft.Health.Dicom.Api.Models
{
    public class UpdateExtendedQueryTagOptionsTests
    {
        [Fact]
        public void GivenNoExtensionData_WhenValidate_ShouldReturnEmpty()
        {
            var options = new UpdateExtendedQueryTagOptions();
            Assert.Empty(options.Validate(null));
        }

        [Fact]
        public void GivenExtensionDatas_WhenValidate_ShouldReturnMultipleResults()
        {
            var options = new UpdateExtendedQueryTagOptions();
            string key1 = "key1";
            string key2 = "key2";
            var data = new Dictionary<string, JsonElement>();
            data.Add(key1, default);
            data.Add(key2, default);
            options.ExtensionData = data;
            var result = options.Validate(null).ToArray();
            Assert.Equal(data.Count, result.Length);
            string[] keys = { key1, key2 };
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Single(result[i].MemberNames);
                Assert.Equal(keys[i], result[i].MemberNames.First());
                Assert.Equal($"The field is not supported: \"{keys[i]}\".", result[i].ErrorMessage);
            }
        }

    }
}
