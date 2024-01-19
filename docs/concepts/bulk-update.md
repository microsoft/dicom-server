# Bulk update overview
Bulk update is a feature that enables updates of DICOM attributes/metadata without needing to delete and re-add. The currently supported attributes include those in the [Patient Identification Module](https://dicom.nema.org/dicom/2013/output/chtml/part03/sect_C.2.html#table_C.2-2), [Patient Demographic Module](https://dicom.nema.org/dicom/2013/output/chtml/part03/sect_C.2.html#table_C.2-3) and the [General Study Module](https://dicom.nema.org/medical/dicom/2020b/output/chtml/part03/sect_C.7.2.html#table_C.7-3) that are not sequences, also listed below.

**Patient Identification Module**
| Attribute Name   | Tag           | Description           |
| ---------------- | --------------| --------------------- |
| Patient's Name   | (0010,0010)   | Patient's full name   |
| Patient ID       | (0010,0020)   | Primary hospital identification number or code for the patient. |
| Other Patient IDs| (0010,1000) | Other identification numbers or codes used to identify the patient. 
| Type of Patient ID| (0010,0022) |  The type of identifier in this item. Enumerated Values: TEXT RFID BARCODE Note The identifier is coded as a string regardless of the type, not as a binary value. 
| Other Patient Names| (0010,1001) | Other names used to identify the patient. 
| Patient's Birth Name| (0010,1005) | Patient's birth name. 
| Patient's Mother's Birth Name| (0010,1060) | Birth name of patient's mother. 
| Medical Record Locator | (0010,1090) | An identifier used to find the patient's existing medical record (e.g., film jacket). 
| Issuer of Patient ID | (0010,0021) | Identifier of the Assigning Authority (system, organization, agency, or department) that issued the Patient ID. 

**Patient Demographic Module**
| Attribute Name   | Tag           | Description           |
| ---------------- | --------------| --------------------- |
| Patient's Age | (0010,1010) | Age of the Patient.  |
| Occupation | (0010,2180) | Occupation of the Patient.  |
| Confidentiality Constraint on Patient Data Description | (0040,3001) | Special indication to the modality operator about confidentiality of patient information (e.g., that he should not use the patients name where other patients are present).  |
| Patient's Birth Date | (0010,0030) | Date of birth of the named patient  |
| Patient's Birth Time | (0010,0032) | Time of birth of the named patient  |
| Patient's Sex | (0010,0040) | Sex of the named patient.  |
| Quality Control Subject |(0010,0200) | Indicates whether or not the subject is a quality control phantom.  |
| Patient's Size | (0010,1020) | Patient's height or length in meters  |
| Patient's Weight | (0010,1030) | Weight of the patient in kilograms  |
| Patient's Address | (0010,1040) | Legal address of the named patient  |
| Military Rank | (0010,1080) | Military rank of patient  |
| Branch of Service | (0010,1081) | Branch of the military. The country allegiance may also be included (e.g., U.S. Army).  |
| Country of Residence | (0010,2150) | Country in which patient currently resides  |
| Region of Residence | (0010,2152) | Region within patient's country of residence  |
| Patient's Telephone Numbers | (0010,2154) | Telephone numbers at which the patient can be reached  |
| Ethnic Group | (0010,2160) | Ethnic group or race of patient  |
| Patient's Religious Preference | (0010,21F0) | The religious preference of the patient  |
| Patient Comments | (0010,4000) | User-defined comments about the patient | 
| Responsible Person | (0010,2297) | Name of person with medical decision making authority for the patient.  |
| Responsible Person Role | (0010,2298) | Relationship of Responsible Person to the patient.  |
| Responsible Organization | (0010,2299) | Name of organization with medical decision making authority for the patient.  |
| Patient Species Description | (0010,2201) | The species of the patient.  |
| Patient Breed Description | (0010,2292) | The breed of the patient.See Section C.7.1.1.1.1.  |
| Breed Registration Number | (0010,2295) | Identification number of a veterinary patient within the registry.  |

**General study module**
| Attribute Name   | Tag           | Description           |
| ---------------- | --------------| --------------------- |
| Referring Physician's Name | (0008,0090)   | Name of the Patient's referring physician   |
| Accession Number | (0008,0050)   | A RIS generated number that identifies the order for the Study. |
| Study Description | (0008,1030) | Institution-generated description or classification of the Study (component) performed. 

After a study is updated, there are two versions of the instances that can be retrieved: the original, unmodified instances and the latest version with updated attributes.  Intermediate versions are not persisted.  

## API Design
Following URIs assume an implicit DICOM service base URI. For example, the base URI of a DICOM server running locally would be `https://localhost:63838/`.
Example requests can be sent in the [Postman collection](../resources/Conformance-as-Postman.postman_collection.json).

### Bulk update study
Bulk update endpoint starts a long running operation that updates all the instances in the study with the specified attributes.

```http
POST ...v2/studies/$bulkUpdate
POST ...v2/partitions/{PartitionName}/studies/$bulkUpdate
```

#### Request Header

| Name         | Required  | Type   | Description                     |
| ------------ | --------- | ------ | ------------------------------- |
| Content-Type | False     | string | `application/json` is supported |

#### Request Body

Below `UpdateSpecification` is passed as the request body. The `UpdateSpecification` needs both `studyInstanceUids` and `changeDataset` to be specified. 

```json
{
   "studyInstanceUids": ["1.113654.3.13.1026"],
    "changeDataset": { 
        "00100010": { 
            "vr": "PN", 
            "Value": 
            [
                { 
                    "Alphabetic": "New Patient Name 1" 
                }
            ] 
        } 
    }
}
```

#### Responses
Upon successfully starting an bulk update operation, the bulk update API returns a `202` status code. The body of the response contains a reference to the operation.

```http
HTTP/1.1 202 Accepted
Content-Type: application/json
{
    "id": "1323c079a1b64efcb8943ef7707b5438",
    "href": "../v2/operations/1323c079a1b64efcb8943ef7707b5438"
}
```

| Name              | Type                                        | Description                                                  |
| ----------------- | ------------------------------------------- | ------------------------------------------------------------ |
| 202 (Accepted)    | [Operation Reference](#operation-reference) | A long-running operation has been started to update DICOM attributes |
| 400 (Bad Request) |                                             | Request body has invalid data                                |

### Operation Status
The above `href` URL can be polled for the current status of the export operation until completion. A terminal state is signified by a `200` status instead of `202`.

```http
GET .../operations/{operationId}
```

#### URI Parameters

| Name        | In   | Required | Type   | Description      |
| ----------- | ---- | -------- | ------ | ---------------- |
| operationId | path | True     | string | The operation id |

#### Responses

**Successful response**

```json
{
    "operationId": "1323c079a1b64efcb8943ef7707b5438",
    "type": "update",
    "createdTime": "2023-05-08T05:01:30.1441374Z",
    "lastUpdatedTime": "2023-05-08T05:01:42.9067335Z",
    "status": "completed",
    "percentComplete": 100,
    "results": {
        "studyUpdated": 1,
        "instanceUpdated": 16
    }
}
```

**Failure respose**
```
{
    "operationId": "1323c079a1b64efcb8943ef7707b5438",
    "type": "update",
    "createdTime": "2023-05-08T05:01:30.1441374Z",
    "lastUpdatedTime": "2023-05-08T05:01:42.9067335Z",
    "status": "failed",
    "percentComplete": 100,
    "results": {
        "studyUpdated": 0,
        "studyFailed": 1,
        "instanceUpdated": 0,
        "errors": [
            "Failed to update instances for study 1.113654.3.13.1026"
        ]
    }
}
```

If there are any instance specific exception, it will be added to the `errors` list. It will include all the UIDs of the instance like
`Instance UIDs - PartitionKey: 1, StudyInstanceUID: 1.113654.3.13.1026, SeriesInstanceUID: 1.113654.3.13.1035, SOPInstanceUID: 1.113654.3.13.1510`

| Name            | Type                    | Description                                  |
| --------------- | ----------------------- | -------------------------------------------- |
| 200 (OK)        | [Operation](#operation) | The operation with the specified ID has completed |
| 202 (Accepted)  | [Operation](#operation) | The operation with the specified ID is running |
| 404 (Not Found) |                         | Operation not found                   |

## Retrieve (WADO-RS)

The Retrieve Transaction feature provides the ability to retrieve stored studies, series, and instances, including both the original and latest versions.

> Note: The supported endpoints for retrieving instances and metadata are listed below.

| Method | Path                                                                    | Description |
| :----- | :---------------------------------------------------------------------- | :---------- |
| GET    | ../studies/{study}                                                      | Retrieves all instances within a study. |
| GET    | ../studies/{study}/metadata                                             | Retrieves the metadata for all instances within a study. |
| GET    | ../studies/{study}/series/{series}                                      | Retrieves all instances within a series. |
| GET    | ../studies/{study}/series/{series}/metadata                             | Retrieves the metadata for all instances within a series. |
| GET    | ../studies/{study}/series/{series}/instances/{instance}                 | Retrieves a single instance. |
| GET    | ../studies/{study}/series/{series}/instances/{instance}/metadata        | Retrieves the metadata for a single instance. |

In order to retrieve original version, `msdicom-request-original` header should be set to `true`.

Example request to retrieve an orifginal version of an instance is shown below.

```http 
GET .../studies/{study}/series/{series}/instances/{instance}
Accept: multipart/related; type="application/dicom"; transfer-syntax=*
msdicom-request-original: true
Content-Type: application/dicom
 ```
## Delete

This transaction will delete both original and latest version of instances.

> Note: After a Delete transaction the both the deleted instances will not be recoverable.

## Change Feed

Along with other actions (Create and Delete), Updated change feed action is populated for every update operation.

Examples of the request and response of change feed action can be found [here](./change-feed.md).

### Other APIs

There is no change in other APIs. All the other APIs supports only latest version of instances.

### What changed in DICOM file

As part of bulk update, only DICOM metadata is updated. The pixel data is not updated. Pixel data will be same as the original version.

Other than updating the metadata, the file meta information of the DICOM file is updated with the below information.

| Tag           | Attribute name        | Description           | Value
| --------------| --------------------- | --------------------- | --------------|
| (0002,0012)   | Implementation Class UID | Uniquely identifies the implementation that wrote this file and its content. | 1.3.6.1.4.1.311.129 |
| (0002,0013)   | Implementation Version Name | Identifies a version for an Implementation Class UID (0002,0012) | Assembly version of the DICOM service (e.g. 0.1.4785) |

Here, the UID `1.3.6.1.4.1.311.129` is a registered under [Microsoft OID arc](https://oidref.com/1.3.6.1.4.1.311) in IANA.

#### Limitations

> Only Patient identificaton and demographic attributes are supported for bulk update.

> Maximum of 50 studies can be updated at once.

> Only one update operation can be performed at a time.

> There is no way to delete only the latest version or revert back to original version.

> We do not support updating any field from non-null to a null value.
