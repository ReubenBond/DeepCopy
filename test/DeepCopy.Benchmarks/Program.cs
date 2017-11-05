using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;

namespace DeepCopy.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = ManualConfig.Create(DefaultConfig.Instance).With(MemoryDiagnoser.Default);
            BenchmarkRunner.Run<GetCloneBenchmarks>(config);
        }
    }

    public class SimpleClassBase
    {
        public int BaseInt { get; set; }
    }

    public class SimpleClass : SimpleClassBase
    {
        public int Int { get; set; }
        public uint UInt { get; set; }
        public long Long { get; set; }
        public ulong ULong { get; set; }
        public double Double { get; set; }
        public float Float { get; set; }
        public string String { get; set; }
    }

    public struct SimpleStruct
    {
        public int Int { get; set; }
        public uint UInt { get; set; }
        public long Long { get; set; }
    }
}
