# Frequently asked questions about the Medical Imaging Server for DICOM

## What are the key requirements to use the Medical Imaging Server for DICOM?

The Medical Imaging Server for DICOM needs an Azure subscription to configure and run the required components. These components are, by default, created inside of an existing or new Azure Resource Group to simplify management. Additionally, an Azure Active Directory account is required.

## Where is the data persisted using the Medical Imaging Server for DICOM?

The customer controls all of the data persisted by the Medical Imaging Server for DICOM. The following components are used to persist data:
- Blob storage: persists all DICOM data and metadata
- Azure SQL: indexes a subset of the DICOM metadata to support queries, and maintains a queryable log of changes
- Azure Key Vault: stores critical security information

## What data formats are compatible with the Medical Imaging Server for DICOM?

The Medical Imaging Server for DICOM exposes a REST API that is compatible with the DICOMweb&trade; Standards specified by NEMA and maintained at https://www.dicomstandard.org/dicomweb/.

The server does not support DICOM DIMSE, which works primarily over a local area network and is unsuited for modern internet-based APIs. DIMSE is an incredibly popular standard used by nearly all medical imaging devices to communicate with other components of a provider’s medical imaging solution, such as PACS (Picture Archiving and Communication Systems) and medical imaging viewers. However, many modern systems, especially PACS and  viewers, have begun to also support the related (and compatible) DICOMweb&trade; Standard. For those systems which only speak DICOM DIMSE there are adapters available which allow for seamless communication between the local DIMSE-supporting systems and the Medical Imaging Server for DICOM.

## What version of DICOM does the Medical Imaging Server for DICOM support? 

The DICOM standards has been fixed at version 3.0 since 1993. However, the standard continues to add both breaking and non-breaking changes through various workgroups.

No single product, including the Medical Imaging Server for DICOM, fully supports the DICOM standard. Instead, each product includes a DICOM Conformance document that specifies exactly what is supported. (Unsupported features are traditionally not called out explicitly.) The Conformance document is available [here](conformance-statement.md).

## Does the Medical Imaging Server for DICOM store any PHI?

Absolutely. One of the core objectives for the Medical Imaging Server for DICOM is to support standard and innovating radiologist workflows. These workflows demand the use of PHI data.