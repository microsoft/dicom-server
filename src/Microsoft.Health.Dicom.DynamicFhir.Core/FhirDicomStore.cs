// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Core.Features.Conformance;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.Dicom.DynamicFhir.Core
{
    public class FhirDicomStore : IFhirDataStore, IProvideCapability
    {
        private const string _fhirDicomCodingScheme = "dicom";
        private const string _testModality = "CT";
        private const string _testDescription = "Test Description";
        private const string _testUid = "1.2.3.4";
        private const string _resourceId = "42";

        private readonly IResourceWrapperFactory _resourceWrapperFactory;

        public FhirDicomStore(IResourceWrapperFactory resourceWrapperFactory)
        {
            EnsureArg.IsNotNull(resourceWrapperFactory, nameof(resourceWrapperFactory));

            _resourceWrapperFactory = resourceWrapperFactory;
        }

        public void Build(ListedCapabilityStatement statement)
        {
            var supportedResources = new[]
            {
                ResourceType.Patient,
                ResourceType.ImagingStudy,
                ResourceType.Endpoint,
                ResourceType.CompartmentDefinition,
                ResourceType.Bundle,
                ResourceType.CapabilityStatement,
            };

            foreach (var resourceType in supportedResources)
            {
                statement.BuildRestResourceComponent(resourceType, builder =>
                {
                    builder.Versioning.Add(CapabilityStatement.ResourceVersionPolicy.NoVersion);
                    builder.ReadHistory = false;
                });

                statement.TryAddRestInteraction(resourceType, CapabilityStatement.TypeRestfulInteraction.Read);
            }
        }

        public Task<ResourceWrapper> GetAsync(ResourceKey key, CancellationToken cancellationToken)
        {
            if (key.Id == _resourceId && key.ResourceType == ResourceType.ImagingStudy.ToString())
            {
                // Fake implementation untill we can hook up to Dicom Storage
                var imagingStudy = new ImagingStudy();
                imagingStudy.Id = key.Id;
                imagingStudy.Meta = new Meta() { VersionId = key.Id };

                imagingStudy.Description = _testDescription;
                imagingStudy.Uid = _testUid;
                imagingStudy.ModalityList = new[] { _testModality }.Select(t => new Coding(_fhirDicomCodingScheme, t)).ToList();

                return Task.FromResult(_resourceWrapperFactory.Create(imagingStudy, false));
            }
            else
            {
                return Task.FromResult<ResourceWrapper>(null);
            }
        }

        public Task HardDeleteAsync(ResourceKey key, CancellationToken cancellationToken)
        {
            throw new System.NotSupportedException();
        }

        public Task<UpsertOutcome> UpsertAsync(ResourceWrapper resource, WeakETag weakETag, bool allowCreate, bool keepHistory, CancellationToken cancellationToken)
        {
            throw new System.NotSupportedException();
        }
    }
}
