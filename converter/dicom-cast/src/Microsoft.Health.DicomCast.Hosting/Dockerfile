# Define the "runtime" image which will run DICOMcast
FROM mcr.microsoft.com/dotnet/aspnet:7.0.11-alpine3.17-amd64@sha256:4dba2a2a4598edc87dc662d055c6893ae7e427f7a62a5b6e400c5b863debc916 AS runtime
RUN set -x && \
    addgroup nonroot && \
    adduser -S -D -H -s /sbin/nologin -G nonroot -g nonroot nonroot
USER nonroot

# Copy the DICOMcast project and build it
FROM mcr.microsoft.com/dotnet/sdk:7.0.401-alpine3.18-amd64@sha256:3e2fe7e8cb43fcfa25f8ad6acc9f4d8be860c06631ae662416d0ef6fb87c603c AS build
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
