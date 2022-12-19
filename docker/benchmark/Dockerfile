# Copy the DICOM Server project and build the benchmark
FROM mcr.microsoft.com/dotnet/sdk:7.0.100-alpine3.16@sha256:7608d66e56af46b09033b269fef6f048a4b58d50aa54b857471ec0d0194c59f9 AS sdk
RUN set -x && \
    # See https://www.abhith.net/blog/docker-sql-error-on-aspnet-core-alpine/
    apk add --no-cache icu-libs && \
    addgroup nonroot && \
    adduser -S -D -H -s /sbin/nologin -G nonroot -g nonroot nonroot
ENV LANG=en_US.UTF-8 \
    LC_ALL=en_US.UTF-8
WORKDIR /dicom-server
COPY . .
WORKDIR /dicom-server/src/Microsoft.Health.Dicom.Benchmark
RUN dotnet publish "Microsoft.Health.Dicom.Benchmark.csproj" -c Release -p:ContinuousIntegrationBuild=false -warnaserror -o /dicom-server/src/Microsoft.Health.Dicom.Benchmark/publish
WORKDIR /dicom-server/src/Microsoft.Health.Dicom.Benchmark/publish
ENTRYPOINT ["dotnet", "Microsoft.Health.Dicom.Benchmark.dll"]
