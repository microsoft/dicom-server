// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using EnsureThat;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Health.Dicom.Core.Extensions;

internal static class ConfigurationBinderExtensions
{
    public static void Set<T>(this IConfiguration configuration, T value) where T : class
        => Set(configuration, typeof(T), value, null); // TODO: Avoid boxing?

    public static void Set<T>(this IConfiguration configuration, T value, Action<BinderOptions> configureOptions) where T : class
        => Set(configuration, typeof(T), value, configureOptions);

    public static void Set(this IConfiguration configuration, Type type, object value)
        => Set(configuration, type, value, null);

    public static void Set(this IConfiguration configuration, Type type, object value, Action<BinderOptions> configureOptions)
    {
        EnsureArg.IsNotNull(configuration, nameof(configuration));
        EnsureArg.IsNotNull(type, nameof(type));
        EnsureArg.IsNotNull(value, nameof(value));

        var options = new BinderOptions();
        configureOptions?.Invoke(options);

        // TODO: Cache a dynamic method to perform the reflection only once. The Get methods are not cached,
        //       so it is not unreasonable to leave this as-is

        // TODO: Support copying from IConfiguration/IConfigurationSection

        // TODO: Support value types and primitives
        if (type.IsValueType)
            throw new InvalidOperationException();

        CopyToConfiguration(configuration, type, value, options);
    }

    private static void CopyToConfiguration(IConfiguration configuration, Type type, object value, BinderOptions options)
    {
        if (IsLiteralType(type))
        {
            if (configuration is not IConfigurationSection section)
                throw new InvalidOperationException();

            TypeConverter converter = TypeDescriptor.GetConverter(type);
            section.Value = converter.ConvertToInvariantString(value);
        }
        else if (type.IsArray || IsArrayLike(type))
        {
            Type elementType;
            if (type.IsArray)
            {
                elementType = type.GetElementType();
                if (type.IsArray && type.GetArrayRank() > 1)
                {
                    throw new InvalidOperationException();
                }
            }
            else
            {
                elementType = type.GetGenericArguments()[0];
            }

            int i = 0;
            foreach (object element in (IEnumerable)value)
            {
                CopyToConfiguration(configuration.GetSection(i.ToString(CultureInfo.InvariantCulture)), elementType, element, options);
                i++;
            }
        }
        else
        {
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Static;
            bindingFlags |= options.BindNonPublicProperties ? BindingFlags.Public : BindingFlags.NonPublic;
            foreach (PropertyInfo p in type.GetProperties(bindingFlags))
                CopyToConfiguration(configuration.GetSection(p.Name), p.PropertyType, p.GetValue(p), options);
        }
    }



    private static bool IsLiteralType(Type type)
        => type == typeof(sbyte)
        || type == typeof(byte)
        || type == typeof(short)
        || type == typeof(ushort)
        || type == typeof(int)
        || type == typeof(uint)
        || type == typeof(long)
        || type == typeof(ulong)
        || type == typeof(float)
        || type == typeof(double)
        || type == typeof(decimal)
        || type == typeof(char)
        || type == typeof(string)
        || type == typeof(DateTime)
        || type == typeof(DateTimeOffset)
        || type == typeof(TimeSpan)
        || type == typeof(Guid)
        || type == typeof(Uri);

    private static bool IsArrayLike(Type type)
    {
        if (!type.IsInterface || !type.IsConstructedGenericType)
            return false;

        Type genericTypeDefinition = type.GetGenericTypeDefinition();
        return genericTypeDefinition == typeof(IEnumerable<>)
            || genericTypeDefinition == typeof(IReadOnlyCollection<>)
            || genericTypeDefinition == typeof(IReadOnlyList<>);
    }
}
