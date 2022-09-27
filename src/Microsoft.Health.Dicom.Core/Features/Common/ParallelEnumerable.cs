// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Common;

internal sealed class ParallelEnumerationOptions : ParallelOptions
{
    public int MaxBufferedItems { get; init; } = -1;
}

internal static class ParallelEnumerable
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Any failure should be surfaced to the reader.")]
    public static async IAsyncEnumerable<TResult> SelectParallel<TSource, TResult>(
        this IEnumerable<TSource> source,
        Func<TSource, CancellationToken, ValueTask<TResult>> selector,
        ParallelEnumerationOptions options)
    {
        // TODO: Support proper enumerator cancellation. Perhaps do not re-use ParallelOptions
        var state = new State<TSource, TResult>(source, selector, options);
        ValueTask producer = ProduceAsync(state);

        await foreach (TResult item in state.Items.Reader.ReadAllAsync(options.CancellationToken))
        {
            yield return item;
        }

        await producer;

        static async ValueTask ProduceAsync(State<TSource, TResult> state)
        {
            ChannelWriter<TResult> writer = state.Items.Writer;

            try
            {
                // Note: The order is not deterministic.
                // Items will be produced in the order that they are returned by the selector.
                await Parallel.ForEachAsync(
                    state.Source,
                    state.Options,
                    (item, token) => ProduceItemAsync(item, state.Selector, writer, token));
            }
            catch (Exception e)
            {
                writer.Complete(e);
                return;
            }

            writer.Complete();
        }

        static async ValueTask ProduceItemAsync(
            TSource item,
            Func<TSource, CancellationToken, ValueTask<TResult>> selector,
            ChannelWriter<TResult> writer,
            CancellationToken cancellationToken)
        {
            TResult result = await selector(item, cancellationToken);
            await writer.WriteAsync(result, cancellationToken);
        }
    }

    private sealed class State<TSource, TResult>
    {
        public State(
            IEnumerable<TSource> source,
            Func<TSource, CancellationToken, ValueTask<TResult>> selector,
            ParallelEnumerationOptions options)
        {
            Options = EnsureArg.IsNotNull(options, nameof(options));
            Selector = EnsureArg.IsNotNull(selector, nameof(selector));
            Source = EnsureArg.IsNotNull(source, nameof(source));

            // TODO: Enable buffering with unbounded parallelism if ChannelWriter supports a factory argument.
            // We have to modify the channel capacity as we will still resolve the selector before blocking
            // on the channel write. So if the maximum parallelism is 'p', there will be at most 'p' additional
            // threads blocked after the channel has reached capacity.
            if (options.MaxBufferedItems != -1)
            {
                if (options.MaxDegreeOfParallelism == -1)
                    throw new ArgumentException(DicomCoreResource.UnsupportedBuffering, nameof(options));

                if (options.MaxBufferedItems <= options.MaxDegreeOfParallelism)
                    throw new ArgumentException(DicomCoreResource.InvalidItemBuffering, nameof(options));

                Items = Channel.CreateBounded<TResult>(
                    new BoundedChannelOptions(options.MaxBufferedItems - options.MaxDegreeOfParallelism)
                    {
                        FullMode = BoundedChannelFullMode.Wait,
                        SingleReader = true,
                    });
            }
            else
            {
                Items = Channel.CreateUnbounded<TResult>(new UnboundedChannelOptions { SingleReader = true });
            }
        }

        public Channel<TResult> Items { get; }

        public ParallelOptions Options { get; }

        public Func<TSource, CancellationToken, ValueTask<TResult>> Selector { get; }

        public IEnumerable<TSource> Source { get; }
    }
}
