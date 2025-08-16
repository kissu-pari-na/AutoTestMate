using AutoTestMate.SampleUnderTest;
using System;
using Xunit;

namespace AutoTestMate.SampleUnderTest.Tests
{
    public class ClassifyGradeTests
    {
        [Theory]
        [InlineData(90, "A")]
        [InlineData(80, "B")]
        [InlineData(70, "C")]
        [InlineData(60, "D")]
        public void ClassifyGrade_ValidScore_ReturnsCorrectGrade(int score, string expectedGrade)
        {
            // Arrange
            var snippet = new SnippetClass();

            // Act
            string actualGrade = snippet.ClassifyGrade(score);

            // Assert
            Assert.Equal(expectedGrade, actualGrade);
        }

        [Theory]
        [InlineData(59)]
        [InlineData(0)]
        [InlineData(-5)]
        public void ClassifyGrade_FailingScore_ReturnsF(int score)
        {
            // Arrange
            var snippet = new SnippetClass();

            // Act
            string actualGrade = snippet.ClassifyGrade(score);

            // Assert
            Assert.Equal("F", actualGrade);
        }


        [Fact]
        public void ClassifyGrade_ScoreOutOfRange_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var snippet = new SnippetClass();
            int score = 101; //Score out of range

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => snippet.ClassifyGrade(score));
        }
    }
}