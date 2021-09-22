// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Api.Features.ModelBinders
{
    internal abstract class CsvModelBinder<T> : CsvModelBinder
    {
        protected override object DefaultValue => Array.Empty<T>();

        protected sealed override bool TryParse(string[] values, out object result)
        {
            T[] parsed = new T[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                if (!TryParse(values[i], out T parsedValue))
                {
                    result = default;
                    return false;
                }

                parsed[i] = parsedValue;
            }

            result = parsed;
            return true;
        }

        protected abstract bool TryParse(string value, out T result);
    }
}
