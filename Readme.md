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
Note that there is a memory limit of 100MB but the app is designed to try to use
500 MB. This means that some operations are going to fail with a
`System.OutOfMemoryException` because .NET knows about the memory limit and
won't allow the allocation.
```sh
kubectl \
    -n cplc \
    run dotnet-mem-allocator \
    --rm -i --tty \
    --image='x' \
    --overrides='{"apiVersion":"v1","spec":{"containers":[{"name":"dotnet-mem-allocator","image":"nmmarco/dotnet-mem-allocator:net8-servergc","imagePullPolicy":"Always","resources":{"limits":{"memory":"100Mi"}}}]}}'
```

## Get OOMKilled
See: https://learn.microsoft.com/en-us/dotnet/core/runtime-config/garbage-collector#heap-limit

Note that there is a memory limit of 100MB but the app is designed to try to use
500 MB. By overriding the internal .NET behavior and setting the heap hard limit
above the Kubernetes memory limit (`1F400000` = 500MB) we can simulate running
over the memory limit and Kubernetes should forcibly kill the process with an
OOMKilled exit code.

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
There are two failure states here: sometimes this can be killed by the OOMKiller
and sometimes it will be evicted from the node because the node runs out of
memory before the OOMKiller kicks in.
```sh
kubectl \
    -n cplc \
    run dotnet-mem-allocator \
    -i --tty \
    --image='x' \
    --overrides='{"apiVersion":"v1","spec":{"containers":[{"name":"dotnet-mem-allocator","image":"nmmarco/dotnet-mem-allocator:net8-servergc","imagePullPolicy":"Always","command":["dotnet"],"args":["DotnetMemAllocator.dll","--max_size=4294967296","--max_alloc=524288000","--large_alloc_pct=10"]}]}}'

kubectl -n cplc delete pod dotnet-mem-allocator
```
