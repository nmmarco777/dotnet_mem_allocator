# Container Memory Performance Lab

## Compare Server GC...
```sh
kubectl \
    -n cplc \
    run dotnet-mem-allocator \
    --rm -i --tty \
    --image='x' \
    --overrides='{"apiVersion":"v1","spec":{"containers":[{"name":"dotnet-mem-allocator","image":"nmmarco/dotnet-mem-allocator:net8-servergc","imagePullPolicy":"Always","resources":{"limits":{"memory":"700Mi"}}}]}}'
```

## ... versus Client GC
```sh
kubectl \
    -n cplc \
    run dotnet-mem-allocator \
    --rm -i --tty \
    --image='x' \
    --overrides='{"apiVersion":"v1","spec":{"containers":[{"name":"dotnet-mem-allocator","image":"nmmarco/dotnet-mem-allocator:net8-clientgc","imagePullPolicy":"Always","resources":{"limits":{"memory":"700Mi"}}}]}}'
```

## Bump into dotnet System.OutOfMemoryException
```sh
kubectl \
    -n cplc \
    run dotnet-mem-allocator \
    --rm -i --tty \
    --image='x' \
    --overrides='{"apiVersion":"v1","spec":{"containers":[{"name":"dotnet-mem-allocator","image":"nmmarco/dotnet-mem-allocator:net8-servergc","imagePullPolicy":"Always","resources":{"limits":{"memory":"100Mi"}}}]}}'
```

## Get OOMKilled
```sh
kubectl \
    -n cplc \
    run dotnet-mem-allocator \
    -i --tty \
    --image='x' \
    --overrides='{"apiVersion":"v1","spec":{"containers":[{"name":"dotnet-mem-allocator","image":"nmmarco/dotnet-mem-allocator:net8-servergc","imagePullPolicy":"Always","restartPolicy":"Never","resources":{"limits":{"memory":"100Mi"}},"env":[{"name":"DOTNET_GCHeapHardLimit","value":"1F400000"}]}]}}'

kubectl -n cplc delete pod dotnet-mem-allocator
```

## No Limits -- Go rogue and get evicted!
```sh
kubectl \
    -n cplc \
    run dotnet-mem-allocator \
    -i --tty \
    --image='x' \
    --overrides='{"apiVersion":"v1","spec":{"containers":[{"name":"dotnet-mem-allocator","image":"nmmarco/dotnet-mem-allocator:net8-servergc","imagePullPolicy":"Always","command":["dotnet"],"args":["DotnetMemAllocator.dll","--max_size=4294967296","--max_alloc=524288000","--large_alloc_pct=10"]}]}}'

kubectl -n cplc delete pod dotnet-mem-allocator
```
