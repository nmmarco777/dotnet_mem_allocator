#!/bin/bash
docker build --target app -t nmmarco/dotnet-mem-allocator:net8-servergc -f dotnet_mem_allocator_server_GC/Dockerfile .
