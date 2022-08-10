# Swagger

We use [Swashbuckle](https://github.com/domaindrivendev/Swashbuckle.AspNetCore) to generate Swagger/ API Documentation.
If you've never worked with Swagger before, it may be helpful to
checkout [this sample](https://github.com/dotnet/AspNetCore.Docs/tree/main/aspnetcore/tutorials/web-api-help-pages-using-swagger/samples/6.x/SwashbuckleSample)
and play with it.

A quick summary of workflow is:

- Make changes to an API or to some of the customizations or defaults we have for swagger
- Build the solution. The .dll generated is what is then used to generate the API documentation.
- Use `dotnet swagger` to generate documentation from the new .dll

The DICOM OSS project is setup to autogenerate this documentation for you on build.

## Customization and Defaulting

Swagger and customizations are
set [here](https://github.com/microsoft/dicom-server/blob/main/src/Microsoft.Health.Dicom.Api/Registration/DicomServerServiceCollectionExtensions.cs#L133)
.

We've written some customizations and added defaults.

Defaults can be seen at src/Microsoft.Health.Dicom.Api/Configs/SwaggerConfiguration.cs.
Customizations can be seen at src/Microsoft.Health.Dicom.Api/Features/Swagger.

Note that we also specify Licensing in src/Microsoft.Health.Dicom.Web/appsettings.json. If you take out the License
defaulting in SwaggerConfiguration.cs, appsettings.json will be used to get Licensing when using the post build hook.
However, these settings are not used if using `dotnet swagger` in your terminal, outside of a build. If anyone knows
why, please replace this content.

## Updating Swagger YAML

Swagger yaml will be generated for you on each build using a post build hook name `SwaggerPostBuildTarget` in
Microsoft.Health.Dicom.Web.csproj.

### Add A New Version

You can add a new version by adding a new `Exec MSBuild task` in `Microsoft.Health.Dicom.Web.csproj`.
Output should go to `swagger\<your-new-version>\swagger.yaml` and with `<your-new-version>` at the end of
the `dotnet swagger tofile` command,
specifying `name of the swagger doc you want to retrieve, as configured in your startup class`.

Example:

```
<Exec Command="dotnet swagger tofile  --yaml --output ..\..\swagger\v1\swagger.yaml $(OutputPath)\Microsoft.Health.Dicom.Web.dll v1"></Exec>
```

Be sure to also update the build/common/versioning.yml Powershell tasks to check for new versions.

### ADO Checks

We utilize [openapi-diff](https://github.com/OpenAPITools/openapi-diff) to check for differences and breaking API
changes.

#### Checks For Latest Swagger

As a way to ensure we always keep the swagger yaml up to date, there is a step in our ADO pipeline that will generate
swagger and error out if what is generated has differences from the yaml that was checked in.
This script lives in ./build/common/scripts/CheckForSwaggerChanges.ps1

You can run this script locally as well:

```
.\build\common\scripts\CheckForSwaggerChanges.ps1  -SwaggerDir 'swagger' -AssemblyDir 'src\Microsoft.Health.Dicom.Web\bin\x64\Debug\net6.0\Microsoft.Health.Dicom.Web.dll' -Version 'v1-prerelease','v1'
```

Note that this script does not use `dotnet swagger`'s comparison to detect changes as that only looks like API changes.
We want to compare the file as a whole, so we use Powershell's `Compare-Object` instead.

#### Checks for Breaking APIChanges

As a way to ensure we always consider breaking API changes, there is a step in our ADO pipeline that will error out if
what was checked in has breaking changes compared to the yaml in main branch.
This script lives in ./build/common/scripts/CheckForBreakingAPISwaggerChanges.ps1

You can run this script locally as well:

```
.\build\common\scripts\CheckForBreakingAPISwaggerChanges.ps1  -SwaggerDir 'swagger' -Version 'v1-prerelease','v1'
```

Example output with no changes:

```

Mode                 LastWriteTime         Length Name
----                 -------------         ------ ----
d-----         8/10/2022   9:22 AM                FromMain
Running comparison with baseline for version v1-prerelease
old: swagger\FromMain\v1-prerelease.yaml
new: swagger\v1-prerelease\swagger.yaml
No differences. Specifications are equivalents
Running comparison with baseline for version v1
old: swagger\FromMain\v1.yaml
new: swagger\v1\swagger.yaml
No differences. Specifications are equivalents


PS C:\dev\hls\dicom-server>
```
