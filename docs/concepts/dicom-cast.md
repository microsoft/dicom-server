# DICOM Cast overview

DICOM Cast allows synchronizing the data from a Medical Imaging Server for DICOM to a [FHIR Server for Azure](https://github.com/microsoft/fhir-server), which allows healthcare organization to integrate clinical and imaging data. DICOM Cast expands the use cases for health data by supporting both a streamlined view of longitudinal patient data and the ability to effectively create cohorts for medical studies, analytics, and machine learning.

## Architecture

![Architecture](/docs/images/dicom-cast-architecture.png)

1. **Poll for batch of changes**: DICOM Cast polls for any changes via [Change Feed](../concepts/change-feed.md), which captures any changes that occur in your Medical Imaging Server for DICOM.
1. **Fetch corresponding FHIR resources, if any**: If any changes correspond to FHIR resources, DICOM Cast will fetch these changes. DICOM Cast synchronizes DICOM tags to the FHIR resource types *Patient* and *ImagingStudy*.
1. **Merge FHIR resources and PUT as a bundle in a transaction**: The FHIR resources corresponding the DICOM Cast captured changes will be merged. The FHIR resources will be PUT as a bundle in a transaction into your Azure API for FHIR server.
1. **Persist state and process next batch**: DICOM Cast will then persist the current state to prepare for next batch of changes.

The current implementation of DICOM Cast supports:

- A single-threaded process that reads from DICOM change feed and writes to FHIR server.
- The process is hosted by Azure Container Instance in our sample template, but can be run elsewhere.
- Synchronizes DICOM tags to *Patient* and *ImagingStudy*  FHIR resource types*.
- Configuration to ignore invalid tags when syncing data from the change feed to FHIR resource types.
    - If `EnforceValidationOfTagValues` is enabled then the change feed entry will not be written to the FHIR server unless every tag that is mapped (see below for mappings) is valid
    - If `EnforceValidationOfTagValues` is disabled (default) then as if a value is invalid, but not required to be mapped (see below for required tags for Patient and Imaging Study) then that particular tag will not be mapped but the rest of the change feed entry will be mapped to FHIR resources. If a required tag is invalid then the change feed entry will not be written to FHIR Server
- Storage of errors to Azure Table Storage
    - Errors when processing change feed entries are persisted in Azure Table Storage in different tables depending on the cause of the error.
        - `InvalidDicomTagExceptionTable`: Stores information about any tags that had invalid values. Entries in here does not necessarily mean that the entire change feed entry was not stored in FHIR, but that the particular value had a validation issue.
        - `DicomFailToStoreExceptionTable`: Stores information about change feed entries that were not stored to FHIR due to an issue with the change feed entry (such as invalid required tag). All entries in this table were not stored to FHIR.
        - `FhirFailToStoreExceptionTable`: Stores information about change feed entries that were not stored to FHIR due to an issue with the FHIR server (such as conflicting resource already existing). All entries in this table were not stored to FHIR.
        - `TransientRetryExceptionTable`: Stores information about change feed entries that faced a transient error (such as FHIR server too busy) and are being retried. Entries in this table note how many times they have been retried but does not necessarily mean that they eventually failed or succeeded to store to FHIR.
        - `TransientFailureExceptionTable`: Stores information about change feed entries that had a transient error, and went through the retry policy and still failed to store to FHIR. All entries in this table failed to store to FHIR.

## Mappings

The current implementation of DICOM Cast has the following mappings:

**Patient:**

| Property | Tag Id | Tag Name | Required Tag?| Note |
| :------- | :----- | :------- | :----- | :----- |
| Patient.identifier.where(system = '') | (0010,0020) | PatientID | Yes | For now, the system will be empty string. We will add support later for allowing the system to be specified. |
| Patient.name.where(use = 'usual') | (0010,0010) | PatientName | No | PatientName will be split into components and added as HumanName to the Patient resource. |
| Patient.gender | (0010,0040) | PatientSex | No | |
| Patient.birthDate | (0010,0030) | PatientBirthDate | No | PatientBirthDate only contains the date. This implementation assumes that the FHIR and DICOM servers have data from the same time zone. |

**Endpoint:**

| Property | Tag Id | Tag Name | Note |
| :------- | :----- | :------- | :--- |
| Endpoint.status ||| The value 'active' will be used when creating Endpoint. |
| Endpoint.connectionType ||| The system 'http://terminology.hl7.org/CodeSystem/endpoint-connection-type' and value 'dicom-wado-rs' will be used when creating Endpoint. |
| Endpoint.address ||| The root URL to the DICOMWeb service will be used when creating Endpoint. The rule is described in 'http://hl7.org/fhir/imagingstudy.html#endpoint' |

**ImagingStudy:**

| Property | Tag Id | Tag Name | Required | Note |
| :------- | :----- | :------- | :--- | :--- |
| ImagingStudy.identifier.where(system = 'urn:dicom:uid') | (0020,000D) | StudyInstanceUID | Yes | The value will have prefix of `urn:oid:`. |
| ImagingStudy.status | | | No | The value 'available' will be used when creating ImagingStudy. |
| ImagingStudy.modality | (0008,0060) | Modality | No | Or should this be (0008,0061) ModalitiesInStudy? |
| ImagingStudy.subject | | | No | It will be linked to the Patient [above](##Mappings). |
| ImagingStudy.started | (0008,0020), (0008,0030), (0008,0201) | StudyDate, StudyTime, TimezoneOffsetFromUTC | No | More detail about how timestamp is constructed [below](###Timestamp). |
| ImagingStudy.endpoint | | | | It will be linked to the Endpoint above. |
| ImagingStudy.note | (0008,1030) | StudyDescription | No | |
| ImagingStudy.series.uid | (0020,000E) | SeriesInstanceUID | Yes | |
| ImagingStudy.series.number | (0020,0011) | SeriesNumber | No | |
| ImagingStudy.series.modality | (0008,0060) | Modality | Yes | |
| ImagingStudy.series.description | (0008,103E) | SeriesDescription | No | |
| ImagingStudy.series.started | (0008,0021), (0008,0031), (0008,0201) | SeriesDate, SeriesTime, TimezoneOffsetFromUTC | No | More detail about how timestamp is constructed [below](###Timestamp). |
| ImagingStudy.series.instance.uid | (0008,0018) | SOPInstanceUID | Yes | |
| ImagingStudy.series.instance.sopClass | (0008,0016) | SOPClassUID | Yes | |
| ImagingStudy.series.instance.number | (0020,0013) | InstanceNumber | No| |
| ImagingStudy.identifier.where(type.coding.system='http://terminology.hl7.org/CodeSystem/v2-0203' and type.coding.code='ACSN')) | (0008,0050) | Accession Number | No | Refer to http://hl7.org/fhir/imagingstudy.html#notes. |

### Timestamp

DICOM has different date time VR types. Some tags (like Study and Series) have the date, time, and UTC offset stored separately. This means that the date might be partial. This code attempts to translate this into a partial date syntax allowed by the FHIR server.

## Summary

In this concept, we reviewed the architecture and mappings of DICOM Cast. To implement DICOM Cast in your Medical Imaging for DICOM Server, refer to the following documents:

- [Quickstart on DICOM Cast](../quickstarts/deploy-dicom-cast.md)
- [Sync DICOM Metadata to FHIR](../how-to-guides/sync-dicom-metadata-to-fhir.md)
