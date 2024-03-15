# Define the "runtime" image which will run DICOMcast
FROM mcr.microsoft.com/dotnet/aspnet:8.0.3-alpine3.18-amd64@sha256:269b84c8f0704adcc8ae84f51057f9b5a768ac02965afa42e43bd3ff61de980a AS runtime
USER $APP_UID

# Copy the DICOMcast project and build it
FROM mcr.microsoft.com/dotnet/sdk:8.0.203-alpine3.18-amd64@sha256:88b2081be6c081e4f4a5cbf4d3317794ac01be3f15bc9afac4fe88c41d85521d AS build
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
