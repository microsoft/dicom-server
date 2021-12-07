// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.Health.Dicom.Api.Features.Swagger
{
    // The ReflectionTypeFilter is used to remove types added mistakenly by the Swashbuckle library.
    // Swashbuckle mistakenly assumes the DICOM server will serialize Type properties present on the fo-dicom types,
    // so this filter removes erroneously added properties in addition to their recursively added data models.
    internal class ReflectionTypeFilter : IDocumentFilter
    {
        // Get all built-in generic collections (and IEnumerable<T>)
        private static readonly HashSet<Type> CollectionTypes = typeof(List<>).Assembly
            .ExportedTypes
            .Where(x => x.Namespace == "System.Collections.Generic")
            .Where(x => x == typeof(IEnumerable<>) || x.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            .ToHashSet();

        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            HashSet<string> reflectionTypes = GetExposedTypes(typeof(Type));

            // Resolve the enumerable up-front so that the dictionary can be mutated below
            List<KeyValuePair<string, OpenApiSchema>> schemas = context.SchemaRepository.Schemas.ToList();

            // Remove all properties, and schemas themselves, whose types are from System.Reflection
            foreach (KeyValuePair<string, OpenApiSchema> entry in schemas)
            {
                if (reflectionTypes.Contains(entry.Key))
                {
                    context.SchemaRepository.Schemas.Remove(entry.Key);
                }
                else if (entry.Value.Type == "object" && entry.Value.Properties?.Count > 0)
                {
                    entry.Value.Properties = entry.Value.Properties
                        .Where(x => x.Value.Reference == null || !reflectionTypes.Contains(x.Value.Reference.Id))
                        .ToDictionary(x => x.Key, x => x.Value);
                }
            }
        }

        private static HashSet<string> GetExposedTypes(Type t)
        {
            var exposedTypes = new HashSet<Type>();
            UpdateExposedTypes(t, exposedTypes);

            // The OpenApiSchema type only has the type names
            return exposedTypes.Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
        }

        private static void UpdateExposedTypes(Type t, HashSet<Type> exposed)
        {
            // Get all public instance properties present in the Type that may be discovered via reflection by Swashbuckle
            foreach (Type propertyType in t.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(x => GetExposedType(x.PropertyType)))
            {
                if (!IsBuiltInType(propertyType) && exposed.Add(propertyType))
                {
                    UpdateExposedTypes(propertyType, exposed);
                }
            }
        }

        private static bool IsBuiltInType(Type t)
            => (t.IsPrimitive && t != typeof(IntPtr) && t != typeof(UIntPtr))
            || t == typeof(string)
            || t == typeof(TimeSpan)
            || t == typeof(DateTime)
            || t == typeof(DateTimeOffset)
            || t == typeof(Guid);

        private static Type GetExposedType(Type t)
        {
            // If we're serializing a collection to JSON, it will be represented as a JSON array.
            // So the type we're really concerned with is the element type contained in the collection.
            if (t.IsArray)
            {
                t = t.GetElementType();
            }
            else if (t.IsGenericType && CollectionTypes.Contains(t.GetGenericTypeDefinition()))
            {
                Type enumerableType = t.GetGenericTypeDefinition() != typeof(IEnumerable<>)
                    ? t.GetInterfaces().Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    : t;

                t = enumerableType.GetGenericArguments()[0];
            }

            return t;
        }
    }
}
