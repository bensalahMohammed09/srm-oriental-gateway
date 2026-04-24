using FluentValidation;
using Srm.Gateway.Application.DTOs;

namespace Srm.Gateway.Application.Validators
{
    // --- 1. Validateur pour la création (Ingestion OCR) ---
    public class OcrIngestionValidator : AbstractValidator<OcrIngestionRequest>
    {
        public OcrIngestionValidator()
        {
            RuleFor(x => x.Reference).NotEmpty().MaximumLength(100);

            RuleForEach(x => x.Metadata).ChildRules(meta => {
                meta.RuleFor(m => m.Key).NotEmpty();
                // 🌟 CHANGEMENT : On utilise NotNull() car Value est un 'object' maintenant (JSON)
                meta.RuleFor(m => m.Value).NotNull().WithMessage("La valeur de la métadonnée ne peut pas être nulle.");
                meta.RuleFor(m => m.Confidence).InclusiveBetween(0, 1);
            });
        }
    }

    // --- 2. Validateur pour la validation métier finale du document ---
    public class DocumentValidationValidator : AbstractValidator<DocumentValidationRequest>
    {
        public DocumentValidationValidator()
        {
            RuleFor(x => x.CategoryId).NotEmpty();
            RuleFor(x => x.Reference).NotEmpty();

            // 🗑️ SUPPRIMÉ : L'itération sur MetadataCorrections a été retirée 
            // car le DTO ne gère plus les métadonnées. C'est le rôle de la nouvelle route dédiée !
        }
    }

    // --- 3. 🌟 NOUVEAU : Validateur pour notre route "Clear & Replace" JSONB ---
    public class UpdateMetadataValidator : AbstractValidator<UpdateMetadataRequest>
    {
        public UpdateMetadataValidator()
        {
            RuleFor(x => x.NewMetadata)
                .NotNull()
                .WithMessage("Le dictionnaire de métadonnées est requis.");

            // On itère sur le Dictionary<string, MetadataValueDto>
            RuleForEach(x => x.NewMetadata)
                .ChildRules(meta => {
                    // 'meta' représente un KeyValuePair<string, MetadataValueDto>

                    // Validation de la Clé JSON
                    meta.RuleFor(m => m.Key)
                        .NotEmpty()
                        .WithMessage("La clé de la métadonnée est requise.");

                    // Sécurité : On s'assure que le Frontend n'a pas envoyé une valeur "null"
                    meta.RuleFor(m => m.Value)
                        .NotNull()
                        .WithMessage("L'objet de valeur ne peut pas être nul.");

                    // Validation à l'intérieur de l'objet MetadataValueDto
                    meta.When(m => m.Value != null, () =>
                    {
                        meta.RuleFor(m => m.Value.Value)
                            .NotNull()
                            .WithMessage("Le contenu de la métadonnée ne peut pas être nul.");

                        meta.RuleFor(m => m.Value.Confidence)
                            .InclusiveBetween(0, 1)
                            .WithMessage("Le score de confiance doit être compris entre 0 et 1.");
                    });
                });
        }
    }
}