using FluentValidation;
using Srm.Gateway.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Srm.Gateway.Application.Validators
{
    // Validateur pour la création (Ingestion OCR)
    public class OcrIngestionValidator : AbstractValidator<OcrIngestionRequest>
    {
        public OcrIngestionValidator()
        {
            RuleFor(x => x.Reference).NotEmpty().MaximumLength(100);

            RuleForEach(x => x.Metadata).ChildRules(meta => {
                meta.RuleFor(m => m.Key).NotEmpty();
                meta.RuleFor(m => m.Value).NotEmpty();
                meta.RuleFor(m => m.Confidence).InclusiveBetween(0, 1);
                // ICI : Pas de règle sur l'ID car l'ID n'existe pas encore
            });
        }
    }

    // Validateur pour la mise à jour (Correction Agent)
    public class DocumentValidationValidator : AbstractValidator<DocumentValidationRequest>
    {
        public DocumentValidationValidator()
        {
            RuleFor(x => x.CategoryId).NotEmpty();
            RuleFor(x => x.Reference).NotEmpty();

            RuleForEach(x => x.MetadataCorrections).ChildRules(meta => {
                meta.RuleFor(m => m.Id).NotEmpty().WithMessage("L'ID de la métadonnée est requis pour la correction.");
                meta.RuleFor(m => m.Value).NotEmpty().WithMessage("La valeur corrigée ne peut pas être vide.");
            });
        }
    }

}
