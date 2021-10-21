// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Core.Features.Security.Authorization;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Security;
using Microsoft.Health.Dicom.Core.Messages.Partition;

namespace Microsoft.Health.Dicom.Core.Features.Partition
{
    public class AddPartitionHandler : BaseHandler, IRequestHandler<GetOrAddPartitionRequest, GetOrAddPartitionResponse>
    {
        private readonly IPartitionService _partitionService;

        public AddPartitionHandler(IAuthorizationService<DataActions> authorizationService, IPartitionService partitionService)
            : base(authorizationService)
        {
            _partitionService = EnsureArg.IsNotNull(partitionService, nameof(partitionService));
        }

        public async Task<GetOrAddPartitionResponse> Handle(GetOrAddPartitionRequest request, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(request, nameof(request));

            if (await AuthorizationService.CheckAccess(DataActions.Write, cancellationToken) != DataActions.Write)
            {
                throw new UnauthorizedDicomActionException(DataActions.Write);
            }

            PartitionEntry partitionEntry = await _partitionService.AddPartition(request.PartitionName, cancellationToken);

            return new GetOrAddPartitionResponse(partitionEntry);
        }
    }
}
