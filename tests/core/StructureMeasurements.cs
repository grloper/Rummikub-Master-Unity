using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;

// Optional observations of the linked production generic list. Not a Unity/game benchmark.
static class StructureMeasurements
{
    public static void Run()
    {
        var rows = new List<object>();
        foreach (int size in new[] { 1000, 10000, 100000 })
        {
            var list = new DoublyLinkedList<int>();
            for (int i = 0; i < size; i++) list.AddLast(i);
            const int queries = 200000;
            for (int i = 0; i < queries; i++) list.Contains(i % size); // JIT warmup
            var lookup = new List<double>();
            var append = new List<double>();
            for (int trial = 0; trial < 7; trial++)
            {
                int found = 0;
                var watch = Stopwatch.StartNew();
                for (int i = 0; i < queries; i++) if (list.Contains(i % size)) found++;
                watch.Stop();
                if (found != queries) throw new Exception("Measurement lookup mismatch");
                lookup.Add(watch.Elapsed.TotalMilliseconds * 1000000 / queries);
                var receiver = new DoublyLinkedList<int>();
                var donor = new DoublyLinkedList<int>();
                for (int i = 0; i < size; i++) donor.AddLast(i);
                watch.Restart(); receiver.Append(donor); watch.Stop();
                if (receiver.Count != size || donor.Count != 0) throw new Exception("Measurement transfer mismatch");
                append.Add(watch.Elapsed.TotalMilliseconds);
            }
            rows.Add(new {nodes=size, trials=7, queriesPerTrial=queries,
                containsMedianNanoseconds=lookup.OrderBy(x=>x).ElementAt(3),
                appendMedianMilliseconds=append.OrderBy(x=>x).ElementAt(3)});
        }
        Console.WriteLine(JsonSerializer.Serialize(new {
            measuredAtUtc=DateTime.UtcNow.ToString("O"), runtime=Environment.Version.ToString(),
            scope="Production DoublyLinkedList<int> linked into .NET harness; not Unity runtime, Card lifecycle, AI or gameplay latency",
            method="Seven trials after lookup warmup. Median includes loop/modulo/hash costs; append timing excludes donor construction but includes receiver index allocation and node ownership transfer. Wall-clock noise, GC and JIT affect timings. No latency threshold or asymptotic claim inferred from timings alone.", rows
        }, new JsonSerializerOptions {WriteIndented=true}));
    }
}
