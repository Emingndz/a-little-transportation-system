using Prolab_4.Core;
using Xunit;
using FluentAssertions;

namespace Prolab_4.Tests.Core
{
    /// <summary>
    /// Result<T> testleri.
    /// </summary>
    public class ResultTests
    {
        #region Success Tests
        
        [Fact]
        public void Success_WithValue_CreatesSuccessResult()
        {
            // Arrange
            var value = 42;
            
            // Act
            var result = Result<int>.Success(value);
            
            // Assert
            result.IsSuccess.Should().BeTrue();
            result.IsFailure.Should().BeFalse();
            result.Value.Should().Be(42);
            result.Error.Should().BeNull();
        }
        
        [Fact]
        public void Success_WithNullValue_CreatesSuccessResult()
        {
            // Arrange
            string value = null;
            
            // Act
            var result = Result<string>.Success(value);
            
            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeNull();
        }
        
        #endregion

        #region Failure Tests
        
        [Fact]
        public void Failure_WithMessage_CreatesFailureResult()
        {
            // Arrange
            var error = "Bir hata oluştu";
            
            // Act
            var result = Result<int>.Failure(error);
            
            // Assert
            result.IsSuccess.Should().BeFalse();
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(error);
            result.Value.Should().Be(default(int));
        }
        
        [Fact]
        public void Failure_WithException_CreatesFailureResultWithMessage()
        {
            // Arrange
            var ex = new InvalidOperationException("Test hatası");
            
            // Act
            var result = Result<string>.Failure(ex);
            
            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Test hatası");
        }
        
        #endregion

        #region Implicit Conversion Tests
        
        [Fact]
        public void ImplicitConversion_FromValue_CreatesSuccessResult()
        {
            // Act
            Result<int> result = 42;
            
            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(42);
        }
        
        #endregion

        #region Match Tests
        
        [Fact]
        public void Match_OnSuccess_ExecutesSuccessAction()
        {
            // Arrange
            var result = Result<int>.Success(10);
            int matchedValue = 0;
            
            // Act
            result.Match(
                onSuccess: v => matchedValue = v,
                onFailure: _ => matchedValue = -1);
            
            // Assert
            matchedValue.Should().Be(10);
        }
        
        [Fact]
        public void Match_OnFailure_ExecutesFailureAction()
        {
            // Arrange
            var result = Result<int>.Failure("Hata");
            string matchedError = "";
            
            // Act
            result.Match(
                onSuccess: _ => matchedError = "success",
                onFailure: e => matchedError = e);
            
            // Assert
            matchedError.Should().Be("Hata");
        }
        
        [Fact]
        public void MatchFunc_OnSuccess_ReturnsSuccessValue()
        {
            // Arrange
            var result = Result<int>.Success(5);
            
            // Act
            var output = result.Match(
                onSuccess: v => v * 2,
                onFailure: _ => 0);
            
            // Assert
            output.Should().Be(10);
        }
        
        [Fact]
        public void MatchFunc_OnFailure_ReturnsFailureValue()
        {
            // Arrange
            var result = Result<int>.Failure("Hata");
            
            // Act
            var output = result.Match(
                onSuccess: v => v * 2,
                onFailure: _ => -1);
            
            // Assert
            output.Should().Be(-1);
        }
        
        #endregion

        #region ValueOrDefault Tests
        
        [Fact]
        public void ValueOrDefault_OnSuccess_ReturnsValue()
        {
            // Arrange
            var result = Result<int>.Success(42);
            
            // Act
            var value = result.ValueOrDefault(0);
            
            // Assert
            value.Should().Be(42);
        }
        
        [Fact]
        public void ValueOrDefault_OnFailure_ReturnsDefault()
        {
            // Arrange
            var result = Result<int>.Failure("Hata");
            
            // Act
            var value = result.ValueOrDefault(99);
            
            // Assert
            value.Should().Be(99);
        }
        
        #endregion
    }
}
