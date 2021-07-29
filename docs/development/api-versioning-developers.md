# API Versioning for DICOM Server - Developer Guide

This guide gives an overview of the API versioning of the REST endpoints for DICOM Server.

## Routes

API Version number are set within the route. Example:
`/v1.0-prerelease/studies`

To add a route, use the `[VersionedRoute]` attribute to automatically add the version number to the route. Example:
```C#   
[HttpPost]
[VersionedRoute("studies")]
public async Task<IActionResult> PostAsync(string studyInstanceUid = null)
```

## Incrementing the version

We will only increment the major version of the API, and leave the minor version at 0. Ex: 1.0, 2.0, 3.0, etc.

### Breaking change
The major version must be incremented if a breaking change is introduced.

List of things we will consider to be a breaking change
1. Renaming or removing endpoints
1. Removing parameters or adding mandatory parameters
1. Changing status code
1. Deleting property in response or altering response type at all (but okay to add properties to the response)
1. Changing the type of a property
1. Behavior of an API changes (changes in buisness logic, used to do foo, now does bar)

More info on breaking changes from the [REST guidelines](https://github.com/Microsoft/api-guidelines/blob/master/Guidelines.md#123-definition-of-a-breaking-change)

Additive changes are not considered breaking changes. For example, adding a response field or adding a new route.

Bug fixes are not considered breaking changes.

### Prerelease versions

Adding a version with the status "prerelease" is a good idea if you have breaking changes to add that are still prone to change, or are not production ready. 
Prerelease versions may experience breaking changes and are not recommended for customers to use in production environments.

`[ApiVersion("x.0-prerelease")]`

or

`ApiVersion prereleaseVersion = new ApiVersion(x, 0, "prerelease");`

### Testing for breaking changes
Currently we have a test in our pr and ci pipeline that checks to make sure that any defined api versions do not have any breaking changes (changes that are not backward compatible). We use [OpenAPI-diff](https://github.com/OpenAPITools/openapi-diff) to compare a baseline OpenApi Doc for each version with a version that is generated after the build step in the pipeline. If there are breaking changes detected between the baseline that is checked into the repo and the OpenApi doc generated in the pipeline, then the pipeline fails. 

### How to increment the version

1. Add a new controller to hold the endpoints for the new version, and annotate with `[ApiVersion("<desiredVersion>")]`. All existing endpoints must get the new version.
2. Add the new version number to `test/Microsoft.Health.Dicom.Web.Tests.E2E/Rest/VersionAPIData.cs` to test the new endpoints.
3. Test to verify the breaking changes were not added to the previous version(s).
4. Do the following to add the checks in the pr and ci pipeline to verify that developers do not accidentally create breaking changes.
    1. Add the new version to the arguments in `build/versioning.yml`. The powershell script takes in an array of versions so the new version can just be added to the argument.
    1. Generate the yaml file for the new version and save it to `/dicom-server/swagger/{Version}/swagger.yaml`. This will allow us to use this as the new baseline to compare against in the pr and ci pipelines to make sure there are no breaking changes introduced accidentally. The step needs to only be done once for each new version, however if the version is still in development then it can be updated multiple times.
5. Update the index.html file in the electron tool `tools\dicom-web-electron\index.html` to allow for the user to select the new version. 

## Deprecation

We can deprecate old versions by marking the version as deprecated as follows:
```c#
[ApiVersion("2.0")]
[ApiVersion("1.0", Deprecated = true)]
```

TBD: When to deprecate and when to retire old versions

## Communicating changes to customers
TBD: if process is needed for developers to document their changes to communicate to customers
