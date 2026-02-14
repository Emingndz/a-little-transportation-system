using Prolab_4.Core.Validation;
using Xunit;
using FluentAssertions;

namespace Prolab_4.Tests.Core
{
    /// <summary>
    /// ValidationHelper testleri.
    /// </summary>
    public class ValidationHelperTests
    {
        #region IsNullOrEmpty Tests
        
        [Fact]
        public void IsNullOrEmpty_NullString_ReturnsTrue()
        {
            // Arrange
            string value = null;
            
            // Act
            var result = ValidationHelper.IsNullOrEmpty(value);
            
            // Assert
            result.Should().BeTrue();
        }
        
        [Fact]
        public void IsNullOrEmpty_EmptyString_ReturnsTrue()
        {
            // Arrange
            var value = "";
            
            // Act
            var result = ValidationHelper.IsNullOrEmpty(value);
            
            // Assert
            result.Should().BeTrue();
        }
        
        [Fact]
        public void IsNullOrEmpty_ValidString_ReturnsFalse()
        {
            // Arrange
            var value = "test";
            
            // Act
            var result = ValidationHelper.IsNullOrEmpty(value);
            
            // Assert
            result.Should().BeFalse();
        }
        
        #endregion

        #region IsValidCoordinate Tests
        
        [Theory]
        [InlineData(41.0, 29.0, true)]
        [InlineData(0, 0, true)]
        [InlineData(-90, -180, true)]
        [InlineData(90, 180, true)]
        [InlineData(-91, 0, false)]
        [InlineData(91, 0, false)]
        [InlineData(0, -181, false)]
        [InlineData(0, 181, false)]
        public void IsValidCoordinate_VariousValues_ReturnsExpected(double lat, double lon, bool expected)
        {
            // Act
            var result = ValidationHelper.IsValidCoordinate(lat, lon);
            
            // Assert
            result.Should().Be(expected);
        }
        
        #endregion

        #region IsValidDurakId Tests
        
        [Fact]
        public void IsValidDurakId_ValidId_ReturnsTrue()
        {
            // Arrange
            var id = "D001";
            
            // Act
            var result = ValidationHelper.IsValidDurakId(id);
            
            // Assert
            result.Should().BeTrue();
        }
        
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void IsValidDurakId_InvalidId_ReturnsFalse(string id)
        {
            // Act
            var result = ValidationHelper.IsValidDurakId(id);
            
            // Assert
            result.Should().BeFalse();
        }
        
        #endregion

        #region IsPositive Tests
        
        [Theory]
        [InlineData(1, true)]
        [InlineData(100, true)]
        [InlineData(0, false)]
        [InlineData(-1, false)]
        public void IsPositive_VariousValues_ReturnsExpected(int value, bool expected)
        {
            // Act
            var result = ValidationHelper.IsPositive(value);
            
            // Assert
            result.Should().Be(expected);
        }
        
        #endregion

        #region IsNonNegative Tests
        
        [Theory]
        [InlineData(0, true)]
        [InlineData(1, true)]
        [InlineData(-1, false)]
        public void IsNonNegative_VariousValues_ReturnsExpected(double value, bool expected)
        {
            // Act
            var result = ValidationHelper.IsNonNegative(value);
            
            // Assert
            result.Should().Be(expected);
        }
        
        #endregion

        #region IsInRange Tests
        
        [Theory]
        [InlineData(5, 1, 10, true)]
        [InlineData(1, 1, 10, true)]
        [InlineData(10, 1, 10, true)]
        [InlineData(0, 1, 10, false)]
        [InlineData(11, 1, 10, false)]
        public void IsInRange_VariousValues_ReturnsExpected(int value, int min, int max, bool expected)
        {
            // Act
            var result = ValidationHelper.IsInRange(value, min, max);
            
            // Assert
            result.Should().Be(expected);
        }
        
        #endregion
    }
}
