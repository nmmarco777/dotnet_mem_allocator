using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace dotnet_mem_allocator;

static class Program
{
    const int MB_FACTOR = 1024 * 1024;

    static async Task Main(string[] args)
    {
        var config = new ConfigurationBuilder().AddCommandLine(args).Build();

        // Handle Control+C or Control+Break
        Console.CancelKeyPress += (o, e) =>
        {
            Environment.Exit(0);
        };

        Console.WriteLine("Waiting 5 seconds for the kubectl tty to attach...");
        await Task.Delay(5000);

        // CLI Parameters
        long maxAlloc = config.GetValue<long>("max_alloc", 10 * MB_FACTOR);
        long minAlloc = config.GetValue<long>("min_alloc", 10);
        long maxSize = config.GetValue<long>("max_size", 500 * MB_FACTOR);
        int largeAllocPct = config.GetValue("large_alloc_pct", 2);
        int sleepMs = config.GetValue("sleep", 250);
        bool leak = config.GetValue("leak", false);
        bool forceFullCollection = config.GetValue("force_full_collection", false);
        int collect = config.GetValue("collect", 0);

        // Print Execution Parameters
#if CLIENT_GC
        Console.WriteLine("Server GC: false");
#endif
#if SERVER_GC
        Console.WriteLine("Server GC: true");
#endif

        Console.WriteLine("Max allocated bytes: " + HumanReadableSize(maxAlloc));
        Console.WriteLine("Min allocated bytes: " + HumanReadableSize(minAlloc));
        Console.WriteLine("Max total in-use bytes: " + HumanReadableSize(maxSize));
        Console.WriteLine("Simulate a memory leak: " + leak);
        Console.WriteLine("Sleep ms: " + sleepMs.ToString("N0"));
        Console.WriteLine("Force full collection: " + forceFullCollection);
        if (collect > 0)
        {
            Console.WriteLine("Run GC every X cycles: " + collect);
        }
        Console.WriteLine("\npress Ctrl+C to exit\n");

        // Keep track of bytes allocated and deallocated in the loop
        long trackedTotalMemory = 0;

        try
        {
            var rand = new Random();
            List<byte[]> storedArrays = new List<byte[]>();

            // If we're manually forcing GC every so many iterations we need to track the loop count
            int iLoop = 0;
            while (true)
            {
                Console.Write(iLoop.ToString().PadRight(6));

                // Allocate some memory
                var allocationSize = AllocationSize(
                    minAlloc,
                    maxAlloc,
                    maxSize,
                    largeAllocPct,
                    rand
                );
                trackedTotalMemory += allocationSize;
                Console.Write(Attempting(allocationSize, trackedTotalMemory));
                byte[] array;
                try
                {
                    array = new byte[allocationSize];
                }
                catch (OutOfMemoryException)
                {
                    trackedTotalMemory -= allocationSize;
                    Console.WriteLine("Out of memory exception");
                    continue;
                }

                rand.NextBytes(array);

                // Maybe (if we're not simulating a leak) deallocate some memory
                long deallocated = 0;
                if (!leak)
                {
                    for (int i = 0; i < storedArrays.Count; i++)
                    {
                        if (rand.Next(0, 100) < 20)
                        {
                            deallocated += storedArrays[i].Length;
                            storedArrays.RemoveAt(i);
                        }
                    }
                }

                // Keep track of how much memory we are using and add the new array to the list
                // so it doesn't get garbage collected too soon.
                trackedTotalMemory -= deallocated;
                storedArrays.Add(array);

                // If we're manually forcing a full GC, do it now
                if (collect > 0 && iLoop % collect == 0)
                {
                    Console.WriteLine("run GC");
                    GC.Collect();
                }

                // Write some statistics to the console
                Console.WriteLine(
                    TotalMemory()
                        + TrackedMemory(trackedTotalMemory)
                        + Allocated(array)
                        + Deallocated(deallocated)
                        + GCCount()
                );

                // Wait, then repeat
                await Task.Delay(sleepMs);
                iLoop++;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception: " + ex.ToString());
        }
    }

    private static long AllocationSize(
        long minAlloc,
        long maxAlloc,
        long maxSize,
        int largeAllocPct,
        Random rand
    )
    {
        const int logBase = 1024;

        // Take us half way to the max size (our possibly unenforced memory limit)
        if (rand.Next(0, 100) < largeAllocPct)
        {
            return Math.Max(0, (maxSize / 2) - GC.GetTotalMemory(false));
        }

        int minExponent = (int)Math.Floor(Math.Log(minAlloc, logBase));
        int maxExponent = (int)Math.Floor(Math.Log(maxAlloc, logBase));
        int exponent = rand.Next(minExponent, maxExponent + 1);

        int root = rand.Next(0, 100) + 1;

        return root * (long)Math.Pow(logBase, exponent);
    }

    private static string Attempting(long size, long total) =>
        "Attempting: "
        + (HumanReadableSize(size, 0) + " / " + HumanReadableSize(total)).PadRight(21);

    private static string TotalMemory() =>
        "GC Total Memory: " + HumanReadableSize(GC.GetTotalMemory(false)).PadRight(12);

    private static string TrackedMemory(long trackedTotalMemory) =>
        "Tracked Memory: " + HumanReadableSize(trackedTotalMemory).PadRight(12);

    private static string Allocated(byte[] array) =>
        "Allocated: " + HumanReadableSize(array.Length).PadRight(12);

    private static string Deallocated(long deallocated) =>
        "Deallocated: " + HumanReadableSize(deallocated).PadRight(12);

    private static string HumanReadableSize(long size, int precision = 2)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = size;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        var formattedLength = len.ToString("N" + precision);

        return formattedLength + " " + sizes[order];
    }

    private static string GCCount()
    {
        var output = new StringBuilder();

        for (int i = 0; i < GC.MaxGeneration; i++)
        {
            output.Append(("Gen" + i + " GCs: " + GC.CollectionCount(i).ToString()).PadRight(16));
        }

        return output.ToString();
    }
}
