# Benchmarking

Our benchmarks are written using [BenchmarkDotNet](https://benchmarkdotnet.org/articles/overview.html) and can be run via [Docker](https://www.docker.com/) or directly from the EXE. Remember that benchmarks must be run with the Release configurtion. All benchmarks are included in the project [`Microsoft.Health.Dicom.Benchmark`](../../src/Microsoft.Health.Dicom.Benchmark/).

## Running Benchmarks

The easiest way to run the benchmarks is to run them via docker, as you'll likely need to pass in connection information benchmarks via environment variables.

### Docker
First build the image:
```bash
docker build -f ./docker/benchmark/Dockerfile -t dicom-benchmark .
```

The run the image (in the below example, the container has access to 1 core):
```bash
docker run -e BlobStore__ConnectionString='<connection-string>' -e SqlServer__ConnectionString='<connection-string>' -e DicomClient__BaseAddress='<dicom base address>' -a stdin -a stdout -a stderr --rm --privileged --cpus=1 --cpuset-cpus='0' --name benchmark dicom-benchmark
```

Feel free to customize the resources used by docker based on your scenario.
