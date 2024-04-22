#!/bin/bash

while true; do
    read -p "Run with Server or Client GC? (S/c) " -r
    case $REPLY in
        [Ss]|"" ) dotnet run --project DotnetMemAllocator -p:DefineConstants=server_gc -p:ServerGarbageCollection=true; break;;
        [Cc] ) dotnet run --project DotnetMemAllocator -p:DefineConstants=client_gc -p:ServerGarbageCollection=false; break;;
        * ) echo "Please answer 's' or 'c'.";;
    esac
done
