namespace Doctorly.Scheduling.Api.OpenApi;

/// <summary>
/// Names of the OpenAPI documents this API publishes.
/// </summary>
/// <remarks>
/// <c>public</c> is the supported third-party contract and omits anything marked
/// <see cref="InternalApiAttribute"/>. <c>internal</c> contains the full surface.
/// </remarks>
public static class ApiDocuments
{
    public const string Public = "public";
    public const string Internal = "internal";
}
