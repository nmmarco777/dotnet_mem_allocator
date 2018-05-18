# .net memory allocator
test project for .net memory allocation in docker  

https://hub.docker.com/r/schaefferlinkbit/dotnet_mem_allocator/

# parameters
alloc_mb | 10 | number MB allocated
sleep | 1000 | sleep milliseconds between allocations
free | true | set to true if memory should be freed
force_full_collection | false | controlling the forceFullCollection parameter of System.GC.GetTotalMemory
collect | 0 | starts GC every n cycles (cycle % collect == 0)
