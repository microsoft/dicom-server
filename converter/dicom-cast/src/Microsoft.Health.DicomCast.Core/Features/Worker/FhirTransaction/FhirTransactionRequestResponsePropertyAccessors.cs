// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    /// <summary>
    /// Provides property accessors for <see cref="FhirTransactionRequestEntry"/> and <see cref="FhirTransactionResponseEntry"/>.
    /// </summary>
    public class FhirTransactionRequestResponsePropertyAccessors : IFhirTransactionRequestResponsePropertyAccessors
    {
        private readonly FhirTransactionRequestResponsePropertyAccessor[] _propertiesAccessors;

        public FhirTransactionRequestResponsePropertyAccessors()
        {
            // Get the list of properties from the interface.
            PropertyInfo[] interfaceProperties = typeof(IFhirTransactionRequestResponse<>).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            _propertiesAccessors = interfaceProperties.Select(interfacePropertyInfo => Create(interfacePropertyInfo))
                .OrderBy(propertyAccessor => propertyAccessor.PropertyName)
                .ToArray();

            static FhirTransactionRequestResponsePropertyAccessor Create(PropertyInfo interfacePropertyInfo)
            {
                string propertyName = interfacePropertyInfo.Name;

                // Property getter from the request. This will generate equivalent of:
                // request => request.PropertyX;
                ParameterExpression requestParameterExpression = Expression.Parameter(typeof(FhirTransactionRequest));
                MemberExpression propertyExpression = Expression.Property(requestParameterExpression, propertyName);

                Func<FhirTransactionRequest, FhirTransactionRequestEntry> requestPropertyGetterDelegate = Expression.Lambda<Func<FhirTransactionRequest, FhirTransactionRequestEntry>>(
                    propertyExpression,
                    requestParameterExpression).Compile();

                // Property setter for the response. This will generate equivalent of:
                // (response, responseEntry) => response.PropertyX = responseEntry;
                ParameterExpression responseParameterExpression = Expression.Parameter(typeof(FhirTransactionResponse));
                propertyExpression = Expression.Property(responseParameterExpression, propertyName);
                ParameterExpression responseEntryParameterExpression = Expression.Parameter(typeof(FhirTransactionResponseEntry), "responseEntry");

                Action<FhirTransactionResponse, FhirTransactionResponseEntry> responsePropertySetterDelegate = Expression.Lambda<Action<FhirTransactionResponse, FhirTransactionResponseEntry>>(
                    Expression.Assign(propertyExpression, responseEntryParameterExpression),
                    responseParameterExpression,
                    responseEntryParameterExpression).Compile();

                return new FhirTransactionRequestResponsePropertyAccessor(propertyName, requestPropertyGetterDelegate, responsePropertySetterDelegate);
            }
        }

        /// <inheritdoc/>
        public IReadOnlyList<FhirTransactionRequestResponsePropertyAccessor> PropertyAccessors => _propertiesAccessors;
    }
}
