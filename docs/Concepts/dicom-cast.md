# Dicom-cast overview

Dicom cast allows synchronizing the data from a Dicom server to the Fhir server. This allows healthcare organization to integrate clinical and imaging data, expanding the use cases for health data and allows both a streamlined view of longitudinal patient data, and the ability to effectively create cohorts for medical studies, analytics, and machine learning.

## Architecture
![Architecture](/docs/images/dicom-cast-architecture.png)

## Current implementation supports
- A single-threaded process that reads from DICOM change feed and writes to FHIR server.
- The process is hosted by Azure Container Instance in our sample template.
- The mapping is of dicom tags to Fhir resource properties is specified below.
- Syncnronizes dicom tags to below Fhir resource types
    - Patient
    - ImagingStudy

## Mappings

The current implementation has below mapping.

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
| Endpoint.address ||| The root URL to the DICOMWeb service will be used when creating Endpoint. The rule is described in http://hl7.org/fhir/imagingstudy.html#endpoint. |

**ImagingStudy:**

| Property | Tag Id | Tag Name | Note |
| :------- | :----- | :------- | :--- |
| ImagingStudy.identifier.where(system = 'urn:dicom:uid') | (0020,000D) | StudyInstanceUID | The value will have prefix of `urn:oid:`. |
| ImagingStudy.status | | | The value 'available' will be used when creating ImagingStudy. |
| ImagingStudy.modality | (0008,0060) | Modality | Or should this be (0008,0061) ModalitiesInStudy? |
| ImagingStud y.subject | | | It will be linked to the Patient above. |
| ImagingStudy.started | (0008,0020), (0008,0030), (0008,0201) | StudyDate, StudyTime, TimezoneOffsetFromUTC | More detail about how timestamp is constructed below. |
| ImagingStudy.endpoint | | | It will be linked to the Endpoint above. |
| ImagingStudy.note | (0008,1030) | StudyDescription | |
| ImagingStudy.series.uid | (0020,000E) | SeriesInstanceUID | |
| ImagingStudy.series.number | (0020,0011) | SeriesNumber | |
| ImagingStudy.series.modality | (0008,0060) | Modality | |
| ImagingStudy.series.description | (0008,103E) | SeriesDescription | |
| ImagingStudy.series.started | (0008,0021), (0008,0031), (0008,0201) | SeriesDate, SeriesTime, TimezoneOffsetFromUTC | More detail about how timestamp is constructed below. |
| ImagingStudy.series.instance.uid | (0008,0018) | SOPInstanceUID | |
| ImagingStudy.series.instance.sopClass | (0008,0016) | SOPClassUID | |
| ImagingStudy.series.instance.number | (0020,0013) | InstanceNumber | |

_Timestamp:_

DICOM has different date time VR types. Some tags (like Study and Series) have the date, time, and UTC offset stored separately. This means that the date might be partial. We will try to translate this into partial date syntax allowed by the FHIR server.