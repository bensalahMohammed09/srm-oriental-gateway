using System;
using System.Collections.Generic;
using FluentValidation.TestHelper;
using Xunit;

// Tes namespaces
using Srm.Gateway.Application.DTOs;
using Srm.Gateway.Application.Validators;

namespace Srm.Gateway.UnitTests.Application;

public class DocumentValidatorsTests
{
    private readonly OcrIngestionValidator _ocrValidator;
    private readonly DocumentValidationValidator _agentValidator;

    public DocumentValidatorsTests()
    {
        _ocrValidator = new OcrIngestionValidator();
        _agentValidator = new DocumentValidationValidator();
    }

    // --- Tests pour OcrIngestionValidator ---

    [Fact]
    public void OcrIngestionValidator_ShouldNotHaveError_WhenRequestIsValid()
    {
        // Arrange : Utilisation du nouveau Record MetadataDto
        var metadata = new List<MetadataDto>
        {
            new MetadataDto("TVA", "20%", 0.99)
        };
        var request = new OcrIngestionRequest("INV-VALID-001", "Supplier Test", 1000m, metadata);

        // Act
        var result = _ocrValidator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void OcrIngestionValidator_ShouldHaveError_WhenReferenceIsEmpty()
    {
        // Arrange
        var request = new OcrIngestionRequest("", "Supplier Test", 1000m, new List<MetadataDto>());

        // Act
        var result = _ocrValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Reference);
    }

    // --- Tests pour DocumentValidationValidator (Agent BO) ---

    [Fact]
    public void AgentValidator_ShouldHaveError_WhenMetadataCorrectionKeyIsEmpty()
    {
        // Arrange : On utilise maintenant un Dictionary !
        var corrections = new Dictionary<string, string>
        {
            // ❌ La clé est vide, ce qui est interdit par meta.RuleFor(m => m.Key).NotEmpty()
            { "", "Nouvelle Valeur" }
        };

        var request = new DocumentValidationRequest(
            CategoryId: Guid.NewGuid(),
            Reference: "CORRECTED-REF",
            TotalAmount: 1500m,
            MetadataCorrections: corrections
        );

        // Act
        var result = _agentValidator.TestValidate(request);

        // Assert : FluentValidation cible l'index [0] du dictionnaire et sa propriété "Key"
        result.ShouldHaveValidationErrorFor("MetadataCorrections[0].Key")
              .WithErrorMessage("La clé de la métadonnée est requise pour la correction.");
    }
}