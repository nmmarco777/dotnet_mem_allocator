using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace dotnet_mem_allocator;

static class Program
{
    const int MB_FACTOR = 1024 * 1024;

    static void Main(string[] args)
    {
        var config = new ConfigurationBuilder().AddCommandLine(args).Build();

        // Handle Control+C or Control+Break
        Console.CancelKeyPress += (o, e) =>
        {
            Environment.Exit(0);
        };

        var maxAlloc = config.GetValue("max_alloc", 10 * MB_FACTOR);
        var minAlloc = config.GetValue("min_alloc", 10);
        var maxSize = config.GetValue("max_size", 500 * MB_FACTOR);
        var sleepMs = config.GetValue("sleep", 250);
        var leak = config.GetValue("leak", false);
        var forceFullCollection = config.GetValue("force_full_collection", false);
        var collect = config.GetValue("collect", 0);

#if CLIENT_GC
        Console.WriteLine("server GC: false");
#endif
#if SERVER_GC
        Console.WriteLine("server GC: true");
#endif

        Console.WriteLine("Max allocated bytes: " + maxAlloc.ToString("N0"));
        Console.WriteLine("Min allocated bytes: " + minAlloc.ToString("N0"));
        Console.WriteLine("Max total in-use bytes: " + maxSize.ToString("N0"));
        Console.WriteLine("Simulate a memory leak: " + leak);
        Console.WriteLine("Sleep ms: " + sleepMs.ToString("N0"));
        Console.WriteLine("Force full collection: " + forceFullCollection);
        if (collect > 0)
        {
            Console.WriteLine("Run GC every X cycles: " + collect);
        }
        Console.WriteLine("\npress Ctrl+C to exit\n");

        long trackedTotalMemory = 0;

        try
        {
            var rand = new Random();
            List<byte[]> storedArrays = new List<byte[]>();

            int iLoop = 0;
            while (true)
            {
                var allocationSize = AllocationSize(minAlloc, maxAlloc, maxSize, rand);
                trackedTotalMemory += allocationSize;
                var array = new byte[allocationSize];
                var value = (byte)rand.Next();
                Parallel.ForEach(
                    array,
                    (item) =>
                    {
                        item = value;
                    }
                );
                rand.NextBytes(array);

                long deallocated = 0;
                if (!leak)
                {
                    for (int i = 0; i < storedArrays.Count; i++)
                    {
                        if (rand.Next(0, 100) > 80)
                        {
                            deallocated += storedArrays[i].Length;
                            storedArrays.RemoveAt(i);
                        }
                    }
                }
                trackedTotalMemory -= deallocated;

                storedArrays.Add(array);

                if (collect > 0 && iLoop % collect == 0)
                {
                    Console.WriteLine("run GC");
                    GC.Collect();
                }

                Console.WriteLine(
                    TotalMemory()
                        + TrackedMemory(trackedTotalMemory)
                        + Allocated(array)
                        + Deallocated(deallocated)
                        + CollectionCount()
                );

                Thread.Sleep(sleepMs);
                iLoop++;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception: " + ex.ToString());
        }

        //Console.WriteLine("parameters:");
        //Console.WriteLine("\talloc_mb: number of MB allocated, default 10MB");
        //Console.WriteLine("\tfree: if memory should be deallocated, default true");
        //Console.WriteLine("\tsleep: number of ms to sleep, default 1000 ms");
    }

    private static long AllocationSize(int minAlloc, int maxAlloc, long maxSize, Random rand)
    {
        const int logBase = 1024;

        if (rand.Next(0, 100) < 2)
        {
            return Math.Max(0, (maxSize / 2) - GC.GetTotalMemory(false));
        }

        int minExponent = (int)Math.Floor(Math.Log(minAlloc, logBase));
        int maxExponent = (int)Math.Floor(Math.Log(maxAlloc, logBase));
        int exponent = rand.Next(minExponent, maxExponent + 1);

        int root = rand.Next(0, 100) + 1;

        return root * (long)Math.Pow(logBase, exponent);
    }

    private static string TotalMemory() =>
        "GC Total Memory: " + HumanReadableSize(GC.GetTotalMemory(false)).PadRight(12);

    private static string TrackedMemory(long trackedTotalMemory) =>
        "Tracked Memory: " + HumanReadableSize(trackedTotalMemory).PadRight(12);

    private static string Allocated(byte[] array) =>
        "Allocated: " + HumanReadableSize(array.Length).PadRight(12);

    private static string Deallocated(long deallocated) =>
        "Deallocated: " + HumanReadableSize(deallocated).PadRight(12);

    private static string HumanReadableSize(long size)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = size;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:N2} {sizes[order]}";
    }

    private static string CollectionCount()
    {
        var output = new StringBuilder();

        for (int i = 0; i < GC.MaxGeneration; i++)
        {
            output.Append(("Gen" + i + " GCs: " + GC.CollectionCount(i).ToString()).PadRight(16));
        }

        return output.ToString();
    }
}
