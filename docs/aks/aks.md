# Running DICOM Server in AKS

> **NOTE**: This doc is a TODO for now

## Infrastructure
1. Create Azure SQL database, allowing Azure Services in the Firewall settings
1. Create a KV and put the `SQLServerPassword` into a secret
1. Create an AKS cluster and ACR
1. Connect the AKS cluster to the ACR via `az cli` e.g. `az aks update -n cddicom -g cd-dicom --attach-acr cddicom`
1. Deploy an nginx ingress controller
1. Create a k8s environment in AzDO connected to the `dicom` namespace in the AKS cluster
1. Deploy FHIR to the cluster
1. Create an Azure Pipeline from `build/aks.yml` (remember to udpate values in `build/aks-variables.yml`)
