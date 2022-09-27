// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Common;

internal sealed class ParallelEnumerationOptions
{
    public int MaxBufferedItems { get; init; } = -1;

    public int MaxDegreeOfParallelism { get; init; } = -1;
}

internal static class ParallelEnumerable
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Any failure should be surfaced to the reader.")]
    public static async IAsyncEnumerable<TResult> SelectParallel<TSource, TResult>(
        this IEnumerable<TSource> source,
        Func<TSource, CancellationToken, ValueTask<TResult>> selector,
        ParallelEnumerationOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var state = new State<TSource, TResult>(source, selector, options, cancellationToken);
        ValueTask producer = ProduceAsync(state);

        await foreach (TResult item in state.Items.Reader.ReadAllAsync(cancellationToken))
        {
            yield return item;
        }

        await producer;

        static async ValueTask ProduceAsync(State<TSource, TResult> state)
        {
            ChannelWriter<TResult> writer = state.Items.Writer;

            // Note that the order is not guaranteed to be deterministic. Items will be queued in the order they are found!
            await Parallel.ForEachAsync(state.Source, state.Options, (item, token) => ProduceItemAsync(item, state.Selector, writer, token));
            writer.Complete();
        }

        static async ValueTask ProduceItemAsync(
            TSource item,
            Func<TSource, CancellationToken, ValueTask<TResult>> selector,
            ChannelWriter<TResult> writer,
            CancellationToken cancellationToken)
        {
            try
            {
                TResult result = await selector(item, cancellationToken);
                await writer.WriteAsync(result, cancellationToken);
            }
            catch (Exception e)
            {
                writer.Complete(e);
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

            Items = options.MaxBufferedItems == -1
                ? Channel.CreateUnbounded<TResult>(new UnboundedChannelOptions { SingleReader = true })
                : Channel.CreateBounded<TResult>(new BoundedChannelOptions(options.MaxBufferedItems) { FullMode = BoundedChannelFullMode.Wait, SingleReader = true });
            Options = new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = options.MaxDegreeOfParallelism,
            };
            Selector = EnsureArg.IsNotNull(selector, nameof(selector));
            Source = EnsureArg.IsNotNull(source, nameof(source));
        }

        public Channel<TResult> Items { get; }

        public ParallelOptions Options { get; }

        public Func<TSource, CancellationToken, ValueTask<TResult>> Selector { get; }

        public IEnumerable<TSource> Source { get; }
    }
}
