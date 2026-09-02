using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Doctorly.Scheduling.Api.Serialization;

// The JSON converter only covers request bodies. Query string values are bound separately,
// and would otherwise be read in server local time.
public sealed class RequiredOffsetDateTimeModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

        if (value == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, value);

        var text = value.FirstValue;
        var isNullable = Nullable.GetUnderlyingType(bindingContext.ModelType) is not null;

        if (string.IsNullOrWhiteSpace(text))
        {
            if (isNullable)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
            }
            else
            {
                bindingContext.ModelState.TryAddModelError(
                    bindingContext.ModelName, TimestampOffset.ErrorMessage);
            }

            return Task.CompletedTask;
        }

        if (!TimestampOffset.HasOffset(text)
            || !DateTimeOffset.TryParse(
                text,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var parsed))
        {
            bindingContext.ModelState.TryAddModelError(
                bindingContext.ModelName, TimestampOffset.ErrorMessage);

            return Task.CompletedTask;
        }

        bindingContext.Result = ModelBindingResult.Success(parsed);

        return Task.CompletedTask;
    }
}
