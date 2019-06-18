// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net;
using System.Runtime.Serialization;

namespace Microsoft.Health.Dicom.Core.Features.Persistence.Exceptions
{
    [Serializable]
    public class IndexDataStoreException : Exception
    {
        public IndexDataStoreException(HttpStatusCode httpStatusCode)
        {
            StatusCode = httpStatusCode;
        }

        public IndexDataStoreException(HttpStatusCode? httpStatusCode, Exception innerException)
            : base(innerException.Message, innerException)
        {
            StatusCode = httpStatusCode ?? HttpStatusCode.InternalServerError;
        }

        public IndexDataStoreException()
        {
        }

        public IndexDataStoreException(string message)
            : base(message)
        {
        }

        public IndexDataStoreException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected IndexDataStoreException(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            throw new NotImplementedException();
        }

        public HttpStatusCode StatusCode { get; }
    }
}
