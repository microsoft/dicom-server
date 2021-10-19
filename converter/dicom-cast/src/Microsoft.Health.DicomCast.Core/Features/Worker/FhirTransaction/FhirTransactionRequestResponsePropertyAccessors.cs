// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections;
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
                Func<FhirTransactionRequest, IEnumerable<FhirTransactionRequestEntry>> requestPropertyGetterDelegate = CreateGetter(interfacePropertyInfo);

                Action<FhirTransactionResponse, IEnumerable<FhirTransactionResponseEntry>> responsePropertySetterDelegate = CreateSetter(interfacePropertyInfo);

                return new FhirTransactionRequestResponsePropertyAccessor(interfacePropertyInfo.Name, requestPropertyGetterDelegate, responsePropertySetterDelegate);
            }
        }

        private static Func<FhirTransactionRequest, IEnumerable<FhirTransactionRequestEntry>> CreateGetter(PropertyInfo propertyInfo)
        {
            ParameterExpression paramExpr = Expression.Parameter(typeof(FhirTransactionRequest));
            MemberExpression getPropertyExpr = Expression.Property(paramExpr, propertyInfo.Name);

            Expression bodyExpr;
            if (typeof(IEnumerable).IsAssignableFrom(propertyInfo.PropertyType))
            {
                bodyExpr = getPropertyExpr;
            }
            else
            {
                // TODO: Do these need to be cast to IEnumerable<FhirTransactionRequestEntry> or can expr tree handle it?
                bodyExpr = Expression.NewArrayInit(typeof(FhirTransactionRequestEntry), getPropertyExpr);
            }

            return Expression.Lambda<Func<FhirTransactionRequest, IEnumerable<FhirTransactionRequestEntry>>>(bodyExpr, paramExpr).Compile();
        }

        private static Action<FhirTransactionResponse, IEnumerable<FhirTransactionResponseEntry>> CreateSetter(PropertyInfo propertyInfo)
        {
            ParameterExpression responseParamExpr = Expression.Parameter(typeof(FhirTransactionResponse));
            ParameterExpression entryParamExpr = Expression.Parameter(typeof(IEnumerable<FhirTransactionResponseEntry>));
            MemberExpression propertyExpr = Expression.Property(responseParamExpr, propertyInfo.Name);

            Expression bodyExpr;
            if (typeof(IEnumerable).IsAssignableFrom(propertyInfo.PropertyType))
            {
                bodyExpr = Expression.Assign(propertyExpr, entryParamExpr);
            }
            else
            {
                var singleMethod = typeof(Enumerable)
                    .GetMethods()
                    .FirstOrDefault(
                        x => x.Name.Equals("Single", StringComparison.OrdinalIgnoreCase) &&
                             x.IsGenericMethod &&
                             x.GetParameters().Length == 1)?
                    .MakeGenericMethod(typeof(FhirTransactionResponseEntry));

                bodyExpr = Expression.Assign(propertyExpr, Expression.Call(null, singleMethod, entryParamExpr));
            }

            return Expression.Lambda<Action<FhirTransactionResponse, IEnumerable<FhirTransactionResponseEntry>>>(bodyExpr, responseParamExpr, entryParamExpr).Compile();
        }

        /// <inheritdoc/>
        public IReadOnlyList<FhirTransactionRequestResponsePropertyAccessor> PropertyAccessors => _propertiesAccessors;
    }
}
