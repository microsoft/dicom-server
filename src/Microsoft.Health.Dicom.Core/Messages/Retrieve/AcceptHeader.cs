// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Health.Dicom.Core.Web;

namespace Microsoft.Health.Dicom.Core.Messages.Retrieve
{
    public class AcceptHeader
    {
        private const string RequestOriginalDicomTransferSyntax = "*";
        private const string TransferSyntaxParameterName = "transfer-syntax";
        private const string TypeParameterName = "type";

        public AcceptHeader(string mediaType)
        {
            MediaType = mediaType;
            Parameters = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        }

        public string MediaType { get; }

        public IDictionary<string, string> Parameters { get; }

        public bool IsMultipartRelated()
        {
            return KnownContentTypes.MultipartRelated.Equals(MediaType, System.StringComparison.InvariantCultureIgnoreCase);
        }

        public string GetMediaTypeForMultipartRelated()
        {
            if (Parameters.ContainsKey(TypeParameterName))
            {
                return RemoveQuotes(Parameters[TypeParameterName]);
            }

            return string.Empty;
        }

        public string GetTransferSyntax()
        {
            if (Parameters.ContainsKey(TransferSyntaxParameterName))
            {
                return RemoveQuotes(Parameters[TransferSyntaxParameterName]);
            }

            return string.Empty;
        }

        public bool IsOriginalTransferSyntaxRequested()
        {
            return IsOriginalTransferSyntaxRequested(GetTransferSyntax());
        }

        public static bool IsOriginalTransferSyntaxRequested(string transferSyntax)
        {
            return RequestOriginalDicomTransferSyntax.Equals(transferSyntax, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Copy from https://github.com/dotnet/aspnetcore/blob/master/src/Http/Headers/src/HeaderUtilities.cs
        /// </summary>
        private static string RemoveQuotes(string input)
        {
            if (IsQuoted(input))
            {
                input = input.Substring(1, input.Length - 2);
            }

            return input;
        }

        /// <summary>
        /// Copy from https://github.com/dotnet/aspnetcore/blob/master/src/Http/Headers/src/HeaderUtilities.cs
        /// </summary>
        private static bool IsQuoted(string input)
        {
            return !string.IsNullOrEmpty(input) && input.Length >= 2 && input[0] == '"' && input[input.Length - 1] == '"';
        }
    }
}
