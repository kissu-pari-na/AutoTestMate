using AutoTestMate.SampleUnderTest;
using System;
using Xunit;

namespace AutoTestMate.SampleUnderTest.Tests
{
    public class ClampTests
    {
        [Theory]
        [InlineData(5, 0, 10, 5)] // within range
        [InlineData(12, 0, 10, 10)] // above range
        [InlineData(-2, 0, 10, 0)] //below range

        public void Clamp_ValidInput_ReturnsClampedValue(int value, int min, int max, int expected)
        {
            //Arrange
            var sut = new SnippetClass(); // Assuming SnippetClass contains the Clamp method

            //Act
            var result = sut.Clamp(value, min, max);

            //Assert
            Assert.Equal(expected, result);
        }


        [Theory]
        [MemberData(nameof(Helper.IntEdgeCasesCsv))]
        public void Clamp_EdgeCases_ReturnsClampedValue(int value, int min, int max, int expected)
        {
            //Arrange
            var sut = new SnippetClass();

            //Act
            var result = sut.Clamp(value, min, max);

            //Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Clamp_MinGreaterThanMax_ThrowsArgumentException()
        {
            //Arrange
            var sut = new SnippetClass();

            //Act & Assert
            Assert.Throws<ArgumentException>(() => sut.Clamp(5, 10, 0));
        }
    }

    public class Helper
    {
        public static TheoryData<int, int, int, int> IntEdgeCasesCsv()
        {
            return new TheoryData<int, int, int, int>
            {
                { int.MinValue, int.MinValue, int.MaxValue, int.MinValue },
                { int.MaxValue, int.MinValue, int.MaxValue, int.MaxValue },
                { 0, int.MinValue, int.MaxValue, 0},
                { 0, 0, 0, 0}
            };
        }

        public static TheoryData<int, int, int> SafeSampleArgsCsv(string types)
        {
            //In a real-world scenario, you would parse the CSV and generate test data dynamically.
            return new TheoryData<int, int, int>
            {
                {5, 0, 10},
                {12, 0, 10},
                {-2, 0, 10}
            };

        }

    }
}