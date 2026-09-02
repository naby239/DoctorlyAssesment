using Microsoft.AspNetCore.Mvc;

namespace Doctorly.Scheduling.Api.OpenApi;

// Documentation boundary only; it does not restrict who may call the endpoint.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class InternalApiAttribute : ApiExplorerSettingsAttribute
{
    public InternalApiAttribute() => GroupName = ApiDocuments.Internal;
}
