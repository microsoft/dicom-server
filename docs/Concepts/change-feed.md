# Change feed overview

Change feed provides logs of all the changes that occur in your dicom server. The change feed provides ordered, guaranteed, immutable, read-only log of these changes. 

Client applications can read these logs at any time, either in streaming or in batch mode. The change feed enables you to build efficient and scalable solutions that process change events that occur in your dicom server.

You can process these change events asynchronously, incrementally or in-full. Any number of client applications can independently read the change feed, in parallel, and at their own pace.

## Usage

### Dicom cast usage
[Dicom cast]((../../converter/dicom-cast)) is a statefull processor that pulls dicom changes from change feed, transforms and publishes them to a configured Fhir service as [ImagingStudy resource](https://www.hl7.org/fhir/imagingstudy.html).
It is implemented to start processing the dicom change events at any point and continue to pull and process new changes incrementally.

### Other potential usage

Change feed support is well-suited for scenarios that process data based on objects that have changed. For example, applications can:

- Build connected application pipelines like ML that react to change events or schedule executions based on created or deleted instance.

- Extract business analytics insights and metrics, based on changes that occur to your objects.

