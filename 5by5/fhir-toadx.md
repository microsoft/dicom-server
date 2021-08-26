# Manual steps to setup ADX and its schema

## Steps

- Create ADX Cluster
- Create ADX DB
- Run the schema script ADX to create the schema
- Create a Blob storage account for the destination of FHIR resources
    - Create containers `fhirpatient`, `fhirimagingstudy`, `fhirconsent`, `fhirdiagnosticreport`
- When deploying Dicom-Cast ACI set `5by5FhirBlobEndpoint` to the blob access key created in previous step.
- Go to the ADX DB portal and add `Data connections` for each of the container to table for BlobCreated event.

## Schema
``` script
// Create table command
////////////////////////////////////////////////////////////
.create table ['Patient']  
(['fullUrl']:string, ['id']:guid, ['gender']:string, ['birthDate']:datetime ,['identifier']:dynamic, ['address']:dynamic, ['name']:dynamic, ['full_record']:dynamic)

// Create mapping command
////////////////////////////////////////////////////////////
.create-or-alter table ['Patient'] ingestion json mapping "Patient_mapping"
'['
'    { "column" : "fullUrl", "Properties":{"Path":"$.fullUrl"}},'
'    { "column" : "id", "Properties":{"Path":"$.resource.id"}},'
'    { "column" : "gender", "Properties":{"Path":"$.resource.gender"}},'
'    { "column" : "birthDate", "Properties":{"Path":"$.resource.birthDate"}},'
'    { "column" : "identifier", "Properties":{"Path":"$.resource.identifier"}},'
'    { "column" : "address", "Properties":{"Path":"$.resource.address"}},'
'    { "column" : "name", "Properties":{"Path":"$.resource.name"}},'
'    { "column" : "full_record", "Properties":{"Path":"$.resource"}},'
']'

.create table ['ImagingStudy']  
(['fullUrl']:string, ['id']:guid, ['identifier']:dynamic, ['patientreference']:string, ['studydate']:datetime, ['studydescription']:string, ['full_record']:dynamic)

// Create mapping command
////////////////////////////////////////////////////////////
.create-or-alter table ['ImagingStudy'] ingestion json mapping "ImagingStudy_mapping"
'['
'    { "column" : "fullUrl", "Properties":{"Path":"$.fullUrl"}},'
'    { "column" : "id", "Properties":{"Path":"$.resource.id"}},'
'    { "column" : "patientreference", "Properties":{"Path":"$.resource.subject.reference"}},'
'    { "column" : "identifier", "Properties":{"Path":"$.resource.identifier"}},'
'    { "column" : "studydate", "Properties":{"Path":"$.resource.started"}},'
'    { "column" : "studydescription", "Properties":{"Path":"$.resource.note"}},'
'    { "column" : "full_record", "Properties":{"Path":"$.resource"}},'
']'

.create table ['ConsentRaw']  
(['fullUrl']:string, ['id']:guid, ['identifier']:dynamic, ['patientreference']:string, ['start']:datetime, ['end']:datetime, ['full_record']:dynamic)

.create-or-alter table ['ConsentRaw'] ingestion json mapping "ConsentRaw_mapping"
'['
'    { "column" : "fullUrl", "Properties":{"Path":"$.fullUrl"}},'
'    { "column" : "id", "Properties":{"Path":"$.resource.id"}},'
'    { "column" : "identifier", "Properties":{"Path":"$.resource.identifier"}},'
'    { "column" : "patientreference", "Properties":{"Path":"$.resource.patient.reference"}},'
'    { "column" : "start", "Properties":{"Path":"$.resource.provision.period.start"}},'
'    { "column" : "end", "Properties":{"Path":"$.resource.provision.period.end"}},'
'    { "column" : "full_record", "Properties":{"Path":"$.resource"}},'
']'

.create table ['Consent']  
(['fullUrl']:string, ['id']:guid, ['identifier']:dynamic, ['patientreference']:string, ['start']:datetime, ['end']:datetime, ['consentcode']:string)

.create function ConsentExpand() {
    ConsentRaw
    | mv-expand actions = full_record.provision.action
    | mv-expand codings = actions["coding"]
    | project
        fullUrl,
        id,
        identifier,
        patientreference,
        start,
        end,
        consentcode = tostring(codings["code"])
}

.alter table Consent policy update @'[{"Source": "ConsentRaw", "Query": "ConsentExpand()", "IsEnabled": "True"}]'


.create table ['DiagnosticReport']  
(['fullUrl']:string, ['id']:guid, ['identifier']:dynamic, ['patientreference']:string, ['issued']:datetime, ['conclusion']:string, ['full_record']:dynamic)

.create-or-alter table ['DiagnosticReport'] ingestion json mapping "DiagnosticReport_mapping"
'['
'    { "column" : "fullUrl", "Properties":{"Path":"$.fullUrl"}},'
'    { "column" : "id", "Properties":{"Path":"$.resource.id"}},'
'    { "column" : "identifier", "Properties":{"Path":"$.resource.identifier"}},'
'    { "column" : "patientreference", "Properties":{"Path":"$.resource.subject.reference"}},'
'    { "column" : "issued", "Properties":{"Path":"$.resource.issued"}},'
'    { "column" : "conclusion", "Properties":{"Path":"$.resource.conclusion"}},'
'    { "column" : "full_record", "Properties":{"Path":"$.resource"}},'
']'

```