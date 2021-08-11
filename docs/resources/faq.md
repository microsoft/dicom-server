# Frequently asked questions about the Medical Imaging Server for DICOM

## What is the Medical Imaging Server for DICOM?

The Medical Imaging Server for DICOM is an open source DICOMweb&trade; server that is easily deployed on Azure. It allows standards-based communication with any DICOMweb&trade;  enabled systems for data exchange, and introduces integration between DICOM and FHIR.  The server identifies and extracts DICOM metadata and injects it into a FHIR endpoint (such as the Azure API for FHIR or FHIR Server for Azure) to create a holistic view of patient data.

## What are the key requirements to use the Medical Imaging Server for DICOM?

The Medical Imaging Server for DICOM needs an Azure subscription to configure and run the required components. These components are, by default, created inside of an existing or new Azure Resource Group to simplify management. Additionally, an Azure Active Directory account is required.

## Where is the data persisted using the Medical Imaging Server for DICOM?

The customer controls all of the data persisted by the Medical Imaging Server for DICOM. The following components are used to persist data:
- Blob storage: persists all DICOM data and metadata
- Azure SQL: indexes a subset of the DICOM metadata to support queries, and maintains a queryable log of changes
- Azure Key Vault: stores critical security information

## What data formats are compatible with the Medical Imaging Server for DICOM?

The Medical Imaging Server for DICOM exposes a REST API that is compatible with the [DICOMweb&trade; Standards](https://www.dicomstandard.org/dicomweb/)specified and maintained by NEMA.

The server does not support DICOM DIMSE, which works primarily over a local area network and is unsuited for modern internet-based APIs. DIMSE is an incredibly popular standard used by nearly all medical imaging devices to communicate with other components of a provider’s medical imaging solution, such as PACS (Picture Archiving and Communication Systems) and medical imaging viewers. However, many modern systems, especially PACS and  viewers, have begun to also support the related (and compatible) DICOMweb&trade; Standard. For those systems which only speak DICOM DIMSE there are adapters available which allow for seamless communication between the local DIMSE-supporting systems and the Medical Imaging Server for DICOM.

## What version of DICOM does the Medical Imaging Server for DICOM support?

The DICOM standards has been fixed at version 3.0 since 1993. However, the standard continues to add both breaking and non-breaking changes through various workgroups.

No single product, including the Medical Imaging Server for DICOM, fully supports the DICOM standard. Instead, each product includes a DICOM Conformance document that specifies exactly what is supported. (Unsupported features are traditionally not called out explicitly.) The Conformance document is available [here](conformance-statement.md).

## What is REST API versioning in the Medical Imaging Server for DICOM?

The Medical Imaging Server for DICOM has versions for the REST API used to access the server. The version is specified as a URL path in the requests. For more information visit the [Api Versioning Documentation](../api-versioning.md).

## Does the Medical Imaging Server for DICOM store any PHI?

Absolutely. One of the core objectives for the Medical Imaging Server for DICOM is to support standard and innovating radiologist workflows. These workflows demand the use of PHI data.

## How does the Medical Imaging Server for DICOM maitain privacy and security?

Trust, data privacy, and security are the highest priority for Microsoft and remain fundamental to managing PHI data in the cloud. The Medical Imaging Server for DICOM is designed to support security and privacy. The OSS code maps structured metadata from DICOM images to the FHIR data framework which allows for downstream exchange of data via FHIR APIS.

Microsoft Azure offers a comprehensive set of offerings to help your organization comply with national, regional, and industry-specific requirements governing the collection and use of data. [Learn more about Azure compliance](https://azure.microsoft.com/overview/trusted-cloud/compliance/).

## What is DICOM?

DICOM (Digital Imaging and Communications in Medicine) is the international standard to transmit, store, retrieve, print, process, and display medical imaging information, and is the primary medical imaging standard accepted across healthcare. Although some exceptions exist (dentistry, veterinary), nearly all medical specialties, equipment manufacturers, software vendors and individual practitioners rely on DICOM at some stage of any medical workflow involving imaging. DICOM ensures that medical images meet quality standards, so that the accuracy of diagnosis can be preserved. Most imaging modalities, including CT, MRI and ultrasound must conform to the DICOM standards. Images that are in the DICOM format need to be accessed and used through specialized DICOM applications.


## What is the difference between Retrieve, Query & Store?

Query, Retrieve, and store are standard DICOMweb&trade; verbs. Query (QIDO) searches for DICOM objects. QIDO enables you to search for studies, series and instances by patient ID. Retrieve (WADO) enables you to retrieve specific studies, series and instances by reference. Store (STOW-RS) enables you to store specific instances to a DICOM server.

You can learn more about the specifics of QIDO, WADO and STOW from [DICOMweb&trade;](https://www.dicomstandard.org/dicomweb).
