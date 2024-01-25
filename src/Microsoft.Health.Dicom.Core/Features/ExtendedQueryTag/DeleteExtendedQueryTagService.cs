// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

public class DeleteExtendedQueryTagService : IDeleteExtendedQueryTagService
{
    private readonly IExtendedQueryTagStore _extendedQueryTagStore;
    private readonly IDicomTagParser _dicomTagParser;
    private readonly IGuidFactory _guidFactory;
    private readonly IDicomOperationsClient _client;

    public DeleteExtendedQueryTagService(
        IExtendedQueryTagStore extendedQueryTagStore,
        IDicomTagParser dicomTagParser,
        IGuidFactory guidFactory,
        IDicomOperationsClient client)
    {
        EnsureArg.IsNotNull(extendedQueryTagStore, nameof(extendedQueryTagStore));
        EnsureArg.IsNotNull(dicomTagParser, nameof(dicomTagParser));
        EnsureArg.IsNotNull(client, nameof(client));
        EnsureArg.IsNotNull(guidFactory, nameof(guidFactory));

        _extendedQueryTagStore = extendedQueryTagStore;
        _dicomTagParser = dicomTagParser;
        _client = client;
        _guidFactory = guidFactory;
    }

    public async Task DeleteExtendedQueryTagAsync(string tagPath, CancellationToken cancellationToken)
    {
        DicomTag[] tags;
        if (!_dicomTagParser.TryParse(tagPath, out tags))
        {
            throw new InvalidExtendedQueryTagPathException(
                string.Format(CultureInfo.InvariantCulture, DicomCoreResource.InvalidExtendedQueryTag, tagPath ?? string.Empty));
        }

        string normalizedPath = tags[0].GetPath();

        OperationReference operation = await _client.StartDeleteExtendedQueryTagOperationAsync(_guidFactory.Create(), normalizedPath, cancellationToken);

        // TODO: get operation and wait for it to be done
    }
}
