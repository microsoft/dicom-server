// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Management.Smo;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    /// <summary>
    /// Validate if new stored procedures compatible with old ones
    /// </summary>
    internal class StoredProcedureCompatibleValidator
    {
        public static void Validate(IReadOnlyCollection<StoredProcedure> newProcedures, IReadOnlyCollection<StoredProcedure> oldProcedures)
        {
            var pairs = GetComparisonProcedures(newProcedures, oldProcedures);

            foreach (var pair in pairs)
            {
                var oldOne = pair.Item1;
                var newOne = pair.Item2;

                List<StoredProcedureParameter> oldList = ParameterCollectionToList(oldOne.Parameters);
                List<StoredProcedureParameter> newList = ParameterCollectionToList(newOne.Parameters);

                // any old parameter should be able to find a match
                foreach (var paramOld in oldList)
                {
                    int iNewParam = newList.FindIndex(x => x.Name == paramOld.Name);
                    Assert.NotEqual(-1, iNewParam);
                    Assert.Equal(paramOld.DataType, newList[iNewParam].DataType);

                    // remove from new list since having a match
                    newList.RemoveAt(iNewParam);
                }

                // additional parameters must have default value
                foreach (var item in newList)
                {
                    Assert.NotEqual(string.Empty, item.DefaultValue);
                }
            }
        }

        private static List<Tuple<StoredProcedure, StoredProcedure>> GetComparisonProcedures(IReadOnlyCollection<StoredProcedure> newProcedures, IReadOnlyCollection<StoredProcedure> oldProcedures)
        {
            List<Tuple<StoredProcedure, StoredProcedure>> pairs = new List<Tuple<StoredProcedure, StoredProcedure>>();
            foreach (var oldOne in oldProcedures)
            {
                // every procedure in old database must have match one in new
                var newOne = newProcedures.FirstOrDefault(x => x.Name == oldOne.Name);
                Assert.NotNull(newOne);
                pairs.Add(new Tuple<StoredProcedure, StoredProcedure>(oldOne, newOne));
            }
            return pairs;
        }

        private static List<StoredProcedureParameter> ParameterCollectionToList(StoredProcedureParameterCollection collection)
        {
            List<StoredProcedureParameter> result = new List<StoredProcedureParameter>();
            for (int i = 0; i < collection.Count; i++)
            {
                result.Add(collection[i]);
            }
            return result;
        }
    }
}
