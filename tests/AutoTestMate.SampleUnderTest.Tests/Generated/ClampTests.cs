using System;
using System.Collections.Generic;
using Xunit;

namespace AutoTestMate.SampleUnderTest.Tests
{
    public class ClampTests
    {
        // Helper functions to generate test data.  These would typically reside in a separate file.
        public static class Helper
        {
            public static List<object[]> SafeSampleArgsCsv(string types)
            {
                //In a real-world scenario, this would read from a CSV or other data source.
                //This example provides a small, hardcoded sample.
                return new List<object[]>
                {
                    new object[] { 5, 0, 10 },
                    new object[] { 15, 0, 10 },
                    new object[] { -5, 0, 10 },
                    new object[] { 5, 10, 0 }, //Error case: min > max
                    new object[] { int.MinValue, int.MinValue, int.MaxValue },
                    new object[] { int.MaxValue, int.MinValue, int.MaxValue },
                    new object[] { 0, 0, 0 }
                };
            }

            public static List<object[]> IntEdgeCasesCsv()
            {
                return new List<object[]>
                {
                    new object[] { int.MinValue, int.MinValue, 0 },
                    new object[] { int.MaxValue, 0, int.MaxValue },
                    new object[] { int.MinValue, 0, int.MaxValue }
                };
            }
        }


        [Theory]
        [MemberData(nameof(Helper.SafeSampleArgsCsv), MemberType = typeof(Helper))]
        public void Clamp_ValidInput_ReturnsClampedValue(int value, int min, int max)
        {
            //Arrange
            var snippet = new AutoTestMate.Snippets.SnippetClass();

            //Act
            var result = snippet.Clamp(value, min, max);

            //Assert
            if (min <= value && value <= max)
            {
                Assert.Equal(value, result);
            }
            else if (value < min)
            {
                Assert.Equal(min, result);
            }
            else
            {
                Assert.Equal(max, result);
            }
        }


        [Theory]
        [MemberData(nameof(Helper.IntEdgeCasesCsv), MemberType = typeof(Helper))]
        public void Clamp_EdgeCases_ReturnsExpectedValue(int value, int min, int max)
        {
            //Arrange
            var snippet = new AutoTestMate.Snippets.SnippetClass();

            //Act
            var result = snippet.Clamp(value, min, max);

            //Assert - Assertions will vary depending on expected behavior for edge cases.
            //This example assumes clamping to min or max as appropriate.  Adjust as needed.

            if (min <= value && value <= max)
            {
                Assert.Equal(value, result);
            }
            else if (value < min)
            {
                Assert.Equal(min, result);
            }
            else
            {
                Assert.Equal(max, result);
            }
        }

        [Fact]
        public void Clamp_MinGreaterThanMax_ThrowsException()
        {
            //Arrange
            var snippet = new AutoTestMate.Snippets.SnippetClass();

            //Act & Assert
            Assert.Throws<ArgumentException>(() => snippet.Clamp(5, 10, 0));
        }
    }
}