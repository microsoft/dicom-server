// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

//using System.Collections.Generic;
//using System.Threading;
//using System.Threading.Tasks;
//using EnsureThat;
//using MediatR;
//using Microsoft.Health.Core.Features.Security.Authorization;
//using Microsoft.Health.Dicom.Core.Exceptions;
//using Microsoft.Health.Dicom.Core.Features.Common;
//using Microsoft.Health.Dicom.Core.Features.Partition;
//using Microsoft.Health.Dicom.Core.Features.Security;

//namespace Microsoft.Health.Dicom.Core.Features.Partition
//{
//    //public class PartitionHandler : BaseHandler, IRequestHandler<PartitionRequest, PartitionResponse>
//    //{
//    //    private readonly IPartitionService _partitionService;

//    //    public PartitionHandler(IAuthorizationService<DataActions> authorizationService, IPartitionService partitionService)
//    //        : base(authorizationService)
//    //    {
//    //        _partitionService = EnsureArg.IsNotNull(partitionService, nameof(partitionService));
//    //    }

//    //    public async Task<PartitionResponse> Handle(PartitionRequest request, CancellationToken cancellationToken)
//    //    {
//    //        EnsureArg.IsNotNull(request, nameof(request));

//    //        if (await AuthorizationService.CheckAccess(DataActions.Read, cancellationToken) != DataActions.Read)
//    //        {
//    //            throw new UnauthorizedDicomActionException(DataActions.Read);
//    //        }

//    //        IReadOnlyCollection<PartitionEntry> partitionEntries = await _partitionService.GetPartitions(cancellationToken);

//    //        return new PartitionResponse(partitionEntries);
//    //    }
//    //}
//}
