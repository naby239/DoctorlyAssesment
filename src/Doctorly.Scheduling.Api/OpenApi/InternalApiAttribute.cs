using Microsoft.AspNetCore.Mvc;

namespace Doctorly.Scheduling.Api.OpenApi;

/// <summary>
/// Excludes a controller or action from the public OpenAPI document.
/// </summary>
/// <remarks>
/// A documentation boundary, not a security one - it controls what is published,
/// not who may call the endpoint.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class InternalApiAttribute : ApiExplorerSettingsAttribute
{
    public InternalApiAttribute() => GroupName = ApiDocuments.Internal;
}
