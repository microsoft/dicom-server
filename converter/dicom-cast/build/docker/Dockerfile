# Start from the sdk image
FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine3.13@sha256:14cc4b76c0c06ec6fc58270662105c790da59e01600d60f741eea7920c023c96 AS build

# Set the working directory to where the source is located
WORKDIR /dicom-cast/src

# Copy everything
COPY ./ ./

# Execute a dotnet build and publish the hosting project
RUN dotnet build "./converter/dicom-cast/src/Microsoft.Health.DicomCast.Hosting/Microsoft.Health.DicomCast.Hosting.csproj" --configuration Release -warnaserror
RUN dotnet publish "./converter/dicom-cast/src/Microsoft.Health.DicomCast.Hosting/Microsoft.Health.DicomCast.Hosting.csproj" -c Release -o "/.build" --no-build

# Build the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:5.0.11-alpine3.13@sha256:2b67665d62471d30de99340361d516d4555a9276cb4ff78fb09db784384b1574 AS runtime

RUN set -x && \
    addgroup nonroot && \
    adduser -S -D -H -s /sbin/nologin -G nonroot -g nonroot nonroot

# Set the workiting directory and copy the build
WORKDIR /app
COPY --from=build .build .

# Expose and start the web app on port 8080
USER nonroot
ENTRYPOINT ["dotnet", "Microsoft.Health.DicomCast.Hosting.dll"]
