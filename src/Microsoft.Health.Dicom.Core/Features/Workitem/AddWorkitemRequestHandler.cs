// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Dicom.Serialization;
using EnsureThat;
using MediatR;
using Microsoft.Health.Core.Features.Security.Authorization;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Security;
using Microsoft.Health.Dicom.Core.Messages.WorkitemMessages;
using Newtonsoft.Json;

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

            if (await AuthorizationService.CheckAccess(DataActions.Write, cancellationToken) != DataActions.Write)
            {
                throw new UnauthorizedDicomActionException(DataActions.Write);
            }

            request.Validate();

            using (var streamReader = new StreamReader(request.RequestBody))
            {
                var json = await streamReader.ReadToEndAsync();

                IEnumerable<DicomDataset> dataset = JsonConvert.DeserializeObject<IEnumerable<DicomDataset>>(json, new JsonDicomConverter());

                return await _workItemService
                    .ProcessAsync(dataset.FirstOrDefault(), request.WorkitemInstanceUid, cancellationToken);
            }
        }
    }
}
