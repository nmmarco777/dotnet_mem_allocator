# Build this Dockerfile from the root directory of the project, not from the project folder:

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
# Possible GCTYPE values: server, client
ARG GCTYPE=server

WORKDIR /src
COPY *.sln .
COPY DotnetMemAllocator/*.csproj ./DotnetMemAllocator/
RUN dotnet restore

COPY DotnetMemAllocator/. ./DotnetMemAllocator/
WORKDIR /src/DotnetMemAllocator
RUN if [ "${GC_TYPE}" = "client" ]; then \
    echo "Building with client GC"; \
    dotnet publish -c Release -o /app --no-restore -p:DefineConstants=client_gc -p:ServerGarbageCollection=false; \
    if [ $? -ne 0 ]; then exit 1; fi; \
    else \
    echo "Building with server GC"; \
    dotnet publish -c Release -o /app --no-restore -p:DefineConstants=server_gc -p:ServerGarbageCollection=true; \
    if [ $? -ne 0 ]; then exit 1; fi; \
    fi

# Force the amd64 version in case we are building on an M1 Mac
FROM mcr.microsoft.com/dotnet/runtime:8.0-bookworm-slim-amd64 AS app
WORKDIR /app
COPY --from=builder /app .
USER app
ENTRYPOINT ["dotnet", "DotnetMemAllocator.dll"]
