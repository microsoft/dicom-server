// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Hl7.Fhir.Model;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;
using Xunit;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Worker.FhirTransaction
{
    public class ImagingStudyPropertySynchronizerTests
    {
        private const string DefaultStudyInstanceUid = "111";
        private const string DefaultSeriesInstanceUid = "222";
        private const string DefaultSopInstanceUid = "333";
        private const string DefaultPatientResourceId = "555";
        private const string NewAccessionNumber = "2";
        private const int NumberOfStudyRelatedSeries = 3;
        private const int NumberOfStudyRelatedInstances = 2;
        private readonly IImagingStudyPropertySynchronizer _imagingStudyPropertySynchronizer;

        public ImagingStudyPropertySynchronizerTests()
        {
            _imagingStudyPropertySynchronizer = new ImagingStudyPropertySynchronizer();
        }

        [Fact]
        public void GivenATransactionContexAndImagingStudy_WhenProcessedForStudy_ThenDicomPropertiesAreCorrectlyMappedtoImagingStudy()
        {
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(DefaultStudyInstanceUid, new List<string>() { DefaultSeriesInstanceUid }, new List<string>() { DefaultSopInstanceUid }, DefaultPatientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset());

            _imagingStudyPropertySynchronizer.Synchronize(context, imagingStudy);

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
        public void GivenATransactionContexAndImagingStudyWithNewModality_WhenProcessedForStudy_ThenNewModalityIsAdded()
        {
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(DefaultStudyInstanceUid, new List<string>() { DefaultSeriesInstanceUid }, new List<string>() { DefaultSopInstanceUid }, DefaultPatientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset());

            _imagingStudyPropertySynchronizer.Synchronize(context, imagingStudy);

            Assert.Collection(
               imagingStudy.Modality,
               modality => string.Equals(modality.Code, "MODALITY", StringComparison.Ordinal));

            FhirTransactionContext newConText = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset(modalityInStudy: "NEWMODALITY", modalityInSeries: "NEWMODALITY"));

            _imagingStudyPropertySynchronizer.Synchronize(newConText, imagingStudy);

            Assert.Collection(
               imagingStudy.Modality,
               modality => string.Equals(modality.Code, "MODALITY", StringComparison.Ordinal),
               modality => string.Equals(modality.Code, "NEWMODALITY", StringComparison.Ordinal));
        }

        [Fact]
        public void GivenATransactionContextAndImagingStudyWithExitsingModality_WhenProcessedForStudy_ThenModalityIsNotAdded()
        {
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(DefaultStudyInstanceUid, new List<string>() { DefaultSeriesInstanceUid }, new List<string>() { DefaultSopInstanceUid }, DefaultPatientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset());

            _imagingStudyPropertySynchronizer.Synchronize(context, imagingStudy);

            Assert.Collection(
               imagingStudy.Modality,
               modality => string.Equals(modality.Code, "MODALITY", StringComparison.Ordinal));

            _imagingStudyPropertySynchronizer.Synchronize(context, imagingStudy);

            Assert.Collection(
               imagingStudy.Modality,
               modality => string.Equals(modality.Code, "MODALITY", StringComparison.Ordinal));
        }

        [Fact]
        public void GivenATransactionContexAndImagingStudyWithNewAccessionNumber_WhenProcessedForStudy_ThenNewAccessionNumberIsAdded()
        {
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(DefaultStudyInstanceUid, new List<string>() { DefaultSeriesInstanceUid }, new List<string>() { DefaultSopInstanceUid }, DefaultPatientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset());

            _imagingStudyPropertySynchronizer.Synchronize(context, imagingStudy);

            Assert.Collection(
                imagingStudy.Identifier,
                identifier => ValidationUtility.ValidateIdentifier("urn:dicom:uid", $"urn:oid:{DefaultStudyInstanceUid}", identifier), // studyinstanceUid
                identifier => ValidationUtility.ValidateAccessionNumber(null, FhirTransactionContextBuilder.DefaultAccessionNumber, identifier)); // accession number

            FhirTransactionContext newConText = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset(accessionNumber: NewAccessionNumber));

            _imagingStudyPropertySynchronizer.Synchronize(newConText, imagingStudy);

            Assert.Collection(
                imagingStudy.Identifier,
                identifier => ValidationUtility.ValidateIdentifier("urn:dicom:uid", $"urn:oid:{DefaultStudyInstanceUid}", identifier), // studyinstanceUid
                identifier => ValidationUtility.ValidateAccessionNumber(null, FhirTransactionContextBuilder.DefaultAccessionNumber, identifier), // accession number
                identifier => ValidationUtility.ValidateAccessionNumber(null, NewAccessionNumber, identifier)); // new accession number
        }

        [Fact]
        public void GivenATransactionContextAndImagingStudyWithExitsingAccessionNumber_WhenProcessedForStudy_ThenAccessionNumberIsNotAdded()
        {
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(DefaultStudyInstanceUid, new List<string>() { DefaultSeriesInstanceUid }, new List<string>() { DefaultSopInstanceUid }, DefaultPatientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset());

            _imagingStudyPropertySynchronizer.Synchronize(context, imagingStudy);

            Assert.Collection(
                imagingStudy.Identifier,
                identifier => ValidationUtility.ValidateIdentifier("urn:dicom:uid", $"urn:oid:{DefaultStudyInstanceUid}", identifier),
                identifier => ValidationUtility.ValidateAccessionNumber(null, FhirTransactionContextBuilder.DefaultAccessionNumber, identifier));

            _imagingStudyPropertySynchronizer.Synchronize(context, imagingStudy);

            Assert.Collection(
                imagingStudy.Identifier,
                identifier => ValidationUtility.ValidateIdentifier("urn:dicom:uid", $"urn:oid:{DefaultStudyInstanceUid}", identifier),
                identifier => ValidationUtility.ValidateAccessionNumber(null, FhirTransactionContextBuilder.DefaultAccessionNumber, identifier));
        }

        [Fact]
        public void GivenATransactionContexAndImagingStudyWithNumberOfFields_WhenProcessedForStudy_ThenNumberofFieldsAdded()
        {
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(DefaultStudyInstanceUid, new List<string>() { DefaultSeriesInstanceUid }, new List<string>() { DefaultSopInstanceUid }, DefaultPatientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset(numberOfStudyRelatedSeries: NumberOfStudyRelatedSeries, numberOfStudyRelatedInstances: NumberOfStudyRelatedInstances));

            _imagingStudyPropertySynchronizer.Synchronize(context, imagingStudy);

            Assert.Equal(NumberOfStudyRelatedSeries, imagingStudy.NumberOfSeries);
            Assert.Equal(NumberOfStudyRelatedInstances, imagingStudy.NumberOfInstances);
        }

        [Fact]
        public void GivenATransactionContextAndImagingStudyWithNoEndpoint_WhenProcessedForStudy_ThenNewEndpointIsAdded()
        {
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(DefaultStudyInstanceUid, new List<string>() { DefaultSeriesInstanceUid }, new List<string>() { DefaultSopInstanceUid }, DefaultPatientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset());

            _imagingStudyPropertySynchronizer.Synchronize(context, imagingStudy);

            Assert.Collection(
               imagingStudy.Endpoint,
               endPoint => Assert.Equal(context.Request.Endpoint.ResourceId.ToResourceReference(), endPoint));
        }

        [Fact]
        public void GivenATransactionContextAndImagingStudyWithExistingEndpointReference_WhenProcessedForStudy_ThenEndpointResourceIsNotAdded()
        {
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(DefaultStudyInstanceUid, new List<string>() { DefaultSeriesInstanceUid }, new List<string>() { DefaultSopInstanceUid }, DefaultPatientResourceId);
            Endpoint endpoint = FhirResourceBuilder.CreateEndpointResource();
            var endpointResourceId = new ServerResourceId(ResourceType.Endpoint, endpoint.Id);
            var endpointReference = endpointResourceId.ToResourceReference();

            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset());
            context.Request.Endpoint = FhirTransactionRequestEntryGenerator.GenerateDefaultNoChangeRequestEntry<Endpoint>(endpointResourceId);

            imagingStudy.Endpoint.Add(endpointReference);

            _imagingStudyPropertySynchronizer.Synchronize(context, imagingStudy);

            Assert.Collection(
             imagingStudy.Endpoint,
             endPoint => Assert.Equal(endpointReference, endPoint));
        }

        [Fact]
        public void GivenATransactionContextAndImagingStudyWithNewEndpointReference_WhenProcessedForStudyWithEndpoint_ThenEndpointIsAdded()
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

            _imagingStudyPropertySynchronizer.Synchronize(context, imagingStudy);

            Assert.Collection(
             imagingStudy.Endpoint,
             endPoint => Assert.Equal(existingEndpointReference, endPoint),
             endPoint => Assert.Equal(endpointReference, endPoint));
        }

        [Fact]
        public void GivenATransactionContexAndImagingStudyWithNewStudyDescription_WhenProcessedForStudy_ThenNewNoteIsAdded()
        {
            ImagingStudy imagingStudy = FhirResourceBuilder.CreateNewImagingStudy(DefaultStudyInstanceUid, new List<string>() { DefaultSeriesInstanceUid }, new List<string>() { DefaultSopInstanceUid }, DefaultPatientResourceId);
            FhirTransactionContext context = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset());

            _imagingStudyPropertySynchronizer.Synchronize(context, imagingStudy);

            Assert.Collection(
               imagingStudy.Note,
               note => string.Equals(note.Text.ToString(), "Study Description", StringComparison.Ordinal));

            // When studyDescription is same, note is not added twice

            _imagingStudyPropertySynchronizer.Synchronize(context, imagingStudy);

            Assert.Collection(
               imagingStudy.Note,
               note => string.Equals(note.Text.ToString(), "Study Description", StringComparison.Ordinal));

            // When study description is new, new note is added
            FhirTransactionContext newConText = FhirTransactionContextBuilder.DefaultFhirTransactionContext(FhirTransactionContextBuilder.CreateDicomDataset(studyDescription: "New Study Description"));

            _imagingStudyPropertySynchronizer.Synchronize(newConText, imagingStudy);

            Assert.Collection(
               imagingStudy.Note,
               note => string.Equals(note.Text.ToString(), "Study Description", StringComparison.Ordinal),
               note => string.Equals(note.Text.ToString(), "New Study Description", StringComparison.Ordinal));
        }
    }
}
