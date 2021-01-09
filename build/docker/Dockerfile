# Start from the sdk image
FROM mcr.microsoft.com/dotnet/sdk:5.0.101@sha256:b77f3a3dc3db717405b1c98c2917a14bee4cdd19b36c2d7b477d7596f644f3cd AS build

# Set teh working directory to where the source is located
WORKDIR /src

# Copy everything
COPY ./ ./

# Execute a dotnet build and publish the web project
RUN dotnet build --configuration Release
RUN dotnet publish "./src/Microsoft.Health.Dicom.Web/Microsoft.Health.Dicom.Web.csproj" -c Release -o "/build" --no-build

# Build the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:5.0-alpine@sha256:6a5df395273307a9188e83e67a78417b3bfdfa7465449c551ea1b33ac525950c AS runtime

RUN set -x && \
    # See https://www.abhith.net/blog/docker-sql-error-on-aspnet-core-alpine/
    apk add --no-cache icu-libs && \
    addgroup nonroot && \
    adduser -S -D -H -s /sbin/nologin -G nonroot -g nonroot nonroot

# Set environment variables
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    ASPNETCORE_URLS=http://+:8080

# Set the workiting directory and copy the build
WORKDIR /app
COPY --from=build /build .

# Expose and start the web app on port 8080
USER nonroot
EXPOSE 8080
ENTRYPOINT ["dotnet", "Microsoft.Health.Dicom.Web.dll"]
