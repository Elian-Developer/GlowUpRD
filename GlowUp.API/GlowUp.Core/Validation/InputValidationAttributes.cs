using System.ComponentModel.DataAnnotations;

namespace GlowUpRD.API.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class PersonNameAttribute : ValidationAttribute
{
    public PersonNameAttribute() : base(InputRules.NameMessage) { }
    public override bool IsValid(object? value) => value is string text && InputRules.IsPersonName(text);
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class CommercialTextAttribute : ValidationAttribute
{
    public CommercialTextAttribute() : base("El nombre debe contener texto válido y no puede estar compuesto solo por símbolos.") { }
    public override bool IsValid(object? value) => value is string text && InputRules.IsCommercialText(text);
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class ValidPhoneAttribute : ValidationAttribute
{
    public ValidPhoneAttribute() : base(InputRules.PhoneMessage) { }
    public override bool IsValid(object? value) => value is null || value is string text && InputRules.IsPhone(text);
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class RealisticEmailAttribute : ValidationAttribute
{
    public RealisticEmailAttribute() : base(InputRules.EmailMessage) { }
    public override bool IsValid(object? value) => value is null || value is string text && (string.IsNullOrWhiteSpace(text) || InputRules.IsEmail(text));
}
