# Change Feed overview

Change feed provides logs of all the changes that occur in your DICOM server. The change feed provides ordered, guaranteed, immutable, read-only log of these changes. 

Client applications can read these logs at any time, either in streaming or in batch mode. The change feed enables you to build efficient and scalable solutions that process change events that occur in your dicom server.

You can process these change events asynchronously, incrementally or in-full. Any number of client applications can independently read the change feed, in parallel, and at their own pace.

## Usage

### DICOM Cast
[DICOM Cast](/converter/dicom-cast) is a stateful processor that pulls DICOM changes from change feed, transforms and publishes them to a configured FHIR service as an [ImagingStudy resource](https://www.hl7.org/fhir/imagingstudy.html).
It can start processing the DICOM change events at any point and continue to pull and process new changes incrementally.

### Other potential usage patterns

Change feed support is well-suited for scenarios that process data based on objects that have changed. For example, it can be used to:

- Build connected application pipelines like ML that react to change events or schedule executions based on created or deleted instance.

- Extract business analytics insights and metrics, based on changes that occur to your objects.

- Poll the change feed to create an event source for push notifications.
