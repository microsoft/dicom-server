// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.Validation;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    public partial class WorkitemService : IWorkitemService
    {
        private static readonly Action<ILogger, ushort, Exception> LogValidationFailedDelegate =
            LoggerMessage.Define<ushort>(
                LogLevel.Information,
                default,
                "Validation failed for the DICOM instance work-item entry. Failure code: {FailureCode}.");

        private readonly IWorkitemResponseBuilder _responseBuilder;
        private readonly IEnumerable<IWorkitemDatasetValidator> _validators;
        private readonly IWorkitemOrchestrator _workitemOrchestrator;
        private readonly IElementMinimumValidator _minimumValidator;
        private readonly ILogger _logger;

        public WorkitemService(
            IWorkitemResponseBuilder responseBuilder,
            IEnumerable<IWorkitemDatasetValidator> dicomDatasetValidators,
            IWorkitemOrchestrator storeOrchestrator,
            IElementMinimumValidator minimumValidator,
            ILogger<WorkitemService> logger)
        {
            _responseBuilder = EnsureArg.IsNotNull(responseBuilder, nameof(responseBuilder));
            _validators = EnsureArg.IsNotNull(dicomDatasetValidators, nameof(dicomDatasetValidators));
            _workitemOrchestrator = EnsureArg.IsNotNull(storeOrchestrator, nameof(storeOrchestrator));
            _minimumValidator = EnsureArg.IsNotNull(minimumValidator, nameof(minimumValidator));
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        }

        private IWorkitemDatasetValidator GetValidator<T>() where T : IWorkitemDatasetValidator
        {
            var validator = _validators.FirstOrDefault(o => o is T);

            return validator;
        }
    }
}
