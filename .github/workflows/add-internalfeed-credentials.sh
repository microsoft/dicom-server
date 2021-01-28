# DESCRIPTION:
# Adds credentials for internal feed nuget packages

set -exo pipefail
sed -i 's;</packageSources>;</packageSources><packageSourceCredentials><Internalfeed><add key="Username" value="any" /><add key="ClearTextPassword" value="$(System.AccessToken)" /></Internalfeed></packageSourceCredentials>;' nuget.config
