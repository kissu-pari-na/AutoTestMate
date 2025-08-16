using AutoTestMate.SampleUnderTest;
using System;
using Xunit;

namespace AutoTestMate.SampleUnderTest.Tests
{
    public class DivideTests
    {
        [Theory]
        [InlineData(10, 2, 5)]
        [InlineData(0, 5, 0)]
        [InlineData(100, 10, 10)]
        public void Divide_ValidInputs_ReturnsCorrectResult(int a, int b, int expected)
        {
            // Arrange
            var snippet = new SnippetClass();

            // Act
            int result = snippet.Divide(a, b);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(10, 0)]
        [InlineData(0,0)]
        [InlineData(-10,0)]

        public void Divide_DivideByZero_ThrowsDivideByZeroException(int a, int b)
        {
            // Arrange
            var snippet = new SnippetClass();

            // Act & Assert
            Assert.Throws<DivideByZeroException>(() => snippet.Divide(a, b));
        }


        [Fact]
        public void Divide_MaxInt_ReturnsCorrectResult()
        {
            // Arrange
            var snippet = new SnippetClass();
            int a = int.MaxValue;
            int b = 1;
            int expected = int.MaxValue;

            // Act
            int result = snippet.Divide(a, b);

            // Assert
            Assert.Equal(expected, result);
        }

        //Helper functions (replace with your actual helper functions if needed)
        public static class Helper
        {
            public static string SafeSampleArgsCsv(string types)
            {
                //This is a placeholder.  Replace with your actual CSV generation logic.
                if (types == "int,int") return "10,2;0,5;100,10;10,0;0,0;-10,0;2147483647,1";
                return ""; // Or throw an exception if types are not supported
            }

            public static string IntEdgeCasesCsv()
            {
                return "2147483647,1; -2147483648,1; 0,1; 1,1";
            }
        }
    }
}