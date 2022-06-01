using FluentValidation;
using FluentValidation.Internal;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using FluentValidation.Results;

namespace Blazored.FluentValidation;

public class FluentValidationValidator : ComponentBase
{
    [Inject] private IServiceProvider ServiceProvider { get; set; } = default!;

    [CascadingParameter] private EditContext? CurrentEditContext { get; set; }

    [Parameter] public IValidator? Validator { get; set; }
    [Parameter] public bool DisableAssemblyScanning { get; set; }

    internal Action<ValidationStrategy<object>>? Options;

    public bool Validate(Action<ValidationStrategy<object>> options)
    {
        if (CurrentEditContext is null)
        {
            throw new NullReferenceException(nameof(CurrentEditContext));
        }

        Options = options;

        try
        {
            return CurrentEditContext.Validate();
        }
        finally
        {
            Options = null;
        }
    }

    /// <summary>
    /// Validates this <see cref="EditContext"/>.
    /// </summary>
    /// <returns>True if there are no validation messages after validation; otherwise false.</returns>
    public async Task<bool> ValidateAsync()
    {
        if (CurrentEditContext is null)
        {
            throw new NullReferenceException(nameof(CurrentEditContext));
        }
        
        CurrentEditContext.Validate();

        if (!CurrentEditContext!.Properties.TryGetValue(EditContextFluentValidationExtensions.PendingAsyncValidation, out var asyncValidationTask))
        {
            throw new InvalidOperationException("No pending ValidationResult found");
        }

        await (Task<ValidationResult>)asyncValidationTask;
        
        return !CurrentEditContext.GetValidationMessages().Any();
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
}