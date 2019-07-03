// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Health.Dicom.Core.Features.Persistence.Exceptions;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Persistence.Exceptions
{
    public class DataStoreExceptionTests
    {
        [Fact]
        public void GivenDataStoreException_WhenNoStatusCodeProvided_DefaultsToInternalServerError()
        {
            var innerException = new Exception("foo");
            Assert.Equal((int)HttpStatusCode.InternalServerError, new DataStoreException((HttpStatusCode?)null, innerException).StatusCode);
            Assert.Equal((int)HttpStatusCode.InternalServerError, new DataStoreException((int?)null, innerException).StatusCode);
        }

        [Fact]
        public void GivenDataStoreException_WhenSerialized_IsDeserializedCorrectly()
        {
            var innerException = new Exception("foo");
            var exception = new DataStoreException(HttpStatusCode.NotFound, innerException);

            var buffer = new byte[4096];
            using (var memoryStream1 = new MemoryStream(buffer))
            using (var memoryStream2 = new MemoryStream(buffer))
            {
                var formatter = new BinaryFormatter();

                formatter.Serialize(memoryStream1, exception);
                var deserializedException = (DataStoreException)formatter.Deserialize(memoryStream2);

                Assert.Equal(exception.StatusCode, deserializedException.StatusCode);
                Assert.Equal(exception.InnerException.Message, deserializedException.InnerException.Message);
                Assert.Equal(exception.Message, deserializedException.Message);
            }
        }
    }
}
