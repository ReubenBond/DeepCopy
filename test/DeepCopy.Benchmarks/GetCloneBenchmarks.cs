using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using CloneExtensions;

namespace DeepCopy.Benchmarks
{
    public class GetCloneBenchmarks
    {
        private readonly SimpleClass _simpleClass;
        private readonly List<int> _listOfInts;
        private readonly List<SimpleClass> _listOfSimpleClassSameInstance;
        private readonly List<SimpleClass> _listOfSimpleClassDifferentInstances;
        private readonly List<SimpleStruct> _listOfSimpleStruct;

        public GetCloneBenchmarks()
        {
            this._simpleClass = new SimpleClass()
            {
                Int = 10,
                UInt = 1231,
                Long = 1231234561L,
                ULong = 1516524352UL,
                Double = 1235.1235762,
                Float = 1.333F,
                String = "Lorem ipsum ..."
            };

            this._listOfInts = Enumerable.Range(0, 10000).ToList();

            this._listOfSimpleClassSameInstance = Enumerable.Repeat(this._simpleClass, 10000).ToList();
            this._listOfSimpleClassDifferentInstances = Enumerable.Range(0, 10000).Select(x => new SimpleClass() {Int = x}).ToList();
            this._listOfSimpleStruct = Enumerable.Range(0, 10000).Select(x => new SimpleStruct() {Int = x}).ToList();
        }

        [Benchmark]
        public int SimpleClass_CloneExt()
        {
            var clone = this._simpleClass.GetClone();
            return clone.Int;
        }

        [Benchmark]
        public int ListOfInts_CloneExt()
        {
            var clone = this._listOfInts.GetClone();
            return clone.Count;
        }

        [Benchmark]
        public int ListOfSimpleClassSameInstance_CloneExt()
        {
            var clone = this._listOfSimpleClassSameInstance.GetClone();
            return clone.Count;
        }

        [Benchmark]
        public int ListOfSimpleClassDifferentInstances_CloneExt()
        {
            var clone = this._listOfSimpleClassDifferentInstances.GetClone();
            return clone.Count;
        }

        [Benchmark]
        public int ListOfStruct_CloneExt()
        {
            var clone = this._listOfSimpleStruct.GetClone();
            return clone.Count;
        }

        [Benchmark]
        public int SimpleClass_DeepCopy()
        {
            var clone = DeepCopier.Copy(this._simpleClass);
            return clone.Int;
        }

        [Benchmark]
        public int ListOfInts_DeepCopy()
        {
            var clone = DeepCopier.Copy(this._listOfInts);
            return clone.Count;
        }

        [Benchmark]
        public int ListOfSimpleClassSameInstance_DeepCopy()
        {
            var clone = DeepCopier.Copy(this._listOfSimpleClassSameInstance);
            return clone.Count;
        }

        [Benchmark]
        public int ListOfSimpleClassDifferentInstances_DeepCopy()
        {
            var clone = DeepCopier.Copy(this._listOfSimpleClassDifferentInstances);
            return clone.Count;
        }

        [Benchmark]
        public int ListOfStruct_DeepCopy()
        {
            var clone = DeepCopier.Copy(this._listOfSimpleStruct);
            return clone.Count;
        }
    }
}