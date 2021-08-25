# Link dicom to adx

## Create Database
create adx cluster
go to cluster
create database

## Create tables

click on database & 
execute below lines

```sql
.create table DicomRaw (Instance: dynamic )

.create table DicomRaw ingestion json mapping 'DicomInstanceMapping' '[{"column":"Instance","Properties":{"path":"$"}}]'

.create function ExpandDicom() {
    DicomRaw
    | project
        dicom = Instance,
        PatientID=Instance["00100020"]["Value"][0],
        URI=strcat("https://dicomcastfhir-dicom.azurewebsites.net/studies/", Instance["0020000D"]["Value"][0], "/series/", Instance["0020000E"]["Value"][0], "/instances/", Instance["00080018"]["Value"][0])
}

.create table Dicom (dicom: dynamic, PatientID: dynamic, URI: string)

.alter table Dicom policy update @'[{"Source": "DicomRaw", "Query": "ExpandDicom()", "IsEnabled": "True"}]'
```

## Link blob to adx table

click on Connections and add the connector for blob use the mapping DicomInstanceMapping

| table | value |
| --- | --- |
| filter - prefix | `/blobServices/default/containers/metadatacontainer`|
| table Name | DicomRaw |
| DataFormat | MultiLine JSON |
| MappingName | DicomInstanceMapping |


https://docs.microsoft.com/en-us/azure/data-explorer/ingest-json-formats?tabs=kusto-query-language#ingest-json-records-containing-arrays

