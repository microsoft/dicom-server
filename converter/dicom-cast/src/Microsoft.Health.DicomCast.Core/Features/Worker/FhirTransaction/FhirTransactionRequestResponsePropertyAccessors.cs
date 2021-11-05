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
                Func<FhirTransactionRequest, IEnumerable<FhirTransactionRequestEntry>> requestPropertyGetterDelegate = CreateGetterDelegate(interfacePropertyInfo);

                Action<FhirTransactionResponse, IEnumerable<FhirTransactionResponseEntry>> responsePropertySetterDelegate = CreateSetterDelegate(interfacePropertyInfo);

                return new FhirTransactionRequestResponsePropertyAccessor(interfacePropertyInfo.Name, requestPropertyGetterDelegate, responsePropertySetterDelegate);
            }
        }

        private static Func<FhirTransactionRequest, IEnumerable<FhirTransactionRequestEntry>> CreateGetterDelegate(PropertyInfo propertyInfo)
        {
            ParameterExpression parameterExpression = Expression.Parameter(typeof(FhirTransactionRequest));
            MemberExpression propertyExpression = Expression.Property(parameterExpression, propertyInfo.Name);

            Expression bodyExpression;
            if (typeof(IEnumerable).IsAssignableFrom(propertyInfo.PropertyType))
            {
                bodyExpression = propertyExpression;
            }
            else
            {
                bodyExpression = Expression.NewArrayInit(typeof(FhirTransactionRequestEntry), propertyExpression);
            }

            return Expression.Lambda<Func<FhirTransactionRequest, IEnumerable<FhirTransactionRequestEntry>>>(bodyExpression, parameterExpression).Compile();
        }

        private static Action<FhirTransactionResponse, IEnumerable<FhirTransactionResponseEntry>> CreateSetterDelegate(PropertyInfo propertyInfo)
        {
            ParameterExpression parameterExpression = Expression.Parameter(typeof(FhirTransactionResponse));
            ParameterExpression enumerableParameterExpression = Expression.Parameter(typeof(IEnumerable<FhirTransactionResponseEntry>));
            MemberExpression propertyExpression = Expression.Property(parameterExpression, propertyInfo.Name);

            Expression bodyExpression;
            if (typeof(IEnumerable).IsAssignableFrom(propertyInfo.PropertyType))
            {
                bodyExpression = Expression.Assign(propertyExpression, enumerableParameterExpression);
            }
            else
            {
                MethodInfo singleMethod = typeof(Enumerable)
                    .GetMethods()
                    .Single(
                        x => x.Name == nameof(Enumerable.Single) &&
                             x.IsGenericMethod &&
                             x.GetParameters().Length == 1)
                    .MakeGenericMethod(typeof(FhirTransactionResponseEntry));

                bodyExpression = Expression.Assign(propertyExpression, Expression.Call(null, singleMethod, enumerableParameterExpression));
            }

            return Expression.Lambda<Action<FhirTransactionResponse, IEnumerable<FhirTransactionResponseEntry>>>(bodyExpression, parameterExpression, enumerableParameterExpression).Compile();
        }

        /// <inheritdoc/>
        public IReadOnlyList<FhirTransactionRequestResponsePropertyAccessor> PropertyAccessors => _propertiesAccessors;
    }
}
