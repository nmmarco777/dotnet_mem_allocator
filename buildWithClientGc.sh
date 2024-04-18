#!/bin/bash
docker build --target app -t nmmarco/dotnet-mem-allocator:net8-clientgc -f dotnet_mem_allocator_client_GC/Dockerfile .
