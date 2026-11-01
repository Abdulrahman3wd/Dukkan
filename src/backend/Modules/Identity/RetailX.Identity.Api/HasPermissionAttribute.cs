using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using RetailX.Identity.Domain;

namespace RetailX.Identity.Api;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _permission;
    public HasPermissionAttribute(string permission) => _permission = permission;

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var svc = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        if (!await svc.HasAsync(_permission, context.HttpContext.RequestAborted))
            context.Result = new ForbidResult();
    }
}