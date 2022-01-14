// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using FellowOakDicom.Serialization;
using MediatR;
using Microsoft.Health.Core.Features.Security.Authorization;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Security;
using Microsoft.Health.Dicom.Core.Messages.WorkitemMessages;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    public class AddWorkitemRequestHandler : BaseHandler, IRequestHandler<AddWorkitemRequest, AddWorkitemResponse>
    {
        private readonly IWorkitemService _workItemService;

        public AddWorkitemRequestHandler(
            IAuthorizationService<DataActions> authorizationService,
            IWorkitemService workItemService)
            : base(authorizationService)
        {
            _workItemService = EnsureArg.IsNotNull(workItemService, nameof(workItemService));
        }

        /// <inheritdoc />
        public async Task<AddWorkitemResponse> Handle(
            AddWorkitemRequest request,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(request, nameof(request));

            if (await AuthorizationService.CheckAccess(DataActions.Write, cancellationToken).ConfigureAwait(false) != DataActions.Write)
            {
                throw new UnauthorizedDicomActionException(DataActions.Write);
            }

            request.Validate();

            JsonSerializerOptions serializerOptions = new JsonSerializerOptions();
            serializerOptions.Converters.Add(new DicomJsonConverter());

            using (var streamReader = new StreamReader(request.RequestBody))
            {
                string json = await streamReader.ReadToEndAsync().ConfigureAwait(false);

                IEnumerable<DicomDataset> dataset = JsonSerializer.Deserialize<IEnumerable<DicomDataset>>(json, serializerOptions);

                return await _workItemService
                    .ProcessAsync(dataset.FirstOrDefault(), request.WorkitemInstanceUid, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}
