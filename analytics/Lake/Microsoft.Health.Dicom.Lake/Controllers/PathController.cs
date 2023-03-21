// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;
using Azure;
using Azure.Storage.Files.DataLake.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Health.Dicom.Lake.Azure;

namespace Microsoft.Health.Dicom.Lake.Controllers;

[ApiController]
[Route("{filesystem}")]
public class PathController : ControllerBase
{
    private readonly ILogger<PathController> _logger;

    public PathController(ILogger<PathController> logger)
        => _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    [HttpGet]
    public IEnumerable<PathItem> List(
        [FromRoute][Required] string filesystem,
        [FromQuery][Required] bool recursive,
        [FromQuery][Required] ResourceType resource,
        [FromQuery] string? continuation,
        [FromQuery] string? directory,
        [FromQuery][Range(0, int.MaxValue)] int maxResults,
        [FromQuery][Range(0, int.MaxValue)] int timeout,
        [FromQuery] bool upn)
    {
        yield return DataLakeModelFactory.PathItem(
            "foo",
            isDirectory: false,
            lastModified: DateTimeOffset.UtcNow,
            eTag: ETag.All,
            contentLength: 123,
            owner: "bar",
            group: "baz",
            permissions: "rw",
            createdOn: DateTimeOffset.UtcNow,
            expiresOn: DateTimeOffset.UtcNow);
    }
}
