# See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

# Define the "runtime" image which will run the DICOM Server
FROM mcr.microsoft.com/dotnet/aspnet:6.0.1-alpine3.14@sha256:daafe40427581dc44b6bfde1d71b1fad3d83b00f406285c3624133b59c469ab9 AS runtime
RUN set -x && \
    # See https://www.abhith.net/blog/docker-sql-error-on-aspnet-core-alpine/
    apk add --no-cache icu-libs && \
    addgroup nonroot && \
    adduser -S -D -H -s /sbin/nologin -G nonroot -g nonroot nonroot
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER nonroot

# Copy the DICOM Server project and build it
FROM mcr.microsoft.com/dotnet/sdk:6.0.102-alpine3.14@sha256:8d15f73dcd72485cc89e59cf0fa2e97ef016c4500164dd26b71cea6d532b8134 AS build
ARG BUILD_CONFIGURATION=Release
ARG CONTINUOUS_INTEGRATION_BUILD=false
WORKDIR /dicom-server
COPY . .
WORKDIR /dicom-server/src/Microsoft.Health.Dicom.Web
RUN dotnet build "Microsoft.Health.Dicom.Web.csproj" -c $BUILD_CONFIGURATION -p:ContinuousIntegrationBuild=$CONTINUOUS_INTEGRATION_BUILD -warnaserror

# Publish the DICOM Server from the build
FROM build as publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Microsoft.Health.Dicom.Web.csproj" -c $BUILD_CONFIGURATION --no-build -o /app/publish

# Copy the published application
FROM runtime AS dicom-server
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Microsoft.Health.Dicom.Web.dll"]
