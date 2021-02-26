// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using EnsureThat;

namespace Common
{
    public static class CollectionExtensions
    {
        private static readonly Random Rng = new Random();

        public static T RandomElement<T>(this IList<T> list)
        {
            EnsureArg.IsNotNull(list, nameof(list));
            return list[Rng.Next(list.Count)];
        }

        public static T RandomElement<T>(this T[] array)
        {
            EnsureArg.IsNotNull(array, nameof(array));
            return array[Rng.Next(array.Length)];
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            EnsureArg.IsNotNull(list, nameof(list));

            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = Rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
