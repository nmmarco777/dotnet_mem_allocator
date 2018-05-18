FROM gitlab.linkbit.io:25000/linkbit/dotnet-docker/dotnet:2.1-runtime-02ae576

WORKDIR /app

COPY . ./

EXPOSE 7777

ENTRYPOINT ["dotnet", "dotnet_mem_allocator_server_GC.dll"]
