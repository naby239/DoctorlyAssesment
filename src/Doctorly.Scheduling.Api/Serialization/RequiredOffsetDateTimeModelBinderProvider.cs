using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Doctorly.Scheduling.Api.Serialization;

public sealed class RequiredOffsetDateTimeModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var modelType = Nullable.GetUnderlyingType(context.Metadata.ModelType)
            ?? context.Metadata.ModelType;

        return modelType == typeof(DateTimeOffset)
            ? new RequiredOffsetDateTimeModelBinder()
            : null;
    }
}
