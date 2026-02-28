namespace Callio.Core.Domain.Validators;

public static class RegexConstants
{
    public const string EmailRegex = "^[a-zA-Z0-9._%+\\-]+@[a-zA-Z0-9.\\-]+\\.[a-zA-Z]{2,}$";
    
    public const string PhoneRegex = "^(\\+371)?[26]\\d{7}$";

    public const string WebsiteRegex =
        "^(https?:\\/\\/)?(www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{2,256}\\.[a-z]{2,6}(\\/[-a-zA-Z0-9@:%_\\+.~#?&=\\/]*)?$";
}