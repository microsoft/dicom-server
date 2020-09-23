# DICOM Cast overview

DICOM Cast allows synchronizing the data from a Medical Imaging Server for DICOM to an Azure API for FHIR server, which allows healthcare organization to integrate clinical and imaging data. DICOM Cast expands the use cases for health data by supporting both a streamlined view of longitudinal patient data and the ability to effectively create cohorts for medical studies, analytics, and machine learning.

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

## Mappings

The current implementation of DICOM Cast has the following mappings:

**Patient:**

| Property | Tag Id | Tag Name | Note |
| :------- | :----- | :------- | :--- |
| Patient.identifier.where(system = '') | (0010,0020) | PatientID | For now, the system will be empty string. We will add support later for allowing the system to be specified. |
| Patient.name.where(use = 'usual') | (0010,0010) | PatientName | PatientName will be split into components and added as HumanName to the Patient resource. |
| Patient.gender | (0010,0040) | PatientSex ||

**Endpoint:**

| Property | Tag Id | Tag Name | Note |
| :------- | :----- | :------- | :--- |
| Endpoint.status ||| The value 'active' will be used when creating Endpoint. |
| Endpoint.connectionType ||| The system 'http://terminology.hl7.org/CodeSystem/endpoint-connection-type' and value 'dicom-wado-rs' will be used when creating Endpoint. |
| Endpoint.address ||| The root URL to the DICOMWeb service will be used when creating Endpoint. The rule is described in 'http://hl7.org/fhir/imagingstudy.html#endpoint' |

**ImagingStudy:**

| Property | Tag Id | Tag Name | Note |
| :------- | :----- | :------- | :--- |
| ImagingStudy.identifier.where(system = 'urn:dicom:uid') | (0020,000D) | StudyInstanceUID | The value will have prefix of `urn:oid:`. |
| ImagingStudy.status | | | The value 'available' will be used when creating ImagingStudy. |
| ImagingStudy.modality | (0008,0060) | Modality | Or should this be (0008,0061) ModalitiesInStudy? |
| ImagingStudy.subject | | | It will be linked to the Patient [above](##Mappings). |
| ImagingStudy.started | (0008,0020), (0008,0030), (0008,0201) | StudyDate, StudyTime, TimezoneOffsetFromUTC | More detail about how timestamp is constructed [below](###Timestamp). |
| ImagingStudy.endpoint | | | It will be linked to the Endpoint above. |
| ImagingStudy.note | (0008,1030) | StudyDescription | |
| ImagingStudy.series.uid | (0020,000E) | SeriesInstanceUID | |
| ImagingStudy.series.number | (0020,0011) | SeriesNumber | |
| ImagingStudy.series.modality | (0008,0060) | Modality | |
| ImagingStudy.series.description | (0008,103E) | SeriesDescription | |
| ImagingStudy.series.started | (0008,0021), (0008,0031), (0008,0201) | SeriesDate, SeriesTime, TimezoneOffsetFromUTC | More detail about how timestamp is constructed [below](###Timestamp). |
| ImagingStudy.series.instance.uid | (0008,0018) | SOPInstanceUID | |
| ImagingStudy.series.instance.sopClass | (0008,0016) | SOPClassUID | |
| ImagingStudy.series.instance.number | (0020,0013) | InstanceNumber | |

### Timestamp

DICOM has different date time VR types. Some tags (like Study and Series) have the date, time, and UTC offset stored separately. This means that the date might be partial. This code attempts to translate this into a partial date syntax allowed by the FHIR server.

## Summary

In this concept, we reviewed the architecture and mappings of DICOM Cast. To implement DICOM Cast in your Medical Imaging for DICOM Server, refer to the following documents:

- [Quickstart on DICOM Cast](../quickstarts/deploy-dicom-cast.md)
- [Sync DICOM Metadata to FHIR](../how-to-guides/sync-dicom-metadata-to-fhir.md)
