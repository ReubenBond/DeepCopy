using System;
using Xunit;

namespace DeepCopy.UnitTests
{
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
