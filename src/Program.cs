﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace dotnet_mem_allocator;

class Program
{
    const double MB_FACTOR = 1024 * 1024;

    static void Main(string[] args)
    {
        var config = new ConfigurationBuilder().AddCommandLine(args).Build();

        // Handle Control+C or Control+Break
        Console.CancelKeyPress += (o, e) =>
        {
            Environment.Exit(0);
        };

        int iAllocBytes = config.GetValue<int>("alloc_mb", 10) * 1000000;
        int iSleepMs = config.GetValue<int>("sleep", 1000);
        bool bFree = config.GetValue<bool>("free", true);
        bool bForceFullCollection = config.GetValue<bool>("force_full_collection", false);
        int iCollect = config.GetValue<int>("collect", 0);

#if CLIENT_GC
        Console.WriteLine("server GC: false");
#endif
#if SERVER_GC
        Console.WriteLine("server GC: true");
#endif

        Console.WriteLine("allocated bytes: " + iAllocBytes);
        Console.WriteLine("sleep ms: " + iSleepMs);
        Console.WriteLine("free: " + bFree);
        Console.WriteLine("force full collection: " + bForceFullCollection);
        Console.WriteLine("run GC every: " + iCollect);

        try
        {
            var rand = new Random();
            List<byte[]> m_arrays = new List<byte[]>();

            int iLoop = 0;
            while (true)
            {
                var array = new byte[iAllocBytes];
                var value = (byte)rand.Next();
                Parallel.ForEach(
                    array,
                    (item) =>
                    {
                        item = value;
                    }
                );
                rand.NextBytes(array);

                if (!bFree)
                    m_arrays.Add(array);

                if (iCollect > 0 && iLoop % iCollect == 0)
                {
                    Console.WriteLine("run GC");
                    GC.Collect();
                }

                Console.WriteLine(AllocatedBytes() + TotalMemory() + CollectionCount());

                Thread.Sleep(iSleepMs);
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

    private static string ToMb(double value) => (value / MB_FACTOR).ToString("N1") + " MB";

    private static string AllocatedBytes() =>
        "AllocatedBytesForCurrentThread: "
        + ToMb(GC.GetAllocatedBytesForCurrentThread()).PadRight(12);

    private static string TotalMemory() =>
        "TotalMemory: " + ToMb(GC.GetTotalMemory(false)).PadRight(12);

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
