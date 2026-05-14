using Microsoft.AspNetCore.Components;

namespace Callio.Admin.Components.Pages.Identity;

public partial class Login : ComponentBase
{
    [SupplyParameterFromQuery(Name = "returnUrl")]
    public string? ReturnUrl { get; set; }

    [SupplyParameterFromQuery(Name = "error")]
    public string? Error { get; set; }
}
