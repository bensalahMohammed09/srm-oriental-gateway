using FluentValidation;
using Srm.Gateway.Application.DTOs;

namespace Srm.Gateway.Application.Validators;

public class OcrIngestionValidator : AbstractValidator<OcrIngestionRequest>
{
    public OcrIngestionValidator()
    {
        RuleFor(x => x.Reference).NotEmpty().MaximumLength(100);

        RuleForEach(x => x.Metadata).ChildRules(meta => {
            meta.RuleFor(m => m.Key).NotEmpty();
            meta.RuleFor(m => m.Value).NotNull().WithMessage("La valeur de la métadonnée ne peut pas être nulle.");
            meta.RuleFor(m => m.Confidence).InclusiveBetween(0, 1);
        });
    }
}

public class DocumentValidationValidator : AbstractValidator<DocumentValidationRequest>
{
    public DocumentValidationValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Reference).NotEmpty();

        RuleFor(x => x.RowVersion)
            .NotEmpty()
            .WithMessage("Le jeton de concurrence (RowVersion) est obligatoire pour valider le document.");
    }
}

public class UpdateMetadataValidator : AbstractValidator<UpdateMetadataRequest>
{
    public UpdateMetadataValidator()
    {
        RuleFor(x => x.RowVersion)
            .NotEmpty()
            .WithMessage("Le jeton de concurrence (RowVersion) est obligatoire pour la mise à jour.");

        RuleFor(x => x.NewMetadata)
            .NotNull()
            .WithMessage("Le dictionnaire de métadonnées est requis.");

        RuleForEach(x => x.NewMetadata)
            .ChildRules(meta => {
                meta.RuleFor(m => m.Key).NotEmpty().WithMessage("La clé est requise.");
                meta.RuleFor(m => m.Value).NotNull().WithMessage("L'objet de valeur ne peut pas être nul.");

                meta.When(m => m.Value != null, () =>
                {
                    meta.RuleFor(m => m.Value.Value).NotNull().WithMessage("Le contenu ne peut pas être nul.");
                    meta.RuleFor(m => m.Value.Confidence).InclusiveBetween(0, 1).WithMessage("Score entre 0 et 1.");
                });
            });
    }
}

public class ManualUploadValidator : AbstractValidator<ManualUploadRequest>
{
    public ManualUploadValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Reference).NotEmpty();
        RuleFor(x => x.TotalAmount).GreaterThanOrEqualTo(0);
    }
}