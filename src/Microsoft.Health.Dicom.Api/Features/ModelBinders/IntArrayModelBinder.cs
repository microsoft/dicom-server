// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Microsoft.Health.Dicom.Api.Features.ModelBinders
{
    internal class IntArrayModelBinder : CsvModelBinder<int>
    {
        protected override bool TryParse(string value, out int result)
            => int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }
}
