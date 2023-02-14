# Define the "runtime" image which will run DICOMcast
FROM mcr.microsoft.com/dotnet/aspnet:7.0.2-alpine3.16@sha256:78a1e45845cba24507e3422a0969832f6ae9fbd1511d2a6bbe6663b625da253f AS runtime
RUN set -x && \
    addgroup nonroot && \
    adduser -S -D -H -s /sbin/nologin -G nonroot -g nonroot nonroot
USER nonroot

# Copy the DICOMcast project and build it
FROM mcr.microsoft.com/dotnet/sdk:7.0.103-alpine3.16@sha256:891fe0ba27eadc9e761cfd83f2ef402bac4d713fd2ff7fd15d90212989c02dea AS build
ARG BUILD_CONFIGURATION=Release
ARG CONTINUOUS_INTEGRATION_BUILD=false
WORKDIR /dicom-server
COPY . .
WORKDIR /dicom-server/converter/dicom-cast/src/Microsoft.Health.DicomCast.Hosting/
RUN dotnet build "Microsoft.Health.DicomCast.Hosting.csproj" -c $BUILD_CONFIGURATION -p:ContinuousIntegrationBuild=$CONTINUOUS_INTEGRATION_BUILD -warnaserror

# Publish the DICOM Server from the build
FROM build as publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Microsoft.Health.DicomCast.Hosting.csproj" -c $BUILD_CONFIGURATION --framework net7.0 --no-build -o /app/publish

# Copy the published application
FROM runtime AS dicom-cast
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Microsoft.Health.DicomCast.Hosting.dll"]
