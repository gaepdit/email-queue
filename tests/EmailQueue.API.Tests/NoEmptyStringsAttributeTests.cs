using EmailQueue.API.Validation;
using System.ComponentModel.DataAnnotations;

namespace EmailQueue.API.Tests;

[TestFixture]
public class NoEmptyStringsAttributeTests
{
    private NoEmptyStringsAttribute _attribute;
    private ValidationContext _validationContext;

    [SetUp]
    public void Setup()
    {
        _attribute = new NoEmptyStringsAttribute();
        _validationContext = new ValidationContext(new object());
    }

    [Test]
    public void WhenValueIsNull_ShouldReturnSuccess()
    {
        // Arrange
        object? value = null;

        // Act
        var result = _attribute.GetValidationResult(value, _validationContext);

        // Assert
        result.Should().BeEquivalentTo(ValidationResult.Success);
    }

    [Test]
    public void WhenListContainsNoEmptyStrings_ShouldReturnSuccess()
    {
        // Arrange
        var value = new List<string> { "value1", "value2", "value3" };

        // Act
        var result = _attribute.GetValidationResult(value, _validationContext);

        // Assert
        result.Should().BeEquivalentTo(ValidationResult.Success);
    }

    [Test]
    public void WhenValueIsNotListOfStrings_ShouldReturnSuccess()
    {
        // Arrange
        const string value = "Not a list of strings";

        // Act
        var result = _attribute.GetValidationResult(value, _validationContext);

        // Assert
        result.Should().BeEquivalentTo(ValidationResult.Success);
    }

    [Test]
    public void WhenListContainsEmptyString_ShouldReturnValidationError()
    {
        // Arrange
        var value = new List<string> { "value1", "", "value3" };
        const string expectedErrorMessage = "The item at position 1 cannot be empty or whitespace.";

        // Act
        var result = _attribute.GetValidationResult(value, _validationContext);

        // Assert
        result.Should().NotBeEquivalentTo(ValidationResult.Success);
        result.ErrorMessage.Should().Be(expectedErrorMessage);
    }

    [Test]
    public void WhenListContainsWhitespaceString_ShouldReturnValidationError()
    {
        // Arrange
        var value = new List<string> { "value1", "value2", "   " };
        const string expectedErrorMessage = "The item at position 2 cannot be empty or whitespace.";

        // Act
        var result = _attribute.GetValidationResult(value, _validationContext);

        // Assert
        result.Should().NotBeEquivalentTo(ValidationResult.Success);
        result.ErrorMessage.Should().Be(expectedErrorMessage);
    }

    [Test]
    public void WhenListContainsNullString_ShouldReturnValidationError()
    {
        // Arrange
        var value = new List<string?> { "value1", null, "value3" };
        const string expectedErrorMessage = "The item at position 1 cannot be empty or whitespace.";

        // Act
        var result = _attribute.GetValidationResult(value, _validationContext);

        // Assert
        result.Should().NotBeEquivalentTo(ValidationResult.Success);
        result.ErrorMessage.Should().Be(expectedErrorMessage);
    }
}
