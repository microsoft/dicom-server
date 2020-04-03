## Exception handling Guidelines

### The Dicom server code follows below pattern for raising exceptions
- All exceptions thrown in the dicom-server code inherit from base type DicomServerException.
- All user input validation errors throw a derived exception from DicomValidationException.
- Internal classes use Ensure library to validate input. Ensure library throws .Net Argument*Exception. 
- Exceptions from dependent libraries like fo-dicom are caught and wrapped in exception inherited from DicomServerException.
- Exceptions from dependent services libraries like Azure storage blob are caught and wrapped in exception inherited from DicomServerException.

### The Dicom server code follows below pattern for handling exceptions
- All DicomServerExceptions are handled in middleware [ExceptionHandlingMiddleware](../src/Microsoft.Health.Dicom.Api/Features/Exceptions/ExceptionHandlingMiddleware.cs). These exceptions are mapped to the right status code and response body.
- All unexcepted exceptions are logged and mapped to 500 server error.
