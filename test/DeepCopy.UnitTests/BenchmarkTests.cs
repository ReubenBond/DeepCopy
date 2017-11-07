using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace DeepCopy.UnitTests
{
    [Trait("TestCategory", "BenchmarkBVT")]
    public class BenchmarkTests
    {
        private readonly SimpleClass _simpleClass;
        private readonly List<int> _listOfInts;
        private readonly List<SimpleClass> _listOfSimpleClassSameInstance;
        private readonly List<SimpleClass> _listOfSimpleClassDifferentInstances;
        private readonly List<SimpleStruct> _listOfSimpleStruct;

        public BenchmarkTests()
        {
            this._simpleClass = new SimpleClass()
            {
                Int = 10,
                UInt = 1231,
                Long = 1231234561L,
                ULong = 1516524352UL,
                Double = 1235.1235762,
                Float = 1.333F,
                String = "Lorem ipsum ...",
            };

            this._listOfInts = Enumerable.Range(0, 10000).ToList();

            this._listOfSimpleClassSameInstance = Enumerable.Repeat(this._simpleClass, 10000).ToList();
            this._listOfSimpleClassDifferentInstances = Enumerable.Range(0, 10000).Select(x => new SimpleClass() {Int = x}).ToList();
            this._listOfSimpleStruct = Enumerable.Range(0, 10000).Select(x => new SimpleStruct() {Int = x}).ToList();
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



        [Fact]
        public int SimpleClass_DeepCopy()
        {
            var clone = DeepCopier.Copy(this._simpleClass);
            return clone.Int;
        }

        [Fact]
        public int ListOfInts_DeepCopy()
        {
            var clone = DeepCopier.Copy(this._listOfInts);
            return clone.Count;
        }

        [Fact]
        public int ListOfSimpleClassSameInstance_DeepCopy()
        {
            var clone = DeepCopier.Copy(this._listOfSimpleClassSameInstance);
            return clone.Count;
        }

        [Fact]
        public int ListOfSimpleClassDifferentInstances_DeepCopy()
        {
            var clone = DeepCopier.Copy(this._listOfSimpleClassDifferentInstances);
            return clone.Count;
        }

        [Fact]
        public int ListOfStruct_DeepCopy()
        {
            var clone = DeepCopier.Copy(this._listOfSimpleStruct);
            return clone.Count;
        }
    }
}