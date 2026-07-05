using CareTrack.Domain.Exceptions;
using FluentValidation;
using DomainValidationException = CareTrack.Domain.Exceptions.ValidationException;
using Xunit;

namespace CareTrack.UnitTests;

public class ValidationExceptionTests
{
    [Fact]
    public void ValidationException_WithErrors_StoresErrorDictionary()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["Email"] = ["Email is required"]
        };

        var ex = new DomainValidationException(errors);

        Assert.Single(ex.Errors);
        Assert.Equal("Email is required", ex.Errors["Email"][0]);
    }
}

public class LoginRequestValidator : AbstractValidator<CareTrack.Application.DTOs.Auth.LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void Validate_WhenEmailEmpty_Fails()
    {
        var result = _validator.Validate(new CareTrack.Application.DTOs.Auth.LoginRequest("", "Password@1"));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenValid_Passes()
    {
        var result = _validator.Validate(new CareTrack.Application.DTOs.Auth.LoginRequest("admin@apollo.edu", "Admin@123"));
        Assert.True(result.IsValid);
    }
}
