namespace Callio.Core.Domain.Validators;

public static class RegexConstants
{
    public const string EmailRegex = "^((?!\\.)[\\w\\-_.]*[^.])(@\\w+)(\\.\\w+(\\.\\w+)?[^.\\W])$\n";
    
    public const string PhoneRegex = "^\\+[0-9]{1,3}[- .]?[0-9]{3}[- .]?[0-9]{4,6}$";
}