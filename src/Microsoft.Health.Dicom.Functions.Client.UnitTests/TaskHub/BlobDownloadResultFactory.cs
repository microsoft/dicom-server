// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq.Expressions;
using System.Reflection;
using Azure.Storage.Blobs.Models;

namespace Microsoft.Health.Dicom.Functions.Client.UnitTests.TaskHub;

internal static class BlobDownloadResultFactory
{
    private static readonly Func<BinaryData, BlobDownloadResult> DownloadResultFactory = CreateFactory();

    public static BlobDownloadResult CreateResult(BinaryData data)
        => DownloadResultFactory(data);

    private static Func<BinaryData, BlobDownloadResult> CreateFactory()
    {
        ParameterExpression binaryParam = Expression.Parameter(typeof(BinaryData), "binaryData");
        ParameterExpression resultsVar = Expression.Variable(typeof(BlobDownloadResult), "results");

        return Expression
            .Lambda<Func<BinaryData, BlobDownloadResult>>(
                Expression.Block(
                    typeof(BlobDownloadResult),
                    new ParameterExpression[] { resultsVar },
                    Expression.Assign(
                        resultsVar,
                        Expression.New(
                            typeof(BlobDownloadResult).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, Type.EmptyTypes)!)),
                    Expression.Call(
                        resultsVar,
                        typeof(BlobDownloadResult).GetProperty(nameof(BlobDownloadResult.Content))!.GetSetMethod(nonPublic: true)!,
                        binaryParam),
                    resultsVar),
                binaryParam)
            .Compile();
    }
}
