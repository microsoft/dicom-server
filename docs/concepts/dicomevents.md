# DICOM Events

Events are a notification and subscription feature in the Azure Health Data Services. Events enable customers to utilize and enhance the analysis and workflows of Digital Imaging and Communications in Medicine (DICOM) images. When DICOM image changes are successfully written to the Azure Health Data Services, the Events feature sends notification messages to Events subscribers. These event notification occurrences can be sent to multiple endpoints to trigger automation ranging from starting workflows to sending email and text messages to support the changes occurring from the health data it originated from. The Events feature integrates with the Azure Event Grid service and creates a system topic for the Azure Health Data Services Workspace.

## Event types:

DicomImageCreated - The event emitted after a DICOM image gets created successfully.

DicomImageDeleted - The event emitted after a DICOM image gets deleted successfully.

## Event message structure:

|Name | Type | Required	| Description
|-----|------|----------|-----------|
|topic	| string	| Yes	| The topic is the Azure Resource ID of your Azure Health Data Services workspace.
|subject | string | Yes | The Uniform Resource Identifier (URI) of the DICOM image that was changed. Customer can access the image with the subject with https:// scheme. Customer should use the dataVersion or data.resourceVersionId to visit specific data version regarding this event.
| eventType	| string(enum)	| Yes	| The type of change on the DICOM image.
| eventTime	| string(datetime)	| Yes	| The UTC time when the DICOM image change was committed.
| id	| string	| Yes	| Unique identifier for the event.
| data	| object	| Yes	| DICOM image change event details.
| data.imageStudyInstanceUid	| string	| Yes | The image's Study Instance UID
| data.imageSeriesInstanceUid	| string	| Yes	| The image's Series Instance UID
| data.imageSopInstanceUid	| string	| Yes	| The image's SOP Instance UID
| data.serviceHostName	| string	| Yes	| The hostname of the dicom service where the change occurred. 
| data.sequenceNumber	| int	| Yes	| The sequence number of the change in the DICOM service. Every image creation and deletion will have a unique sequence within the service. This number correlates to the sequence number of the DICOM service's Change Feed. Querying the DICOM Service Change Feed with this sequence number will give you the change that created this event.
| dataVersion	| string	| No	| The data version of the DICOM image
| metadataVersion	| string	| No	| The schema version of the event metadata. This is defined by Azure Event Grid and should be constant most of the time.

## Samples

## Microsoft.HealthcareApis.DicomImageCreated

### Event Grid Schema

```
{
  "id": "d621839d-958b-4142-a638-bb966b4f7dfd",
  "topic": "/subscriptions/{subscription-id}/resourceGroups/{resource-group-name}/providers/Microsoft.HealthcareApis/workspaces/{workspace-name}",
  "subject": "{dicom-account}.dicom.azurehealthcareapis.com/v1/studies/1.2.3.4.3/series/1.2.3.4.3.9423673/instances/1.3.6.1.4.1.45096.2.296485376.2210.1633373143.864442",
  "data": {
    "imageStudyInstanceUid": "1.2.3.4.3",
    "imageSeriesInstanceUid": "1.2.3.4.3.9423673",
    "imageSopInstanceUid": "1.3.6.1.4.1.45096.2.296485376.2210.1633373143.864442",
    "serviceHostName": "{dicom-account}.dicom.azurehealthcareapis.com",
    "sequenceNumber": 1
  },
  "eventType": "Microsoft.HealthcareApis.DicomImageCreated",
  "dataVersion": "1",
  "metadataVersion": "1",
  "eventTime": "2022-09-15T01:14:04.5613214Z"
}
```

### Cloud Events Schema

```
{
  "source": "/subscriptions/{subscription-id}/resourceGroups/{resource-group-name}/providers/Microsoft.HealthcareApis/workspaces/{workspace-name}",
  "subject": "{dicom-account}.dicom.azurehealthcareapis.com/v1/studies/1.2.3.4.3/series/1.2.3.4.3.9423673/instances/1.3.6.1.4.1.45096.2.296485376.2210.1633373143.864442",
  "type": "Microsoft.HealthcareApis.DicomImageCreated",
  "time": "2022-09-15T01:14:04.5613214Z",
  "id": "d621839d-958b-4142-a638-bb966b4f7dfd",
  "data": {
    "imageStudyInstanceUid": "1.2.3.4.3",
    "imageSeriesInstanceUid": "1.2.3.4.3.9423673",
    "imageSopInstanceUid": "1.3.6.1.4.1.45096.2.296485376.2210.1633373143.864442",
    "serviceHostName": "{dicom-account}.dicom.azurehealthcareapis.com",
    "sequenceNumber": 1
  },
  "specVersion": "1.0"
}
```

## Microsoft.HealthcareApis.DicomImageDeleted

### Event Grid Schema

```
{
  "id": "eac1c1a0-ffa8-4b28-97cc-1d8b9a0a6021",
  "topic": "/subscriptions/{subscription-id}/resourceGroups/{resource-group-name}/providers/Microsoft.HealthcareApis/workspaces/{workspace-name}",
  "subject": "{dicom-account}.dicom.azurehealthcareapis.com/v1/studies/1.2.3.4.3/series/1.2.3.4.3.9423673/instances/1.3.6.1.4.1.45096.2.296485376.2210.1633373143.864442",
  "data": {
    "imageStudyInstanceUid": "1.2.3.4.3",
    "imageSeriesInstanceUid": "1.2.3.4.3.9423673",
    "imageSopInstanceUid": "1.3.6.1.4.1.45096.2.296485376.2210.1633373143.864442",
    "serviceHostName": "{dicom-account}.dicom.azurehealthcareapis.com",
    "sequenceNumber": 2
  },
  "eventType": "Microsoft.HealthcareApis.DicomImageDeleted",
  "dataVersion": "1",
  "metadataVersion": "1",
  "eventTime": "2022-09-15T01:16:07.5692209Z"
}
```

### Cloud Events Schema

```
{
  "source": "/subscriptions/{subscription-id}/resourceGroups/{resource-group-name}/providers/Microsoft.HealthcareApis/workspaces/{workspace-name}",
  "subject": "{dicom-account}.dicom.azurehealthcareapis.com/v1/studies/1.2.3.4.3/series/1.2.3.4.3.9423673/instances/1.3.6.1.4.1.45096.2.296485376.2210.1633373143.864442",
  "type": "Microsoft.HealthcareApis.DicomImageDeleted",
  "time": "2022-09-15T01:14:04.5613214Z",
  "id": "eac1c1a0-ffa8-4b28-97cc-1d8b9a0a6021",
  "data": {
    "imageStudyInstanceUid": "1.2.3.4.3",
    "imageSeriesInstanceUid": "1.2.3.4.3.9423673",
    "imageSopInstanceUid": "1.3.6.1.4.1.45096.2.296485376.2210.1633373143.864442",
    "serviceHostName": "{dicom-account}.dicom.azurehealthcareapis.com",
    "sequenceNumber": 2
  },
  "specVersion": "1.0"
}
```

## FAQs

### Can I use Events with a different DICOM service other than the Azure Health Data Services DICOM service?
No. The Azure Health Data Services Events feature only currently supports the Azure Health Data Services DICOM service.

### What DICOM image events does Events support?
Events are generated from the following DICOM service types:

DicomImageCreated - The event emitted after a DICOM image gets created successfully.

DicomImageDeleted - The event emitted after a DICOM image gets deleted successfully.

### What is the payload of an Events message?
For a detailed description of the Events message structure and both required and non-required elements, see the `Event message structure` section.

### What is the throughput for the Events messages?
The throughput of DICOM events is governed by the throughput of the DICOM service and the Event Grid. When a request made to the DICOM service is successful, it will return a 2xx HTTP status code. It will also generate a DICOM image changing event. The current limitation is 5,000 events/second per a workspace for all DICOM service instances in it.

### How am I charged for using Events?
There are no extra charges for using Azure Health Data Services Events. However, applicable charges for the Event Grid will be assessed against your Azure subscription.

### How do I subscribe to multiple DICOM services in the same workspace separately?
You can use the Event Grid filtering feature. There are unique identifiers in the event message payload to differentiate different accounts and workspaces. You can find a global unique identifier for workspace in the source field, which is the Azure Resource ID. You can locate the unique DICOM account name in that workspace in the `data.serviceHostName` field. When you create a subscription, you can use the filtering operators to select the events you want to get in that subscription.

### Can I use the same subscriber for multiple workspaces or multiple DICOM accounts?
Yes. We recommend that you use different subscribers for each individual DICOM account to process in isolated scopes.

### Is Event Grid compatible with HIPAA and HITRUST compliance obligations?
Yes. Event Grid supports customer's Health Insurance Portability and Accountability Act (HIPAA) and Health Information Trust Alliance (HITRUST) obligations. For more information, see Microsoft Azure Compliance Offerings.

### What is the expected time to receive an Events message?
On average, you should receive your event message within ten seconds after a successful HTTP request. 99.99% of the event messages should be delivered within twenty seconds unless the limitation of either the DICOM service or Event Grid has been met.

### Is it possible to receive duplicate Events message?
Yes. The Event Grid guarantees at least one Events message delivery with its push mode. There may be chances that the event delivery request returns with a transient failure status code for random reasons. In this situation, the Event Grid will consider that as a delivery failure and will resend the Events message. For more information, see Azure Event Grid delivery and retry.

Generally, we recommend that developers ensure idempotency for the event subscriber. The event ID or the combination of all fields in the data property of the message content are unique per each event. The developer can rely on them to de-duplicate.
