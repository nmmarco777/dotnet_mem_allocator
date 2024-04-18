# Build this Dockerfile from the root directory of the project, not from the project folder:
# docker build --target app -t docker.host:net8-servergc -f dotnet_mem_allocator_server_GC/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /src
COPY ./ .
RUN dotnet restore .
WORKDIR /src/dotnet_mem_allocator_server_GC
RUN dotnet publish "dotnet_mem_allocator_server_GC.csproj" -c Release -o /app/publish


FROM mcr.microsoft.com/dotnet/runtime:8.0 AS app
WORKDIR /app
COPY --from=builder /app/publish .
USER app
ENTRYPOINT ["dotnet", "dotnet_mem_allocator_server_GC.dll"]
