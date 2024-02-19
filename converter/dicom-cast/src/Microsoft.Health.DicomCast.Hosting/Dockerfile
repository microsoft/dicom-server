# Define the "runtime" image which will run DICOMcast
FROM mcr.microsoft.com/dotnet/aspnet:8.0.2-alpine3.18-amd64@sha256:c8c3e7e5ae616527d87131e681ff5400eb2e603f5f69e485cf764f8fc1a64ac5 AS runtime
USER $APP_UID

# Copy the DICOMcast project and build it
FROM mcr.microsoft.com/dotnet/sdk:8.0.201-alpine3.18-amd64@sha256:ba7c7ce462015322e6f34e30e60055000e4e860fe805eb14623557fbe90ec60c AS build
ARG BUILD_CONFIGURATION=Release
ARG CONTINUOUS_INTEGRATION_BUILD=false
WORKDIR /dicom-server
COPY . .
WORKDIR /dicom-server/converter/dicom-cast/src/Microsoft.Health.DicomCast.Hosting/
RUN dotnet build "Microsoft.Health.DicomCast.Hosting.csproj" -c $BUILD_CONFIGURATION -p:ContinuousIntegrationBuild=$CONTINUOUS_INTEGRATION_BUILD -warnaserror

# Publish the DICOM Server from the build
FROM build as publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Microsoft.Health.DicomCast.Hosting.csproj" -c $BUILD_CONFIGURATION --no-build -o /app/publish

# Copy the published application
FROM runtime AS dicom-cast
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Microsoft.Health.DicomCast.Hosting.dll"]
