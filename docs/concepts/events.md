# What are Events?

Events are a notification and subscription feature in the Azure Health Data Services. Events enable customers to utilize and enhance the analysis and workflows of structured and unstructured data like vitals and clinical or progress notes, operations data, and Internet of Medical Things (IoMT) health data. When Fast Healthcare Interoperability Resources (FHIR®) resource changes are successfully written to the Azure Health Data Services FHIR service, the Events feature sends notification messages to Events subscribers. These event notification occurrences can be sent to multiple endpoints to trigger automation ranging from starting workflows to sending email and text messages to support the changes occurring from the health data it originated from. The Events feature integrates with the Azure Event Grid service and creates a system topic for the Azure Health Data Services Workspace.

Important:

FHIR resource change data is only written and event messages are sent when the Events feature is turned on. The Event feature doesn't send messages on past FHIR resource changes or when the feature is turned off.

Tip - For more information about the features, configurations, and to learn about the use cases of the Azure Event Grid service, see Azure Event Grid

Events currently supports only the following FHIR resource operations:

FhirResourceCreated - The event emitted after a FHIR resource gets created successfully.

FhirResourceUpdated - The event emitted after a FHIR resource gets updated successfully.

FhirResourceDeleted - The event emitted after a FHIR resource gets soft deleted successfully.

For more information about the FHIR service delete types, see FHIR REST API capabilities for Azure Health Data Services FHIR service

## Scalable
Events are designed to support growth and changes in healthcare technology needs by using the Azure Event Grid service and creating a system topic for the Azure Health Data Services Workspace.

## Configurable
Choose the FHIR resources that you want to receive messages about. Use the advanced features like filters, dead-lettering, and retry policies to tune Events message delivery options.

Note - The advanced features come as part of the Event Grid service.

## Extensible
Use Events to send FHIR resource change messages to services like Azure Event Hubs or Azure Functions to trigger downstream automated workflows to enhance items such as operational data, data analysis, and visibility to the incoming data capturing near real time.

## Secure
Built on a platform that supports protected health information and customer content data compliance with privacy, safety, and security in mind, the Events messages do not transmit sensitive data as part of the message payload.

Use Azure Managed identities to provide secure access from your Event Grid system topic to the Events message receiving endpoints of your choice.

# Deploy Events using the Azure portal

In this Quickstart, you’ll learn how to deploy the Azure Health Data Services Events feature in the Azure portal to send Fast Healthcare Interoperability Resources (FHIR®) event messages.

## Prerequisites
It's important that you have the following prerequisites completed before you begin the steps of deploying the Events feature in Azure Health Data Services.

An active Azure account
Microsoft Azure Event Hubs namespace and an event hub deployed in the Azure portal
Workspace deployed in the Azure Health Data Services
FHIR service deployed in the workspace

Important: 

You will also need to make sure that the Microsoft.EventGrid resource provider has been successfully registered with your Azure subscription to deploy the Events feature. For more information, see Azure resource providers and types - Register resource provider.

Note -

For the purposes of this quickstart, we'll be using a basic Events set up and an event hub as the endpoint for Events messages. To learn how to deploy Azure Event Hubs, see Quickstart: Create an event hub using Azure portal.

## Deploy Events
1. Browse to the workspace that contains the FHIR service you want to send Events messages from and select the Events button on the left hand side of the portal.
2. Select + Event Subscription to begin the creation of an event subscription.
3. In the Create Event Subscription box, enter the following subscription information.
- Name: Provide a name for your Events subscription.
- System Topic Name: Provide a name for your System Topic.

Note -

The first time you set up the Events feature, you will be required to enter a new System Topic Name. Once the system topic for the workspace is created, the System Topic Name will be used for any additional Events subscriptions that you create within the workspace.

- Event types: Type of FHIR events to send messages for (for example: create, updated, and deleted).
- Endpoint Details: Endpoint to send Events messages to (for example: an Azure Event Hubs namespace + an event hub).

Note -

For the purposes of this quickstart, we'll use the Event Schema and the Managed Identity Type settings at their default values.

4. After the form is completed, select Create to begin the subscription creation.
5. Event messages won't be sent until the Event Grid System Topic deployment has successfully completed. Upon successful creation of the Event Grid System Topic, the status of the workspace will change from "Updating" to "Succeeded".
6. After the subscription is deployed, it will require access to your message delivery endpoint.

Tip -

For more information about providing access using an Azure Managed identity, see Assign a system-managed identity to an Event Grid system topic and Event delivery with a managed identity

For more information about managed identities, see What are managed identities for Azure resources

For more information about Azure role-based access control (Azure RBAC), see What is Azure role-based access control (Azure RBAC)

# Consume events with Logic Apps

(leave content of this page alone)

# Use Metrics

(leave content of this page alone)

# Enable diagnostic settings

(leave content of this page alone)

# Disable events and delete workspace

(leave content of this page alone)

# Events troubleshooting guide

(leave content of this page alone, except update list of event types)

# Events message structure

(leave content of this page alone)

# Frequently asked questions (FAQs) about Events

The following are some of the frequently asked questions about Events.

## Events: The basics
## Can I use Events with a different FHIR service other than the Azure Health Data Services FHIR service?
No. The Azure Health Data Services Events feature only currently supports the Azure Health Data Services Fast Healthcare Interoperability Resources (FHIR®) service.

## What FHIR resource events does Events support?
Events are generated from the following FHIR service types:

FhirResourceCreated - The event emitted after a FHIR resource gets created successfully.

FhirResourceUpdated - The event emitted after a FHIR resource gets updated successfully.

FhirResourceDeleted - The event emitted after a FHIR resource gets soft deleted successfully.

For more information about the FHIR service delete types, see FHIR REST API capabilities for Azure Health Data Services FHIR service

## What is the payload of an Events message?
For a detailed description of the Events message structure and both required and non-required elements, see Events troubleshooting guide.

## What is the throughput for the Events messages?
The throughput of FHIR events is governed by the throughput of the FHIR service and the Event Grid. When a request made to the FHIR service is successful, it will return a 2xx HTTP status code. It will also generate a FHIR resource changing event. The current limitation is 5,000 events/second per a workspace for all FHIR service instances in it.

## How am I charged for using Events?
There are no extra charges for using Azure Health Data Services Events. However, applicable charges for the Event Grid will be assessed against your Azure subscription.

## How do I subscribe to multiple FHIR services in the same workspace separately?
You can use the Event Grid filtering feature. There are unique identifiers in the event message payload to differentiate different accounts and workspaces. You can find a global unique identifier for workspace in the source field, which is the Azure Resource ID. You can locate the unique FHIR account name in that workspace in the data.resourceFhirAccount field. When you create a subscription, you can use the filtering operators to select the events you want to get in that subscription.

Screenshot of the Event Grid filters tab.

## Can I use the same subscriber for multiple workspaces or multiple FHIR accounts?
Yes. We recommend that you use different subscribers for each individual FHIR account to process in isolated scopes.

## Is Event Grid compatible with HIPAA and HITRUST compliance obligations?
Yes. Event Grid supports customer's Health Insurance Portability and Accountability Act (HIPAA) and Health Information Trust Alliance (HITRUST) obligations. For more information, see Microsoft Azure Compliance Offerings.

## What is the expected time to receive an Events message?
On average, you should receive your event message within one second after a successful HTTP request. 99.99% of the event messages should be delivered within five seconds unless the limitation of either the FHIR service or Event Grid has been met.

## Is it possible to receive duplicate Events message?
Yes. The Event Grid guarantees at least one Events message delivery with its push mode. There may be chances that the event delivery request returns with a transient failure status code for random reasons. In this situation, the Event Grid will consider that as a delivery failure and will resend the Events message. For more information, see Azure Event Grid delivery and retry.

Generally, we recommend that developers ensure idempotency for the event subscriber. The event ID or the combination of all fields in the data property of the message content are unique per each event. The developer can rely on them to de-duplicate.