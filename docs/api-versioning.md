# API Versioning for DICOM Server

This guide gives an overview of the API version policies for DICOM Server. 

All versions of the DICOM APIs will always conform to the DICOMweb™ Standard specifications, but versions may expose different APIs based on our [conformance statment](https://github.com/microsoft/dicom-server/blob/main/docs/resources/conformance-statement.md).

## Supported Versions

A list of supported versions and details of what is supported can be found in the swagger documentation at swagger/v{version}/swagger.json.

### Prerelease versions

An API version with the label "prerelease" indicates that the version is not ready for production, and should only be used in testing environments. These endpoints may experience breaking changes without notice.

### Breaking changes

We will increment the API version number for any breaking changes to the API.

Breaking changes:
1. Renaming or removing endpoints
2. Removing parameters or adding mandatory parameters
3. Changing status code
4. Deleting property in response or altering response type at all (but okay to add properties to the response)
5. Changing the type of a property
6. Behavior of an API changes (changes in buisness logic, used to do foo, now does bar)

Non-breaking changes (API is not incremented):
1. Addition of properties that are nullable or have a default value
2. Addition of properties to a response model
3. Changing the order of properties

## Headers

`ReportApiVersions` is turned on, which means we will return the headers `api-supported-versions` and `api-deprecated-versions` when appropriate.

- `api-supported-versions` will list which versions are supported for the requested API. It is only returned when calling an endpoint annotated with `[ApiVersion("<someVersion>")]`. 

- `api-deprecated-versions` will list which versions have been deprecated for the requested API. It is only returned when calling an endpoint annotated with `[ApiVersion("<someVersion>", Deprecated = true)]`.

Example:

```
[ApiVersion("1.0")]
[ApiVersion("1.0-prerelease", Deprecated = true)]
```

![Response headers](images/api-headers-example.PNG)