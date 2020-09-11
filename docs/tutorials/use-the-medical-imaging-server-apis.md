# Use DICOMWeb Standard APIs with Medical Imaging Server for DICOM

This tutorial gives on overview of how to use the DICOMWeb Standard APIs with the Medical Imaging Server for DICOM.

The **Medical Imaging Server for DICOM** supports a subset of the DICOMWeb standard. Support includes:

- Store (STOW-RS)
- Retrieve (WADO-RS)
- Search (QIDO-RS)

Additionally, the following non-standard API(s) are supported:

- Delete
- Change Feed

You can learn more about our support of the various DICOM Web Standard APIs in our [Conformance Statement](../resources/conformance-statement.md).

## Prerequisites

In order to use the DICOMWeb Standard APIs, you must have an instance of the Medical Imaging Server for DICOM deployed. If you have not already deployed the Medical Imaging Server, follow the instructions [here](../quickstarts/deploy-via-azure.md).

## Overview of various methods to use with Medical Imaging Server for DICOM

Because the Medical Imaging Server for DICOM is exposed as a REST API, you can access it using any modern development language. For language-agnostic information on working with the service, please refer to our [Conformance Statement](../resources/conformance-statement.md). 

To see language-specific examples, please see the examples below. Alternatively, if you open the Postman Collection, you can see examples in several languages including Go, Java, Javascript, C#, PHP, C, NodeJS, Objective-C, OCaml, PowerShell, Python, Ruby, and Swift.

### C#

The C# examples use the library included in this repo to simplify access to the API. 

You can find the C# examples [here](tutorials/use-dicom-web-standard-apis-with-c%23.md).

### cURL

cURL is a common command line tool for calling web endpoints that is available for nearly any operating system. You can download cURL [here](https://curl.haxx.se/download.html).  To use the examples, you'll need to replace the server name with your instance name, and download the [example DICOM files](../dcms) in this repo to a known location on your local file system.

You can find the cURL examples [here](tutorials/use-dicom-web-standard-apis-with-curl.md).

### Postman

Postman is an excellent tool for designing, building and testing REST APIs. It is available [here](https://www.postman.com/downloads/). You can learn how to effectively use Postman at their [learning site](https://learning.postman.com/).

One important caveat with Postman and the DICOMweb standard: Postman cannot support uploading DICOM files using the approach defined in the DICOM standard. This is because Postman cannot support custom separators in a multipart/related POST request. For more information, please see [https://github.com/postmanlabs/postman-app-support/issues/576](https://github.com/postmanlabs/postman-app-support/issues/576) for more information on this bug. Thus all examples in the Postman collection for uploading DICOM documents are prefixed with [will not work - see description]. However, they are included for completeness.

To use the Postman collection, you'll need to download the collection locally and import the collection through Postman. 

You can find the Postman examples [here](tutorials/use-dicom-web-standard-apis-with-postman.md).
