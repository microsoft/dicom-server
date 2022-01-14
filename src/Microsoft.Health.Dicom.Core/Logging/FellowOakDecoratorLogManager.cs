// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Logging;
using IFellowOakLogger = FellowOakDicom.Log.ILogger;
using IFellowOakLogManager = FellowOakDicom.Log.ILogManager;

namespace Microsoft.Health.Dicom.Core.Logging
{
    internal class FellowOakDecoratorLogManager : IFellowOakLogManager
    {
        private readonly ILoggerFactory _factory;

        public FellowOakDecoratorLogManager(ILoggerFactory factory)
            => _factory = EnsureArg.IsNotNull(factory, nameof(factory));

        public IFellowOakLogger GetLogger(string name)
            => new FellowOakLoggerDecorator(_factory.CreateLogger(name));
    }
}
