// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Messages.Export;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

public class ExportService : IExportService
{

    private readonly IDicomOperationsClient _client;
    private readonly IUrlResolver _uriResolver;

    public ExportService(IDicomOperationsClient client, IUrlResolver uriResolver)
    {
        _client = EnsureArg.IsNotNull(client, nameof(client));
        _uriResolver = EnsureArg.IsNotNull(uriResolver, nameof(uriResolver));
    }

    /// <summary>
    /// Export.
    /// </summary>
    /// <param name="exportInput">The export input.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response.</returns>
    public async Task<ExportResponse> ExportAsync(ExportInput exportInput, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(exportInput, nameof(exportInput));
        Guid operationId = await _client.StartExportAsync(GetOperationInput(exportInput), cancellationToken);
        return new ExportResponse(new OperationReference(operationId, _uriResolver.ResolveOperationStatusUri(operationId)));
    }

    private static ExportOperationInput GetOperationInput(ExportInput input)
    {
        HashSet<string> studies = new HashSet<string>();
        Dictionary<string, ISet<string>> serieses = new Dictionary<string, ISet<string>>();
        Dictionary<string, IDictionary<string, ISet<string>>> instances = new Dictionary<string, IDictionary<string, ISet<string>>>();
        foreach (string id in input.Source.IdFilter.Ids)
        {
            // TODO: more validation
            var ids = id.Split("/");
            if (ids.Length == 1)
            {
                // Study
                studies.Add(id);
            }
            else if (ids.Length == 2)
            {
                // Series
                string study = ids[0];
                if (!serieses.ContainsKey(study))
                {
                    serieses.Add(study, new HashSet<string>());
                }

                serieses[study].Add(ids[1]);
            }
            else if (ids.Length == 3)
            {
                string study = ids[0];
                string series = ids[1];
                string instance = ids[2];
                if (!instances.ContainsKey(study))
                {
                    instances.Add(study, new Dictionary<string, ISet<string>>());
                }
                if (!instances[study].ContainsKey(series))
                {
                    instances[study].Add(series, new HashSet<string>());
                }
                instances[study][series].Add(instance);
            }
            else
            {
                // TODO: throw specific excpetion and return proper httpCode (406?)
                throw new ArgumentException($"{id} is invalid");
            }
        }

        // if study is in export list, associated serieses and instances should not be export again
        List<string> dupStudiesInSeries = new List<string>();
        foreach (var study in serieses.Keys)
        {
            if (studies.Contains(study))
            {
                dupStudiesInSeries.Add(study);
            }
        }
        foreach (var study in dupStudiesInSeries)
        {
            serieses.Remove(study);
        }

        List<string> dupStudiesInInstances = new List<string>();
        foreach (var study in instances.Keys)
        {
            if (studies.Contains(study))
            {
                dupStudiesInInstances.Add(study);
            }
        }

        foreach (var study in dupStudiesInInstances)
        {
            instances.Remove(study);
        }

        // if series in in export list, associated instances should not be export again
        List<string> emptyStudiesInInstances = new List<string>();
        foreach (var study in instances.Keys)
        {
            List<string> dupSeriesInInstances = new List<string>();
            foreach (var series in instances[study].Keys)
            {
                if (serieses.ContainsKey(study) && serieses[study].Contains(series))
                {
                    dupSeriesInInstances.Add(study);
                }
            }

            foreach (var series in dupSeriesInInstances)
            {
                instances[study].Remove(series);
                if (instances[study].Count == 0)
                {
                    emptyStudiesInInstances.Add(study);
                }
            }
        }
        foreach (string study in emptyStudiesInInstances)
        {
            instances.Remove(study);
        }

        return new ExportOperationInput()
        {
            Source = new ExportOperationSource(studies, serieses, instances),
            Destination = input.Destination
        };
    }
}
