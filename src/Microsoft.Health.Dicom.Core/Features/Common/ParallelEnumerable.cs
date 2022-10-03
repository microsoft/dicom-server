// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Common;

internal static class ParallelEnumerable
{
    public static async IAsyncEnumerable<TResult> SelectParallel<TSource, TResult>(
        this IEnumerable<TSource> source,
        Func<TSource, CancellationToken, ValueTask<TResult>> selector,
        ParallelEnumerationOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var state = new State<TSource, TResult>(source, selector, options, cancellationToken);
        ValueTask producer = ProduceAsync(state);

        await foreach (TResult item in state.Results.Reader.ReadAllAsync(cancellationToken))
        {
            yield return item;
        }

        await producer;

        static async ValueTask ProduceAsync(State<TSource, TResult> state)
        {
            ChannelWriter<TResult> writer = state.Results.Writer;

            try
            {
                // Note: The order is not deterministic.
                // Items will be produced in the order that they are returned by the selector.
                await Parallel.ForEachAsync(
                    state.Source,
                    state.Options,
                    async (item, token) =>
                    {
                        if (await writer.WaitToWriteAsync(token))
                        {
                            TResult result = await state.Selector(item, token);
                            await writer.WriteAsync(result, token);
                        }
                    });
            }
            finally
            {
                writer.Complete();
            }
        }
    }

    private sealed class State<TSource, TResult>
    {
        public State(
            IEnumerable<TSource> source,
            Func<TSource, CancellationToken, ValueTask<TResult>> selector,
            ParallelEnumerationOptions options,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(options, nameof(options));

            Source = EnsureArg.IsNotNull(source, nameof(source));
            Selector = EnsureArg.IsNotNull(selector, nameof(selector));
            Options = new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = options.MaxDegreeOfParallelism,
                TaskScheduler = options.TaskScheduler,
            };
            Results = Channel.CreateUnbounded<TResult>(new UnboundedChannelOptions { SingleReader = true });
        }

        public Channel<TResult> Results { get; }

        public ParallelOptions Options { get; }

        public Func<TSource, CancellationToken, ValueTask<TResult>> Selector { get; }

        public IEnumerable<TSource> Source { get; }
    }
}
