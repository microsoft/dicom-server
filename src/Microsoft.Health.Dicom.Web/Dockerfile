#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

# Define the "runtime" image which will run the DICOM Server
FROM mcr.microsoft.com/dotnet/aspnet:5.0.6-alpine3.12@sha256:717a278e712e7b97a0c1021304e95cc95f8690c355b3cad19077ddccb3a83846 AS runtime
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
FROM mcr.microsoft.com/dotnet/sdk:5.0.300-alpine3.12@sha256:cd03cb780dc61a1ef6e450a9e7025aa34c15a2c03e8a62a5dcfe7f490f7d2905 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /dicom-server
COPY . .
WORKDIR /dicom-server/src/Microsoft.Health.Dicom.Web
RUN dotnet build "Microsoft.Health.Dicom.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the DICOM Server from the build
FROM build as publish
RUN dotnet publish "Microsoft.Health.Dicom.Web.csproj" -c "$BUILD_CONFIGURATION" -o /app/publish --no-build

# Copy the published application
FROM runtime AS dicom-server
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Microsoft.Health.Dicom.Web.dll"]
