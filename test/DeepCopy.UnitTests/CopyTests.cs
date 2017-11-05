using System;
using System.Collections.Generic;
using Xunit;

namespace DeepCopy.UnitTests
{
    [Trait("TestCategory", "BVT")]
    public class CopyTests
    {
        [Fact]
        public void CanCopyStrings()
        {
            var original = "hello!";
            var result = DeepCopier.Copy(original);
            Assert.Same(original, result);
        }

        [Fact]
        public void CanCopyIntegers()
        {
            var original = 123;
            var result = DeepCopier.Copy(original);
            Assert.Equal(original, result);
        }

        [Fact]
        public void CanCopyArrays()
        {
            var original = new object[] { 123, "hello!" };
            var result = DeepCopier.Copy(original);
            Assert.Equal(original, result);
            Assert.NotSame(original, result);
        }

        [Fact]
        public void CanCopyPrimitiveArrays()
        {
            var original = new int[] {1, 2, 3};
            var result = DeepCopier.Copy(original);
            Assert.Equal(original, result);
            Assert.NotSame(original, result);
        }

        [Fact]
        public void CanCopyTwoDimensionalArrays()
        {
            var original = new int[][]
            {
                new int[] {1,3,5,7,9},
                new int[] {0,2,4,6},
                new int[] {11,22}
            };
            var result = DeepCopier.Copy(original);
            Assert.Equal(original, result);
            Assert.NotSame(original, result);
        }

        [Fact]
        public void CanCopyThreeDimensionalArrays()
        {
            var original = new int[][][]
            {
                new int[][]
                {
                    new int[] {1, 2}
                },
                new int[][]
                {
                    new int[] {1, 3, 5, 7, 9},
                    new int[] {11, 22}
                },
            };
            var result = DeepCopier.Copy(original);
            Assert.Equal(original, result);
            Assert.NotSame(original, result);
        }

        [Fact]
        public void CanCopyJaggedMultidimensionalArrays()
        {
            var original = new int[3][,]
            {
                new int[,] { {1,3}, {5,7} },
                new int[,] { {0,2}, {4,6}, {8,10} },
                new int[,] { {11,22}, {99,88}, {0,9} }
            };

            var result = DeepCopier.Copy(original);
            Assert.Equal(original, result);
            Assert.NotSame(original, result);
        }

        [Fact]
        public void ImmutableTypesAreNotCopied()
        {
            var original = new ImmutablePoco { Reference = new object[] { 123, "hello!" } };
            var result = DeepCopier.Copy(original);
            Assert.Same(original.Reference, result.Reference);
            Assert.Same(original, result);
        }

        [Fact]
        public void ImmutableWrapperTypesAreNotCopied()
        {
            var original = Immutable.Create(new object[] { 123, "hello!" } );
            var result = DeepCopier.Copy(original);
            Assert.Same(original.Value, result.Value);
        }

        [Fact]
        public void CanCopyCyclicObjects()
        {
            var original = new Poco();
            original.Reference = original;

            var result = DeepCopier.Copy(original);
            Assert.NotSame(original, result);
            Assert.Same(result, result.Reference);
        }

        [Fact]
        public void ReferencesInArraysArePreserved()
        {
            var poco = new Poco();
            var original = new[] { poco, poco };

            var result = DeepCopier.Copy(original);
            Assert.NotSame(original, result);
            Assert.Same(result[0], result[1]);
        }

        [Fact]
        public void CanCopyPrivateReadonly()
        {
            var poco = new Poco();
            poco.Reference = poco;
            var original = new PocoWithPrivateReadonly(poco);

            var result = DeepCopier.Copy(original);
            Assert.NotSame(original, result);
            Assert.Same(result.GetReference(), ((Poco) result.GetReference()).Reference);
        }

        [Theory]
        [MemberData(nameof(ImmutableTestData))]
        public void CanCopyImmutables(object original)
        {
            var result = DeepCopier.Copy(original);
            Assert.Equal(result, original);
        }

        public static IEnumerable<object[]> ImmutableTestData()
        {
            yield return new object[] {5m};
            yield return new object[] {DateTime.Now};
            yield return new object[] {TimeSpan.MaxValue};
            yield return new object[] {"Hello World"};
            yield return new object[] {Guid.NewGuid()};
            yield return new object[] {DateTimeOffset.Now};
            yield return new object[] {new Version(1, 0, 0, 0)};
            yield return new object[] {new Uri("http://localhost")};
            yield return new object[] {new KeyValuePair<string, string>("Hello", "World")};
            yield return new object[] { new Tuple<int, string>(5, "hello") };
            yield return new object[] { new Tuple<int, string, double>(5, "hello", 1d) };
            yield return new object[] { new Tuple<int, string, double, int>(5, "hello", 1d, 2) };
            yield return new object[] { new Tuple<int, string, double, int, string>(5, "hello", 1d, 2, "world") };
            yield return new object[]
                {new Tuple<int, string, double, int, string, double>(5, "hello", 1d, 2, "world", 3d)};
            yield return new object[]
                {new Tuple<int, string, double, int, string, double, int>(5, "hello", 1d, 2, "world", 3d, 7)};
            yield return new object[]
            {
                new Tuple<int, string, double, int, string, double, int, Tuple<string>>(5, "hello", 1d, 2, "world", 3d, 7,
                    Tuple.Create("universe"))
            };
            yield return new object[] { new ValueTuple<int, string>(5, "hello") };
            yield return new object[] { new ValueTuple<int, string, double>(5, "hello", 1d) };
            yield return new object[] { new ValueTuple<int, string, double, int>(5, "hello", 1d, 2) };
            yield return new object[] { new ValueTuple<int, string, double, int, string>(5, "hello", 1d, 2, "world") };
            yield return new object[]
                {new ValueTuple<int, string, double, int, string, double>(5, "hello", 1d, 2, "world", 3d)};
            yield return new object[]
                {new ValueTuple<int, string, double, int, string, double, int>(5, "hello", 1d, 2, "world", 3d, 7)};
            yield return new object[]
            {
                new ValueTuple<int, string, double, int, string, double, int, ValueTuple<string>>(5, "hello", 1d, 2, "world", 3d, 7,
                    new ValueTuple<string>("universe"))
            };
        }

        [Immutable]
        private class ImmutablePoco
        {
            public object Reference { get; set; }
        }

        private class Poco
        {
            public object Reference { get; set; }
        }

        private class PocoWithPrivateReadonly
        {
            private readonly object reference;
            public PocoWithPrivateReadonly(object reference)
            {
                this.reference = reference;
            }

            public object GetReference() => this.reference;
        }
    }
}
