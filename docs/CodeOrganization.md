 # Code Organization

 The codebase is designed to support different data stores, identity providers, operating systems, and is not tied to any particular cloud or hosting environment. To achieve these goals, the project is broken down into layers:

| Layer             | Example                              | Comments                                                                              |
|-------------------|--------------------------------------|---------------------------------------------------------------------------------------|
| Hosting Layer     | `Microsoft.Health.Dicom.Web`          | Supports hosting in different environments with custom configuration of IoC container |
| REST API Layer    | `Microsoft.Health.Dicom.Api`          | Implements the RESTful DICOMWeb                                                   |
| Core Logic Layer  | `Microsoft.Health.Dicom.Core`         | Implements core DICOMWeb logic                                                            |
| Persistence Layer | `Microsoft.Health.Dicom.Sql` `Microsoft.Health.Dicom.Blob`     | Pluggable  persistence provider                                                        |

# Patterns

Dicom server code follows below **patterns** to organize code in these layer.


#### MediatoR Handler:

<em>In-proc domain messages. Uses a mediator pattern to sync/async call the registered handlers for the specific message.

Pros:
- Decoupling the event/message from the handler.
- Multiple handlers</em>


> Currently used to dispatch message from the Controller methods. Used to transform request and response from the hosting layer to the service. Ex: DicomDeleteHandler

>Naming Guidelines: Dicom`Resource`Handler

#### Resource Service: 
<em>Asp.net core service.

Pros:
- Dependency injection to support easy re- configurations of the app
- Instance lifetime handling
</em>

>Used to implement businees logic. Like input validation(inline or call), Orchestration, Core response objects.
Keep the services scoped to the resource operations. Ex: IDicomQueryService

>Naming Guidelines: Dicom`Resource`Service

#### Store Service:
<em>Asp.net core service.</em>

>They are data store specific implemtation of storing/retrieving/delete the data. Interface is defined in the core and implementation in the specific persistance layer. 
They should not be accessed outside a service.
Ex: DicomSqlIndexDataStore

>Naming Guidelines: Dicom`Resource`Store

#### Middleware:
 <em>Organizing app has a pipeline of components that process the request and response

Pros: 
- Standard/Common concerns like auth, routing, logging, exception handling that needs to be done for each request, are now separated to its own component
- Easy re-configuration of app 
</em>

>Middlerware is used to handle exceptions.

#### Action Filters:
<em>Code that can be executed before or after the action. Things that are specific to an action or resource can be coded here. </em>

>Dicom code uses pre-action filters. Authorization filters for authentication and Custom filter for acceptable content-type validation.