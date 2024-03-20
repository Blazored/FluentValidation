using System;
using FluentValidation;
using FluentValidation.Internal;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using FluentValidation.Results;

namespace Blazored.FluentValidation;

public class FluentValidationValidator : ComponentBase
{
    [Inject] private IServiceProvider ServiceProvider { get; set; } = default!;

    [CascadingParameter] private EditContext? CurrentEditContext { get; set; }

    [Parameter] public IValidator? Validator { get; set; }
    [Parameter] public bool DisableAssemblyScanning { get; set; }
    [Parameter] public Action<ValidationStrategy<object>>? Options { get; set; }
    internal Action<ValidationStrategy<object>>? ValidateOptions { get; set; }
    internal Dictionary<FieldIdentifier, List<ValidationFailure>>? LastValidationResult { get; set; }

    public bool Validate(Action<ValidationStrategy<object>>? options = null)
    {
        if (CurrentEditContext is null)
        {
            throw new NullReferenceException(nameof(CurrentEditContext));
        }

        ValidateOptions = options;

        try
        {
            return CurrentEditContext.Validate();
        }
        finally
        {
            ValidateOptions = null;
        }
    }

    /// <summary>
    /// Validates this <see cref="EditContext"/>.
    /// </summary>
    /// <returns>True if there are no validation messages after validation; otherwise false.</returns>
    public async Task<bool> ValidateAsync(Action<ValidationStrategy<object>>? options = null)
    {
        if (CurrentEditContext is null)
        {
            throw new NullReferenceException(nameof(CurrentEditContext));
        }
        
        ValidateOptions = options;

        try
        {
            CurrentEditContext.Validate();

            if (!CurrentEditContext!.Properties.TryGetValue(
                    EditContextFluentValidationExtensions.PendingAsyncValidation, out var asyncValidationTask))
            {
                throw new InvalidOperationException("No pending ValidationResult found");
            }

            await (Task<ValidationResult>) asyncValidationTask;

            return !CurrentEditContext.GetValidationMessages().Any();
        }
        finally
        {
            ValidateOptions = null;
        }
    }

    protected override void OnInitialized()
    {
        if (CurrentEditContext == null)
        {
            throw new InvalidOperationException($"{nameof(FluentValidationValidator)} requires a cascading " +
                                                $"parameter of type {nameof(EditContext)}. For example, you can use {nameof(FluentValidationValidator)} " +
                                                $"inside an {nameof(EditForm)}.");
        }

        CurrentEditContext.AddFluentValidation(ServiceProvider, DisableAssemblyScanning, Validator, this);
    }

    /// <summary>
    /// Gets the full details of the last validation result, optionally by field.
    /// </summary>
    /// <param name="fieldIdentifier">If set, only returns the validation failures pertaining to the given field.</param>
    /// <returns>Validation failures.</returns>
    public ValidationFailure[] GetFailuresFromLastValidation(FieldIdentifier? fieldIdentifier = null)
    {
        if (LastValidationResult is null)
            return Array.Empty<ValidationFailure>();

        if (fieldIdentifier is null)
            return LastValidationResult.Values.SelectMany(f => f).ToArray();
        
        if (!LastValidationResult.TryGetValue(fieldIdentifier.Value, out var failures))
             return Array.Empty<ValidationFailure>();
        
        return failures.ToArray();
    }
}