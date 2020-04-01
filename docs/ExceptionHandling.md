## Exception handling Guidelines

### The Dicom server code follows below pattern for raising exceptions
- All user input validation errors are thrown as DicomClient*Exception.
- Internal classes use Ensure library to validate input. Ensure library throws .Net Argument*Exception. 
- Exceptions from dependent libraries like fo-dicom are caught and wrapped in DicomClient*Exception.
- Exceptions from dependent services libraries like Azure storage blob are caught and wrapped in DicomServer*Exception.

### The Dicom server code follows below pattern for handling exceptions
- All Dicom*Exception are handled in middleware [ExceptionHandlingMiddleware](../src/Microsoft.Health.Dicom.Api/Features/Exceptions/ExceptionHandlingMiddleware.cs). These exceptions are mapped to the right status code and response body.
- All unexcepted exception are logged and mapped to 500 server error.