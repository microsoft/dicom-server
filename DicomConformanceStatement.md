# DICOM Conformance Statement

The **Azure for Health API** supports a subset of the DICOM Web standard. Support includes:

- [Store Transaction](##Store-Transaction)

## Store Transaction

This transaction uses the POST method to Store representations of Studies, Series, and Instances contained in the request payload.

Method|Path|Description
----------|----------|----------
POST|../studies|Store instances.
POST|../studies/{studyInstanceUID}|Store instances for a specific study. If any instance does not belong to the provided identifier, it will be rejected.

The following `'Accept'` headers for the response are supported:
- `application/dicom+json`

The following `'Content-Type'` headers are supported:
- `multipart/related; type=application/dicom`

> Note: The Server will <u>not</u> coerce or replace attributes that conflict with existing data. All data will be stored as provided.

The following DICOM elements are required to be present in every DICOM file attempting to be stored:
- StudyInstanceUID
- SeriesInstanceUID
- SopInstanceUID

Each file stored must have a unique combination of StudyInstanceUID, SeriesInstanceUID and SopInstanceUID. The warning code `45070` will be returned in the result if a file with the same identifiers exists.

### Response Status Codes

Code|Description
----------|----------
200 (OK)|When all the SOP instances in the request have been stored.
202 (Accepted) |When some instances in the request have been stored but others have failed.
400 (Bad Request)|The request was badly formatted.
406 (Not Acceptable)|The specified `Accept` header is not supported.
409 (Conflict) |When none of the instances in the store transaction request have been stored.
415 (Unsupported Media Type)|The provided `Content-Type` is not supported.

### Response Payload

The response payload will populate a DICOM dataset with the following elements:

Tag|Name|Description
----------|----------|----------
(0008, 1190)|RetrieveURL|The Retrieve URL of the study if the StudyInstanceUID was provided in the store request.
(0008,1198)|FailedSOPSequence|The sequence of instances that failed to store.
(0008, 1199)|ReferencedSOPSequence|The sequence of stored instances.

Each dataset in the `FailedSOPSequence` will have the following elements (if the DICOM file attempting to be stored could be read):

Tag|Name|Description
----------|----------|----------
(0008, 1150)|ReferencedSOPClassUID|The SOP class unique identifier of the instance that failed to store.
(0008, 1150)|ReferencedSOPInstanceUID|The SOP instance unique identifier of the instance that failed to store.
(0008,1197)|FailureReason|The reason code why this instance failed to store

Each dataset in the `ReferencedSOPSequence` will have the following elements:

Tag|Name|Description
----------|----------|----------
(0008, 1150)|ReferencedSOPClassUID|The SOP class unique identifier of the instance that failed to store.
(0008, 1150)|ReferencedSOPInstanceUID|The SOP instance unique identifier of the instance that failed to store.
(0008,1190)|RetrieveURL|The retrieve URL of this instance on the DICOM server.

An example response with `Accept` header `application/dicom+json`:

```json
{
  "00081190":
  {
    "vr":"UR",
    "Value":["http://localhost/studies/d09e8215-e1e1-4c7a-8496-b4f6641ed232"]
  },
  "00081198":
  {
    "vr":"SQ",
    "Value":
    [{
      "00081150":
      {
        "vr":"UI","Value":["cd70f89a-05bc-4dab-b6b8-1f3d2fcafeec"]
      },
      "00081155":
      {
        "vr":"UI",
        "Value":["22c35d16-11ce-43fa-8f86-90ceed6cf4e7"]
      },
      "00081197":
      {
        "vr":"US",
        "Value":[43265]
      }
    }]
  },
  "00081199":
  {
    "vr":"SQ",
    "Value":
    [{
      "00081150":
      {
        "vr":"UI",
        "Value":["d246deb5-18c8-4336-a591-aeb6f8596664"]
      },
      "00081155":
      {
        "vr":"UI",
        "Value":["4a858cbb-a71f-4c01-b9b5-85f88b031365"]
      },
      "00081190":
      {
        "vr":"UR",
        "Value":["http://localhost/studies/d09e8215-e1e1-4c7a-8496-b4f6641ed232/series/8c4915f5-cc54-4e50-aa1f-9b06f6e58485/instances/4a858cbb-a71f-4c01-b9b5-85f88b031365"]
      }
    }]
  }
}
```

### Failure Reason Codes

Code|Description
----------|----------
272|The store transaction did not store the instance because of a general failure in processing the operation.
43265|The provided instance StudyInstanceUID did not match the specified StudyInstanceUID in the store request.
45070|A DICOM file with the same StudyInstanceUID, SeriesInstanceUID and SopInstanceUID has already been stored. If you wish to update the contents, delete this instance first.