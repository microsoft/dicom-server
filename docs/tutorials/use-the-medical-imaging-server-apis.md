# Use DICOMweb&trade; Standard APIs with Medical Imaging Server for DICOM

This tutorial gives on overview of how to use the DICOMweb&trade; Standard APIs with the Medical Imaging Server for DICOM.

The Medical Imaging Server for DICOM supports a subset of the DICOMweb&trade; Standard. Support includes:

- Store (STOW-RS)
- Retrieve (WADO-RS)
- Search (QIDO-RS)

Additionally, the following non-standard API(s) are supported:

- Delete
- Change Feed

You can learn more about our support of the various DICOM Web Standard APIs in our [Conformance Statement](../resources/conformance-statement.md).

## Prerequisites

In order to use the DICOMweb&trade; Standard APIs, you must have an instance of the Medical Imaging Server for DICOM deployed. If you have not already deployed the Medical Imaging Server, [Deploy the Medical Imaging Server to Azure](../quickstarts/deploy-via-azure.md).

## Overview of various methods to use with Medical Imaging Server for DICOM

Because the Medical Imaging Server for DICOM is exposed as a REST API, you can access it using any modern development language. For language-agnostic information on working with the service, please refer to our [Conformance Statement](../resources/conformance-statement.md).

To see language-specific examples, please see the examples below. Alternatively, if you open the Postman Collection, you can see examples in several languages including Go, Java, Javascript, C#, PHP, C, NodeJS, Objective-C, OCaml, PowerShell, Python, Ruby, and Swift.

### C#

The C# examples use the library included in this repo to simplify access to the API. Refer to the [C# examples](../use-dicom-web-standard-apis-with-c%23.md) to learn how to use C# with the Medical Imaging Server for DICOM.

### cURL

cURL is a common command line tool for calling web endpoints that is available for nearly any operating system. [Download cURL](https://curl.haxx.se/download.html) to get started. To use the examples, you'll need to replace the server name with your instance name, and download the [example DICOM files](../dcms) in this repo to a known location on your local file system. Refer to the [cURL examples](../tutorials/use-dicom-web-standard-apis-with-curl.md) to learn how to use cURL with the Medical Imaging Server for DICOM.

### Postman

Postman is an excellent tool for designing, building and testing REST APIs. [Download Postman](https://www.postman.com/downloads/) to get started. You can learn how to effectively use Postman at the [Postman learning site](https://learning.postman.com/).

One important caveat with Postman and the DICOMweb&trade; Standard: Postman cannot support uploading DICOM files using the approach defined in the DICOM standard. This is because Postman cannot support custom separators in a multipart/related POST request. For more information, please see [https://github.com/postmanlabs/postman-app-support/issues/576](https://github.com/postmanlabs/postman-app-support/issues/576) for more information on this bug. Thus all examples in the Postman collection for uploading DICOM documents are prefixed with [will not work - see description]. However, they are included for completeness.

To use the Postman collection, you'll need to download the collection locally and import the collection through Postman, which are available here: [Postman Collection Examples](../resources/Conformance-as-Postman.postman_collection.json).

## Summary

This tutorial provided an overview of the APIs supported by the Medical Imaging Server for DICOM. Get started using these APIs with the following tools:

- [Use DICOM Web Standard APIs with C#](../use-dicom-web-standard-apis-with-c%23.md)
- [Use DICOM Web Standard APIs with cURL](../use-dicom-web-standard-apis-with-curl.md)
- [Use DICOM Web Standard APIs with Postman Example Collection](../resources/Conformance-as-Postman.postman_collection.json)
