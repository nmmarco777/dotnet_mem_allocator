#!/bin/bash

DOCKER_REPO=nmmarco/dotnet-mem-allocator
DOTNET_VERSION=8

while true; do
    read -p "Build with Server or Client GC? (S/c) " -r
    case $REPLY in
        [Ss]|"" ) GCTYPE=server; break;;
        [Cc] ) GCTYPE=client; break;;
        * ) echo "Please answer 's' or 'c'.";;
    esac
done

docker build \
    --progress plain \
    --target app \
    --tag ${DOCKER_REPO}:net${DOTNET_VERSION}-${GCTYPE}gc \
    --file Dockerfile \
    --build-arg GCTYPE=${GCTYPE} \
    .

if [[ $? -ne 0 ]]; then
    echo "Build failed"
    exit 1
fi

echo
while true; do
    read -p "Push the image to docker hub? (Y/n) " -r
    case $REPLY in
        [Yy]|"" ) docker push ${DOCKER_REPO}:net${DOTNET_VERSION}-${GCTYPE}gc; break;;
        [Nn] ) break;;
        * ) echo "Please answer 'y' or 'n'.";;
    esac
done
echo
