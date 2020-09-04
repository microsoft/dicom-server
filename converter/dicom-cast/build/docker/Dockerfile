# Start from the sdk image
FROM mcr.microsoft.com/dotnet/core/sdk:3.1.201-bionic AS build

ARG nugetpat=""

# Set the working directory to where the source is located
WORKDIR /src

# Copy everything
COPY ./ ./

RUN if [ -n "$nugetpat" ] ; then\
    dotnet nuget update source "Resolute" --username "useless" --password "$nugetpat" --store-password-in-clear-text ; \
    fi

# Execute a dotnet build and publish the hosting project
RUN dotnet build --configuration Release
RUN dotnet publish "./src/Microsoft.Health.DicomCast.Hosting/Microsoft.Health.DicomCast.Hosting.csproj" -c Release -o "/.build" --no-build

# Build the runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-alpine AS runtime

RUN set -x && \
    addgroup nonroot && \
    adduser -S -D -H -s /sbin/nologin -G nonroot -g nonroot nonroot

# Set the workiting directory and copy the build
WORKDIR /app
COPY --from=build .build .

# Expose and start the web app on port 8080
USER nonroot
ENTRYPOINT ["dotnet", "Microsoft.Health.DicomCast.Hosting.dll"]
