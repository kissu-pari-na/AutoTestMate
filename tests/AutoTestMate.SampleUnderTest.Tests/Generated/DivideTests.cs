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
        [InlineData(15, 3, 5)]
        public void Divide_ValidInputs_ReturnsCorrectResult(int a, int b, int expected)
        {
            // Arrange
            var snippet = new SnippetClass();

            // Act
            var result = snippet.Divide(a, b);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(10,0)]
        [InlineData(5,0)]
        [InlineData(0,0)]

        public void Divide_DivideByZero_ThrowsException(int a, int b)
        {
            // Arrange
            var snippet = new SnippetClass();

            // Act & Assert
            Assert.Throws<DivideByZeroException>(() => snippet.Divide(a, b));
        }


        [Fact]
        public void Divide_MaxValues_ReturnsCorrectResult()
        {
            // Arrange
            var snippet = new SnippetClass();
            int a = int.MaxValue;
            int b = 2;
            int expected = int.MaxValue / 2;

            // Act
            var result = snippet.Divide(a, b);

            // Assert
            Assert.Equal(expected, result);

        }
    }
}