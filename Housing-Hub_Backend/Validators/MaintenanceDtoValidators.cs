using FluentValidation;
using HousingHubBackend.Dtos.Maintenance;

namespace HousingHubBackend.Validators
{
    public class MaintenanceBillDtoValidator : AbstractValidator<MaintenanceBillDto>
    {
        public MaintenanceBillDtoValidator()
        {
            RuleFor(x => x.FlatId)
                .NotNull().WithMessage("FlatId is required")
                .GreaterThan(0).WithMessage("FlatId must be greater than zero");
            RuleFor(x => x.Amount)
                .NotNull().WithMessage("Amount is required")
                .GreaterThan(0).WithMessage("Amount must be greater than zero");
            RuleFor(x => x.PeriodStart)
                .NotNull().WithMessage("PeriodStart is required");
            RuleFor(x => x.PeriodEnd)
                .NotNull().WithMessage("PeriodEnd is required")
                .Must((dto, end) => !dto.PeriodStart.HasValue || (end.HasValue && end.Value >= dto.PeriodStart.Value))
                .WithMessage("PeriodEnd must be on or after PeriodStart");
            RuleFor(x => x.DueDate)
                .NotNull().WithMessage("DueDate is required")
                .Must((dto, due) => !dto.PeriodStart.HasValue || (due.HasValue && due.Value >= dto.PeriodStart.Value))
                .WithMessage("DueDate must be on or after PeriodStart");
            RuleFor(x => x.TransactionId)
                .NotEmpty().WithMessage("TransactionId is required when the bill is paid")
                .MaximumLength(100).WithMessage("TransactionId must not exceed 100 characters")
                .When(x => x.Paid == true);
        }
    }

    public class MaintenanceFeeDtoValidator : AbstractValidator<MaintenanceFeeDto>
    {
        public MaintenanceFeeDtoValidator()
        {
            RuleFor(x => x.WingId)
                .NotNull().WithMessage("WingId is required")
                .GreaterThan(0).WithMessage("WingId must be greater than zero");
            RuleFor(x => x.RatePerSqft)
                .NotNull().WithMessage("RatePerSqft is required")
                .GreaterThan(0).WithMessage("RatePerSqft must be greater than zero");
            RuleFor(x => x.EffectiveFrom)
                .NotNull().WithMessage("EffectiveFrom is required");
            RuleFor(x => x.EffectiveTo)
                .Must((dto, end) => !end.HasValue || !dto.EffectiveFrom.HasValue || end.Value >= dto.EffectiveFrom.Value)
                .WithMessage("EffectiveTo must be on or after EffectiveFrom");
        }
    }

    public class GenerateMaintenanceBillsDtoValidator : AbstractValidator<GenerateMaintenanceBillsDto>
    {
        public GenerateMaintenanceBillsDtoValidator()
        {
            RuleFor(x => x.PeriodStart)
                .NotNull().WithMessage("PeriodStart is required");
            RuleFor(x => x.PeriodEnd)
                .NotNull().WithMessage("PeriodEnd is required")
                .Must((dto, end) => !dto.PeriodStart.HasValue || (end.HasValue && end.Value >= dto.PeriodStart.Value))
                .WithMessage("PeriodEnd must be on or after PeriodStart");
            RuleFor(x => x.DueDate)
                .NotNull().WithMessage("DueDate is required")
                .Must((dto, due) => !dto.PeriodStart.HasValue || (due.HasValue && due.Value >= dto.PeriodStart.Value))
                .WithMessage("DueDate must be on or after PeriodStart");
            RuleFor(x => x.WingId)
                .GreaterThan(0).WithMessage("WingId must be greater than zero")
                .When(x => x.WingId.HasValue);
        }
    }
}
