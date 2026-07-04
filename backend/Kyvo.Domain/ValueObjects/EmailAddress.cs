using Kyvo.Domain.Exceptions;

namespace Kyvo.Domain.ValueObjects;

public sealed record EmailAddress
{
    public string Value { get; private set; } = default!;

    private EmailAddress()
    {
    }

    public EmailAddress(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainValidationException(DomainErrorMessages.EmailAddress.REQUIRED);
        }

        var normalized = email.Trim().ToLowerInvariant();
        if (normalized.Length > 255)
        {
            throw new DomainValidationException(DomainErrorMessages.EmailAddress.MAX_LENGTH);
        }

        if (!IsValidEmail(normalized))
        {
            throw new DomainValidationException(DomainErrorMessages.EmailAddress.INVALID_FORMAT);
        }

        Value = normalized;
    }

    public static implicit operator string(EmailAddress value) => value.Value;
    public static implicit operator EmailAddress(string value) => new(value);

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
