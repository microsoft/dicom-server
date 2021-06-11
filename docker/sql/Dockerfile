# Dockerfile based on twright-msft's Dockerfile here:
# https://github.com/Microsoft/mssql-docker/blob/master/linux/preview/examples/mssql-agent-fts-ha-tools/Dockerfile

# Instructions for Ubuntu 20 (Focal) based on Microsoft documentation:
# https://docs.microsoft.com/en-us/sql/linux/quickstart-install-connect-ubuntu?view=sql-server-ver15

# Base Image uses Ubuntu 20 (Focal)
FROM ubuntu:20.04@sha256:adf73ca014822ad8237623d388cedf4d5346aa72c270c5acc01431cc93e18e2d

# Install SQL Server 2019 and after its prerequisites
RUN export DEBIAN_FRONTEND=noninteractive && \
    apt-get update && \
    apt-get install -yq curl apt-transport-https gnupg && \
    # Get official Microsoft repository configuration
    curl https://packages.microsoft.com/keys/microsoft.asc | apt-key add - && \
    curl https://packages.microsoft.com/config/ubuntu/20.04/mssql-server-2019.list | tee /etc/apt/sources.list.d/mssql-server.list && \
    # Install SQL Server
    apt-get update && \
    apt-get install -y mssql-server && \
    # Install Full Text Search (FTS)
    apt-get install -y mssql-server-fts && \
    # Clean up the Dockerfile
    apt-get clean && \
    rm -rf /var/lib/apt/lists

EXPOSE 1433

# Run SQL Server process
CMD /opt/mssql/bin/sqlservr
