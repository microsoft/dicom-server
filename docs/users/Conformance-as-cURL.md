# Conformance-as-cURL
This document uses cURL to demonstrate working with the Medical Imaging Server for DICOM.

## Uploading DICOM (STOW)
---
### Store-instances-using-multipart/related
This request intends to demonstrate how to upload DICOM files using multipart/related. However, it will not work in Postman.

> NOTE: The Medical Imaging Server for DICOM is more lenient than the DICOM standard. The example below, however, demonstrates a POST request that complies tightly to the standard.

_Details:_

* Path: ../studies
* Method: POST
* Headers:
    * `Accept: application/dicom+json`
    * `Content-Type: multipart/related; type="application/dicom"`
* Body:
    * `Content-Type: application/dicom` for each file uploaded, separated by a boundary value

`curl --location --request POST "http://{service-name}.azurewebsites.net/studies" --header "Accept: application/dicom+json" --header "Content-Type: multipart/related; type=\"application/dicom\"" --form "file1=@C:/githealth/case1_008.dcm;type=application/dicom" --trace-ascii "trace2.txt"`

---
### Store-single-instance

> NOTE: This is a non-standard API that allows the upload of a single DICOM file without the need to configure the POST for multipart/related. Although cURL handles multipart/related well, this API allows tools like Postman to upload files to the DICOMweb service.

The following is required to upload a single DICOM file.

* Path: ../studies
* Method: POST
* Headers:
   *  `Accept: application/dicom+json`
   *  `Content-Type: application/dicom`
* Body:
    * Contains a single DICOM file as bytes.

> NOTE: Not currently implemented! TODO: Implement

---

### Store-instances-for-a-specific-study

This request demonstrates how to upload DICOM files using multipart/related to a designated study. 

_Details:_
* Path: ../studies/{study}
* Method: POST
* Headers:
    * `Accept: application/dicom+json`
    * `Content-Type: multipart/related; type="application/dicom"`
* Body:
    * `Content-Type: application/dicom` for each file uploaded, separated by a boundary value

> Some programming languages and tools behave differently. For instance, some require you to define your own boundary. For those, you may need to use a slightly modified Content-Type header. The following have been used successfully.
 > * `Content-Type: multipart/related; type="application/dicom"; boundary=ABCD1234`
 > * `Content-Type: multipart/related; boundary=ABCD1234`
 > * `Content-Type: multipart/related`

If using Postman, please consider using Store-single-instance. This is a non-standard API that allows the upload of a single DICOM file without the need to configure the POST for multipart/related.

`curl --request POST "http://{service-name}.azurewebsites.net/studies/1.2.276.0.50.192168001099.7810872.14547392.270" --header "Accept: application/dicom+json" --header "Content-Type: multipart/related; type=\"application/dicom\"" --form "file1=@C:/githealth/case1_008.dcm;type=application/dicom"`

## Retrieving DICOM (WADO)
---
### Retrieve-all-instances-within-a-study

This request retrieves all instances within a single study, and returns them as a collection of multipart/related bytes.

_Details:_
* Path: ../studies/{study}
* Method: GET
* Headers:
   * `Accept: multipart/related; type="application/dicom"; transfer-syntax=*`

`curl --request GET "http://{service-name}.azurewebsites.net/studies/1.2.276.0.50.192168001099.7810872.14547392.270" --header "Accept: multipart/related; type=\"application/dicom\"; transfer-syntax=*" --output "suppressWarnings.txt"`

> This cURL command will show the downloaded bytes in the output file (suppressWarnings.txt), but these are not direct DICOM files, only a text representation of the multipart/related download.

---
### Retrieve-metadata-of-all-instances-in-study

This request retrieves the metadata for all instances within a single study.

_Details:_
* Path: ../studies/{study}/metadata
* Method: GET
* Headers:
   * `Accept: application/dicom+json`

> This cURL command will show the downloaded bytes in the output file (suppressWarnings.txt), but these are not direct DICOM files, only a text representation of the multipart/related download.

`curl --request GET "http://{service-name}.azurewebsites.net/studies/1.2.276.0.50.192168001099.7810872.14547392.270/metadata" --header "Accept: application/dicom+json"`

---
### Retrieve-all-instances-within-a-series

This request retrieves all instances within a single series, and returns them as a collection of multipart/related bytes.

_Details:_
* Path: ../studies/{study}/series{series}
* Method: GET
* Headers:
   * `Accept: multipart/related; type="application/dicom"; transfer-syntax=*`

> This cURL command will show the downloaded bytes in the output file (suppressWarnings.txt), but it is not the DICOM file, only a text representation of the multipart/related download.

`curl --request GET "http://{service-name}.azurewebsites.net/studies/1.2.276.0.50.192168001099.7810872.14547392.270/series/1.2.276.0.50.192168001099.7810872.14547392.458" --header "Accept: multipart/related; type=\"application/dicom\"; transfer-syntax=*" --output "suppressWarnings.txt"`


---
### Retrieve-metadata-of-all-instances-within-a-series

This request retrieves the metadata for all instances within a single study.

_Details:_
* Path: ../studies/{study}/metadata
* Method: GET
* Headers:
   * `Accept: application/dicom+json`

`curl --request GET "http://{service-name}.azurewebsites.net/studies/1.2.276.0.50.192168001099.7810872.14547392.270/series/1.2.276.0.50.192168001099.7810872.14547392.458/metadata" --header "Accept: application/dicom+json"`

---
### Retrieve-a-single-instance-within-a-series-of-a-study

This request retrieves a single instances, and returns it as a DICOM formatted stream of bytes.

_Details:_
* Path: ../studies/{study}/series{series}/instances/{instance}
* Method: GET
* Headers:
   * `Accept: application/dicom; transfer-syntax=*`

`curl --request GET "http://{service-name}.azurewebsites.net/studies/1.2.276.0.50.192168001099.7810872.14547392.270/series/1.2.276.0.50.192168001099.7810872.14547392.458/instances/1.2.276.0.50.192168001099.7810872.14547392.467" --header "Accept: application/dicom; transfer-syntax=*" --output "suppressWarnings.txt"`

---
### Retrieve-metadata-of-a-single-instance-within-a-series-of-a-study

This request retrieves the metadata for a single instances within a single study and series.

_Details:_
* Path: ../studies/{study}/series/{series}/instances/{instance}/metadata
* Method: GET
* Headers:
  * `Accept: application/dicom+json`

`curl --request GET "http://{service-name}.azurewebsites.net/studies/1.2.276.0.50.192168001099.7810872.14547392.270/series/1.2.276.0.50.192168001099.7810872.14547392.458/instances/1.2.276.0.50.192168001099.7810872.14547392.467/metadata" --header "Accept: application/dicom+json"`

---
### Retrieve-one-or-more-frames-from-a-single-instance

This request retrieves one or more frames from a single instance, and returns them as a collection of multipart/related bytes.

_Details:_
* Path: ../studies/{study}/series{series}/instances/{instance}
* Method: GET
* Headers:
   * `Accept: multipart/related; type="application/octet-stream"; transfer-syntax=1.2.840.10008.1.2.1` (Default) or
   * `Accept: multipart/related; type="application/octet-stream"; transfer-syntax=*` or
   * `Accept: multipart/related; type="application/octet-stream";`

`curl --request GET "http://{service-name}.azurewebsites.net/studies/1.2.276.0.50.192168001099.7810872.14547392.270/series/1.2.276.0.50.192168001099.7810872.14547392.458/instances/1.2.276.0.50.192168001099.7810872.14547392.467/frames/1" --header "Accept: multipart/related; type=\"application/octet-stream\"; transfer-syntax=1.2.840.10008.1.2.1" --output "suppressWarnings.txt"`

## Query DICOM (QIDO)
---
### Search-for-studies

This request enables searches for one or more studies by DICOM attributes.

> Please see the [Conformance.md](https://github.com/microsoft/dicom-server/blob/master/docs/users/Conformance.md) file for supported DICOM attributes.

_Details:_
* Path: ../studies?StudyInstanceUID={{study}}
* Method: GET
* Headers:
   * `Accept: application/dicom+json`

`curl --request GET "http://{service-name}.azurewebsites.net/studies?StudyInstanceUID=1.2.276.0.50.192168001099.7810872.14547392.270" --header "Accept: application/dicom+json"`

---
### Search-for-series

This request enables searches for one or more series by DICOM attributes.

> Please see the [Conformance.md](https://github.com/microsoft/dicom-server/blob/master/docs/users/Conformance.md) file for supported DICOM attributes.

_Details:_
* Path: ../series?SeriesInstanceUID={{series}}
* Method: GET
* Headers:
   * `Accept: application/dicom+json`

`curl --request GET "http://{service-name}.azurewebsites.net/series?SeriesInstanceUID=1.2.276.0.50.192168001099.7810872.14547392.458" --header "Accept: application/dicom+json"`

---
### Search-for-series-within-a-study

This request enables searches for one or more series within a single study by DICOM attributes.

> Please see the [Conformance.md](https://github.com/microsoft/dicom-server/blob/master/docs/users/Conformance.md) file for supported DICOM attributes.

_Details:_
* Path: ../studies/{{study}}/series?SeriesInstanceUID={{series}}
* Method: GET
* Headers:
   * `Accept: application/dicom+json`

`curl --request GET "http://{service-name}.azurewebsites.net/studies/1.2.276.0.50.192168001099.7810872.14547392.270/series?SeriesInstanceUID=1.2.276.0.50.192168001099.7810872.14547392.458" --header "Accept: application/dicom+json"`

---
### Search-for-instances

This request enables searches for one or more instances by DICOM attributes.

> Please see the [Conformance.md](https://github.com/microsoft/dicom-server/blob/master/docs/users/Conformance.md) file for supported DICOM attributes.

_Details:_
* Path: ../instances?SOPInstanceUID={{instance}}
* Method: GET
* Headers:
   * `Accept: application/dicom+json`

`curl --request GET "http://{service-name}.azurewebsites.net/instances?SOPInstanceUID=1.2.276.0.50.192168001099.7810872.14547392.467" --header "Accept: application/dicom+json"`

---
### Search-for-instances-within-a-study

This request enables searches for one or more instances within a single study by DICOM attributes.

> Please see the [Conformance.md](https://github.com/microsoft/dicom-server/blob/master/docs/users/Conformance.md) file for supported DICOM attributes.

_Details:_
* Path: ../studies/{{study}}/instances?SOPInstanceUID={{instance}}
* Method: GET
* Headers:
   * `Accept: application/dicom+json`

`curl --request GET "http://{service-name}.azurewebsites.net/studies/1.2.276.0.50.192168001099.7810872.14547392.270/instances?SOPInstanceUID=1.2.276.0.50.192168001099.7810872.14547392.467" --header "Accept: application/dicom+json"`

---
### Search-for-instances-within-a-study-and-series

This request enables searches for one or more instances within a single study and single series by DICOM attributes.

> Please see the [Conformance.md](https://github.com/microsoft/dicom-server/blob/master/docs/users/Conformance.md) file for supported DICOM attributes.

_Details:_
* Path: ../studies/{{study}}/series/{{series}}instances?SOPInstanceUID={{instance}}
* Method: GET
* Headers:
   * `Accept: application/dicom+json`

`curl --request GET "http://{service-name}.azurewebsites.net/studies/1.2.276.0.50.192168001099.7810872.14547392.270/series/1.2.276.0.50.192168001099.7810872.14547392.458/instances?SOPInstanceUID=1.2.276.0.50.192168001099.7810872.14547392.467" --header "Accept: application/dicom+json"`

## Delete DICOM 
---
### Delete-a-specific-instance-within-a-study -and-series

This request deletes a single instance within a single study and single series.

> Delete is not part of the DICOM standard, but has been added for convenience.

_Details:_
* Path: ../studies/{{study}}/series/{{series}}/instances/{{instance}}
* Method: DELETE
* Headers: No special headers needed

`curl --request DELETE "http://{service-name}.azurewebsites.net/studies/1.2.276.0.50.192168001099.7810872.14547392.270/series/1.2.276.0.50.192168001099.7810872.14547392.458/instances/1.2.276.0.50.192168001099.7810872.14547392.467"`

---
### Delete-a-specific-series-within-a-study

This request deletes a single series (and all child instances) within a single study.

> Delete is not part of the DICOM standard, but has been added for convenience.

_Details:_
* Path: ../studies/{{study}}/series/{{series}}
* Method: DELETE
* Headers: No special headers needed

`curl --request DELETE "http://{service-name}.azurewebsites.net/studies/1.2.276.0.50.192168001099.7810872.14547392.270/series/1.2.276.0.50.192168001099.7810872.14547392.458"`

---
### Delete-a-specific-study

This request deletes a single study (and all child series and instances).

> Delete is not part of the DICOM standard, but has been added for convenience.

_Details:_
* Path: ../studies/{{study}}
* Method: DELETE
* Headers: No special headers needed

`curl --request DELETE "http://{service-name}.azurewebsites.net/studies/1.2.276.0.50.192168001099.7810872.14547392.270"`




