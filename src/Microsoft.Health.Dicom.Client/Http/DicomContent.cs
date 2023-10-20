// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using FellowOakDicom.IO.Writer;

namespace Microsoft.Health.Dicom.Client.Http;

public sealed class DicomContent : HttpContent
{
    private readonly DicomFile _file;
    private readonly DicomWriteOptions _options;

    public DicomContent(DicomFile file, DicomWriteOptions options = null)
    {
        _file = EnsureArg.IsNotNull(file, nameof(file));
        _options = options;

        Headers.ContentType = DicomWebConstants.MediaTypeApplicationDicom;
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        => _file.SaveAsync(stream, _options);

    protected override bool TryComputeLength(out long length)
    {
        length = default;
        return false;
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Callers will dispose of the StreamContent")]
    public static MultipartContent CreateMultipart(IEnumerable<DicomFile> files, DicomWriteOptions options = null)
    {
        EnsureArg.IsNotNull(files, nameof(files));

        MultipartContent content = new("related");
        content.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("type", $"\"{DicomWebConstants.MediaTypeApplicationDicom.MediaType}\""));

        foreach (DicomFile file in files)
            content.Add(new DicomContent(file, options));

        return content;
    }
}
