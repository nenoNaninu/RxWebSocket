using BenchmarkDotNet.Running;

namespace Benchmark
{
    class Program
    {

        static void Main(string[] args)
        {
            var switcher = new BenchmarkSwitcher(new[]
            {
                typeof(BinBench),
                typeof(NormalBench),
                typeof(MemoryBench)
            });

            switcher.Run(args);
        }

    }
}
