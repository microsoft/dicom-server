# Define the "runtime" image which will run DICOMcast
FROM mcr.microsoft.com/dotnet/aspnet:6.0.7-alpine3.16@sha256:28d95bfe423e5db1d050eff6177dd816a8afa2a161697194fb64310940db6cb0 AS runtime
RUN set -x && \
    addgroup nonroot && \
    adduser -S -D -H -s /sbin/nologin -G nonroot -g nonroot nonroot
USER nonroot

# Copy the DICOMcast project and build it
FROM mcr.microsoft.com/dotnet/sdk:6.0.302-alpine3.16@sha256:e0575d5756d32a4679f0cdec32a6fac22cbd43352ffe8e52c1620a34d9386a9d AS build
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
