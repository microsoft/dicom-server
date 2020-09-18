 # Code Organization

## Projects
 The codebase is designed to support different data stores, identity providers, operating systems, and is not tied to any particular cloud or hosting environment. To achieve these goals, the project is broken down into layers:

| Layer              | Example                                                      | Comments                                                                              |
| ------------------ | ------------------------------------------------------------ |---------------------------------------------------------------------------------------|
| Hosting Layer      | `Microsoft.Health.Dicom.Web`                                 | Supports hosting in different environments with custom configuration of IoC container. For development purpose only. |
| REST API Layer     | `Microsoft.Health.Dicom.Api`                                 | Implements the RESTful DICOMweb&trade; |
| Core Logic Layer   | `Microsoft.Health.Dicom.Core`                                | Implements core logic to support DICOMweb&trade; |
| Persistence Layer  | `Microsoft.Health.Dicom.Sql` `Microsoft.Health.Dicom.Blob`   | Pluggable persistence provider |

## Patterns

Dicom server code follows below **patterns** to organize code in these layer.


### [MediatoR Handler](https://github.com/jbogard/MediatR):

<em>Used to dispatch message from the Controller methods. Used to transform request and response from the hosting layer to the service.</em>

- Naming Guidelines: `Resource`Handler
-  Example: [DeleteHandler](/src/Microsoft.Health.Dicom.Core/Features/Delete/DeleteHandler.cs)

### [Resource Service](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-3.1): 
<em>Used to implement business logic. Like input validation(inline or call), orchestration, or core response objects.</em>

- Keep the services scoped to the resource operations.
- Naming Guidelines: `Resource`Service
-  Example: [IQueryService](/src/Microsoft.Health.Dicom.Core/Features/Query/IQueryService.cs)

### [Store Service](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-3.1):
<em>Data store specific implementation of storing/retrieving/deleting the data.</em>

- Interface is defined in the core and implementation in the specific persistence layer.
- They should not be accessed outside a service.
- Naming Guidelines: `Resource`Store
- Example: [SqlIndexDataStore](/src/Microsoft.Health.Dicom.SqlServer/Features/Store/SqlIndexDataStore.cs)

### [Middleware](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-3.1):
 <em>Standard/Common concerns like authentication, routing, logging, exception handling that needs to be done for each request, are separated to its own component.</em>

- Naming Guidelines: `Responsibility`Middleware.
- Example: [ExceptionHandlingMiddleware](/src/Microsoft.Health.Dicom.Api/Features/Exceptions/ExceptionHandlingMiddleware.cs).

### [Action Filters](https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/filters?view=aspnetcore-3.1):
<em>Dicom code uses pre-action filters. Authorization filters for authentication and Custom filter for acceptable content-type validation.</em>

- Naming Guidelines: `Responsibility`FilterAttribute.
- Example: [AcceptContentFilterAttribute](/src/Microsoft.Health.Dicom.Api/Features/Filters/AcceptContentFilterAttribute.cs).
