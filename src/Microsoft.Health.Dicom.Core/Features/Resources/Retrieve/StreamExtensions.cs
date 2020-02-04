// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using Dicom;
using Dicom.Imaging.Codec;

namespace Microsoft.Health.Dicom.Core.Features.Resources.Retrieve
{
    public static class StreamExtensions
    {
        public static Stream EncodeDicomFileAsDicom(this Stream stream, DicomTransferSyntax requestedTransferSyntax)
        {
            var tempDicomFile = DicomFile.Open(stream);

            // If the DICOM file is already in the requested transfer syntax OR original transfer syntax is requested,
            // return the base stream, otherwise re-encode.
            if ((tempDicomFile.Dataset.InternalTransferSyntax == requestedTransferSyntax) ||
                (requestedTransferSyntax == null))
            {
                stream.Seek(offset: 0, SeekOrigin.Begin);
                return stream;
            }
            else
            {
                try
                {
                    var transcoder = new DicomTranscoder(
                        tempDicomFile.Dataset.InternalTransferSyntax,
                        requestedTransferSyntax);
                    tempDicomFile = transcoder.Transcode(tempDicomFile);
                }
                catch
                {
                    // We catch all here as Transcoder can throw a wide variety of things.
                    // Basically this means codec failure - a quite extraordinary situation, but not impossible
                    // Proper solution here would be to actually try transcoding all the files that we are
                    // returning and either form a PartialContent or NotAcceptable response with an extra error message in
                    // the headers. Because transcoding is an expensive operation, we choose to do it from within the
                    // LazyTransformReadOnlyStream at the time when response is being formed by the server, therefore this code
                    // is called from ASP.NET framework and at this point we can not change our server response.
                    // The decision for now is just to return an empty stream here letting the client handle it.
                    // In the future a more optimal solution may involve maintaining a cache of transcoded images and
                    // using that to determine if transcoding is possible from within the Handle method.

                    tempDicomFile = null;
                }

                var resultStream = new MemoryStream();

                if (tempDicomFile != null)
                {
                    tempDicomFile.Save(resultStream);
                    resultStream.Seek(offset: 0, loc: SeekOrigin.Begin);
                }

                // We can dispose of the base stream as this is not needed.
                stream.Dispose();
                return resultStream;
            }
        }
    }
}
