// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net;
using System.Runtime.Serialization;
using System.Security.Permissions;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    [Serializable]
    public class DicomDataStoreException : Exception
    {
        public DicomDataStoreException(HttpStatusCode httpStatusCode)
        {
            StatusCode = (int)httpStatusCode;
        }

        public DicomDataStoreException(HttpStatusCode? httpStatusCode, Exception innerException)
            : base(innerException.Message, innerException)
        {
            StatusCode = httpStatusCode.HasValue ? (int)httpStatusCode.Value : (int)HttpStatusCode.InternalServerError;
        }

        public DicomDataStoreException(int? statusCode, Exception innerException)
            : base(innerException.Message, innerException)
        {
            StatusCode = statusCode ?? (int)HttpStatusCode.InternalServerError;
        }

        public DicomDataStoreException()
        {
        }

        public DicomDataStoreException(string message)
            : base(message)
        {
        }

        public DicomDataStoreException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected DicomDataStoreException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
            EnsureArg.IsNotNull(serializationInfo, nameof(serializationInfo));

            StatusCode = serializationInfo.GetInt32(nameof(StatusCode));
        }

        public int StatusCode { get; }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext context)
        {
            EnsureArg.IsNotNull(serializationInfo, nameof(serializationInfo));

            serializationInfo.AddValue(nameof(StatusCode), StatusCode);
            base.GetObjectData(serializationInfo, context);
        }
    }
}
