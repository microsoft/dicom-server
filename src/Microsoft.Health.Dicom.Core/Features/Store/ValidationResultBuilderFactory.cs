// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Features.Store;


internal class ValidationResultBuilderFactory : IValidationResultBuilderFactory
{
    private readonly Func<IValidationResultBuilder> _builderFactory;

    public ValidationResultBuilderFactory(Func<IValidationResultBuilder> builderFactory)
    {
        _builderFactory = builderFactory;
    }

    public IValidationResultBuilder Create()
    {
        return _builderFactory == null ? null : _builderFactory();
    }
}
