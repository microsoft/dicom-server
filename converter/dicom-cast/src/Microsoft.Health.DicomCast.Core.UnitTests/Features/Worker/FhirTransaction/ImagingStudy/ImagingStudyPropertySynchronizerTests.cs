// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Options;
using Microsoft.Health.DicomCast.Core.Configurations;
using Microsoft.Health.DicomCast.Core.Features.ExceptionStorage;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;
using NSubstitute;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Worker.FhirTransaction
{
    public class ImagingStudyPropertySynchronizerTests
    {
        private const string DefaultStudyInstanceUid = "111";
        private const string DefaultSeriesInstanceUid = "222";
        private const string DefaultSopInstanceUid = "333";
        private const string DefaultPatientResourceId = "555";
        private const string NewAccessionNumber = "2";
        private readonly IImagingStudyPropertySynchronizer _imagingStudyPropertySynchronizer;

        private readonly DicomCastConfiguration _dicomCastConfig = new DicomCastConfiguration();
        private readonly IExceptionStore _exceptionStore = Substitute.For<IExceptionStore>();

        public ImagingStudyPropertySynchronizerTests()
        {
            _imagingStudyPropertySynchronizer = new ImagingStudyPropertySynchronizer(Options.Create(_dicomCastConfig), _exceptionStore);
        }

        [Fact]
        public async Task GivenATransactionContexAndImagingStudy_WhenProcessedForStudy_ThenDicomPropertiesAreCorrectlyMappedtoImagingStudyAsync()
        {
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(DefaultStudyInstanceUid, new List<string>() { DefaultSeriesInstanceUid }, new List<string>() { DefaultSopInstanceUid }, DefaultPatientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset());

            await _imagingStudyPropertySynchronizer.SynchronizeAsync(context, imagingStudy);

            Assert.Collection(
               imagingStudy.Endpoint,
               reference => string.Equals(reference.Reference, context.Request.Endpoint.Resource.ToString(), StringComparison.Ordinal));

            Assert.Collection(
               imagingStudy.Modality,
               modality => string.Equals(modality.Code, "MODALITY", StringComparison.Ordinal));

            Assert.Collection(
               imagingStudy.Note,
               note => string.Equals(note.Text.ToString(), "Study Description", StringComparison.Ordinal));

            Assert.Collection(
               imagingStudy.Identifier,
               identifier => string.Equals(identifier.Value, $"urn:oid:{DefaultStudyInstanceUid}", StringComparison.Ordinal), // studyinstanceUid
               identifier => string.Equals(identifier.Value, "1", StringComparison.Ordinal)); // accession number

            Assert.Equal(new FhirDateTime(1974, 7, 10, 7, 10, 24, TimeSpan.Zero), imagingStudy.StartedElement);
        }

        [Fact]
        public async Task GivenATransactionContexAndImagingStudyWithNewModality_WhenProcessedForStudy_ThenNewModalityIsAddedAsync()
        {
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(DefaultStudyInstanceUid, new List<string>() { DefaultSeriesInstanceUid }, new List<string>() { DefaultSopInstanceUid }, DefaultPatientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset());

            await _imagingStudyPropertySynchronizer.SynchronizeAsync(context, imagingStudy);

            Assert.Collection(
               imagingStudy.Modality,
               modality => string.Equals(modality.Code, "MODALITY", StringComparison.Ordinal));

            FhirTransactionContext newConText = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset(modalityInStudy: "NEWMODALITY", modalityInSeries: "NEWMODALITY"));

            await _imagingStudyPropertySynchronizer.SynchronizeAsync(newConText, imagingStudy);

            Assert.Collection(
               imagingStudy.Modality,
               modality => string.Equals(modality.Code, "MODALITY", StringComparison.Ordinal),
               modality => string.Equals(modality.Code, "NEWMODALITY", StringComparison.Ordinal));
        }

        [Fact]
        public async Task GivenATransactionContextAndImagingStudyWithExitsingModality_WhenProcessedForStudy_ThenModalityIsNotAddedAsync()
        {
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(DefaultStudyInstanceUid, new List<string>() { DefaultSeriesInstanceUid }, new List<string>() { DefaultSopInstanceUid }, DefaultPatientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset());

            await _imagingStudyPropertySynchronizer.SynchronizeAsync(context, imagingStudy);

            Assert.Collection(
               imagingStudy.Modality,
               modality => string.Equals(modality.Code, "MODALITY", StringComparison.Ordinal));

            await _imagingStudyPropertySynchronizer.SynchronizeAsync(context, imagingStudy);

            Assert.Collection(
               imagingStudy.Modality,
               modality => string.Equals(modality.Code, "MODALITY", StringComparison.Ordinal));
        }

        [Fact]
        public async Task GivenATransactionContexAndImagingStudyWithNewAccessionNumber_WhenProcessedForStudy_ThenNewAccessionNumberIsAddedAsync()
        {
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(DefaultStudyInstanceUid, new List<string>() { DefaultSeriesInstanceUid }, new List<string>() { DefaultSopInstanceUid }, DefaultPatientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset());

            await _imagingStudyPropertySynchronizer.SynchronizeAsync(context, imagingStudy);

            Assert.Collection(
                imagingStudy.Identifier,
                identifier => ValidationUtility.ValidateIdentifier("urn:dicom:uid", $"urn:oid:{DefaultStudyInstanceUid}", identifier), // studyinstanceUid
                identifier => ValidationUtility.ValidateAccessionNumber(null, FhirTransactionContextBuilder.DefaultAccessionNumber, identifier)); // accession number

            FhirTransactionContext newConText = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset(accessionNumber: NewAccessionNumber));

            await _imagingStudyPropertySynchronizer.SynchronizeAsync(newConText, imagingStudy);

            Assert.Collection(
                imagingStudy.Identifier,
                identifier => ValidationUtility.ValidateIdentifier("urn:dicom:uid", $"urn:oid:{DefaultStudyInstanceUid}", identifier), // studyinstanceUid
                identifier => ValidationUtility.ValidateAccessionNumber(null, FhirTransactionContextBuilder.DefaultAccessionNumber, identifier), // accession number
                identifier => ValidationUtility.ValidateAccessionNumber(null, NewAccessionNumber, identifier)); // new accession number
        }

        [Fact]
        public async Task GivenATransactionContextAndImagingStudyWithExitsingAccessionNumber_WhenProcessedForStudy_ThenAccessionNumberIsNotAddedAsync()
        {
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(DefaultStudyInstanceUid, new List<string>() { DefaultSeriesInstanceUid }, new List<string>() { DefaultSopInstanceUid }, DefaultPatientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset());

            await _imagingStudyPropertySynchronizer.SynchronizeAsync(context, imagingStudy);

            Assert.Collection(
                imagingStudy.Identifier,
                identifier => ValidationUtility.ValidateIdentifier("urn:dicom:uid", $"urn:oid:{DefaultStudyInstanceUid}", identifier),
                identifier => ValidationUtility.ValidateAccessionNumber(null, FhirTransactionContextBuilder.DefaultAccessionNumber, identifier));

            await _imagingStudyPropertySynchronizer.SynchronizeAsync(context, imagingStudy);

            Assert.Collection(
                imagingStudy.Identifier,
                identifier => ValidationUtility.ValidateIdentifier("urn:dicom:uid", $"urn:oid:{DefaultStudyInstanceUid}", identifier),
                identifier => ValidationUtility.ValidateAccessionNumber(null, FhirTransactionContextBuilder.DefaultAccessionNumber, identifier));
        }

        [Fact]
        public async Task GivenATransactionContextAndImagingStudyWithNoEndpoint_WhenProcessedForStudy_ThenNewEndpointIsAddedAsync()
        {
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(DefaultStudyInstanceUid, new List<string>() { DefaultSeriesInstanceUid }, new List<string>() { DefaultSopInstanceUid }, DefaultPatientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset());

            await _imagingStudyPropertySynchronizer.SynchronizeAsync(context, imagingStudy);

            Assert.Collection(
               imagingStudy.Endpoint,
               endPoint => Assert.Equal(context.Request.Endpoint.ResourceId.ToResourceReference(), endPoint));
        }

        [Fact]
        public async Task GivenATransactionContextAndImagingStudyWithExistingEndpointReference_WhenProcessedForStudy_ThenEndpointResourceIsNotAddedAsync()
        {
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(DefaultStudyInstanceUid, new List<string>() { DefaultSeriesInstanceUid }, new List<string>() { DefaultSopInstanceUid }, DefaultPatientResourceId);
            Endpoint endpoint = FhirResourceBuilder.CreateEndpointResource();
            var endpointResourceId = new ServerResourceId(ResourceType.Endpoint, endpoint.Id);
            var endpointReference = endpointResourceId.ToResourceReference();

            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset());
            context.Request.Endpoint = FhirTransactionRequestEntryGenerator.GenerateDefaultNoChangeRequestEntry<Endpoint>(endpointResourceId);

            imagingStudy.Endpoint.Add(endpointReference);

            await _imagingStudyPropertySynchronizer.SynchronizeAsync(context, imagingStudy);

            Assert.Collection(
             imagingStudy.Endpoint,
             endPoint => Assert.Equal(endpointReference, endPoint));
        }

        [Fact]
        public async Task GivenATransactionContextAndImagingStudyWithNewEndpointReference_WhenProcessedForStudyWithEndpoint_ThenEndpointIsAddedAsync()
        {
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(DefaultStudyInstanceUid, new List<string>() { DefaultSeriesInstanceUid }, new List<string>() { DefaultSopInstanceUid }, DefaultPatientResourceId);

            // Simulate the imaging study with an existing endpoint.
            Endpoint existingEndpoint = FhirResourceBuilder.CreateEndpointResource(id: "2345", name: "new wado-rs");
            var existingEndpointResourceId = new ServerResourceId(ResourceType.Endpoint, existingEndpoint.Id);
            var existingEndpointReference = existingEndpointResourceId.ToResourceReference();

            imagingStudy.Endpoint.Add(existingEndpointReference);

            Endpoint endpoint = FhirResourceBuilder.CreateEndpointResource();
            var endpointResourceId = new ServerResourceId(ResourceType.Endpoint, endpoint.Id);
            var endpointReference = endpointResourceId.ToResourceReference();

            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset());
            context.Request.Endpoint = FhirTransactionRequestEntryGenerator.GenerateDefaultNoChangeRequestEntry<Endpoint>(endpointResourceId);

            await _imagingStudyPropertySynchronizer.SynchronizeAsync(context, imagingStudy);

            Assert.Collection(
             imagingStudy.Endpoint,
             endPoint => Assert.Equal(existingEndpointReference, endPoint),
             endPoint => Assert.Equal(endpointReference, endPoint));
        }

        [Fact]
        public async Task GivenATransactionContexAndImagingStudyWithNewStudyDescription_WhenProcessedForStudy_ThenNewNoteIsAddedAsync()
        {
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(DefaultStudyInstanceUid, new List<string>() { DefaultSeriesInstanceUid }, new List<string>() { DefaultSopInstanceUid }, DefaultPatientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset());

            await _imagingStudyPropertySynchronizer.SynchronizeAsync(context, imagingStudy);

            Assert.Collection(
               imagingStudy.Note,
               note => string.Equals(note.Text.ToString(), "Study Description", StringComparison.Ordinal));

            // When studyDescription is same, note is not added twice

            await _imagingStudyPropertySynchronizer.SynchronizeAsync(context, imagingStudy);

            Assert.Collection(
               imagingStudy.Note,
               note => string.Equals(note.Text.ToString(), "Study Description", StringComparison.Ordinal));

            // When study description is new, new note is added
            FhirTransactionContext newConText = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset(studyDescription: "New Study Description"));

            await _imagingStudyPropertySynchronizer.SynchronizeAsync(newConText, imagingStudy);

            Assert.Collection(
               imagingStudy.Note,
               note => string.Equals(note.Text.ToString(), "Study Description", StringComparison.Ordinal),
               note => string.Equals(note.Text.ToString(), "New Study Description", StringComparison.Ordinal));
        }
    }
}
