namespace EmailQueue.API.Platform;

[AttributeUsage(AttributeTargets.Property)]
public class NoEmptyStringsAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not List<string> stringList) return ValidationResult.Success;

        for (var i = 0; i < stringList.Count; i++)
            if (string.IsNullOrWhiteSpace(stringList[i]))
                return new ValidationResult($"The item at position {i} cannot be empty or whitespace.");

        return ValidationResult.Success;
    }
}
