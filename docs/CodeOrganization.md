 # Code Organization

## Projects
 The codebase is designed to support different data stores, identity providers, operating systems, and is not tied to any particular cloud or hosting environment. To achieve these goals, the project is broken down into layers:

| Layer             | Example                              | Comments                                                                              |
|-------------------|--------------------------------------|---------------------------------------------------------------------------------------|
| Hosting Layer     | `Microsoft.Health.Dicom.Web`         | Supports hosting in different environments with custom configuration of IoC container. For development purpose only. |
| REST API Layer    | `Microsoft.Health.Dicom.Api`          | Implements the RESTful DICOMWeb |
| Core Logic Layer  | `Microsoft.Health.Dicom.Core`         | Implements core logic to support DICOMWeb |                                                           |
| Persistence Layer | `Microsoft.Health.Dicom.Sql` `Microsoft.Health.Dicom.Blob`     | Pluggable  persistence provider |

## Patterns

Dicom server code follows below **patterns** to organize code in these layer.


### MediatoR Handler:

<em>In-proc domain messages. Uses a mediator pattern to sync/async call the registered handlers for the specific message.</em>

Responsibility:
- Decoupling the event/message from the handler.
Currently used to dispatch message from the Controller methods. Used to transform request and response from the hosting layer to the service. Ex: [DicomDeleteHandler](../src/Microsoft.Health.Dicom.Core/Features/Delete/DicomDeleteHandler.cs)
- Naming Guidelines: Dicom`Resource`Handler

### Resource Service: 
<em>Uses Asp.net core [configuration service dependency injection](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-3.1) to handle the service instance lifetime.</em>

Responsibility:
- Used to implement businees logic. Like input validation(inline or call), Orchestration, Core response objects.
Keep the services scoped to the resource operations. Ex: [IDicomQueryService](../src/Microsoft.Health.Dicom.Core/Features/Query/IDicomQueryService.cs)
- Naming Guidelines: Dicom`Resource`Service

### Store Service:
<em>Uses Asp.net core [configuration service dependency injection](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-3.1) to handle the service instance lifetime.</em>

Responsibility:
- They are data store specific implemtation of storing/retrieving/delete the data. Interface is defined in the core and implementation in the specific persistance layer. 
They should not be accessed outside a service.
Ex: [DicomSqlIndexDataStore](../src/Microsoft.Health.Dicom.SqlServer/Features/Storage/DicomSqlIndexDataStore.cs)
- Naming Guidelines: Dicom`Resource`Store

### Middleware:
 <em>Organizing app has a pipeline of components that process the request and response. Uses Asp.net core [middleware](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-3.1).</em>

Responsibility: 
- Standard/Common concerns like authentication, routing, logging, exception handling that needs to be done for each request, are now separated to its own component. Ex: [ExceptionHandlingMiddleware](../src/Microsoft.Health.Dicom.Api/Features/Exceptions/ExceptionHandlingMiddleware.cs).
- Naming Guidelines: `Responsibility`Middleware.

### Action Filters:
<em>Code that can be executed before or after the action. Things that are specific to an action or resource can be coded here. </em>

Responsibility:
- Dicom code uses pre-action filters. Authorization filters for authentication and Custom filter for acceptable content-type validation.