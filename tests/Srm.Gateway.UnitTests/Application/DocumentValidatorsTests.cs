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
        // Arrange
        var request = new OcrIngestionRequest(
            Reference: "INV-VALID-001",
            SupplierName: "Supplier Test",
            TotalAmount: 1000m,
            Metadata: new List<OcrMetadataInputDto>
            {
                new OcrMetadataInputDto("TVA", "20%", 0.99)
            }
        );

        // Act
        var result = _ocrValidator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void OcrIngestionValidator_ShouldHaveError_WhenReferenceIsEmpty()
    {
        // Arrange
        var request = new OcrIngestionRequest(
            Reference: "", // ❌ Référence vide (doit déclencher une erreur)
            SupplierName: "Supplier Test",
            TotalAmount: 1000m,
            Metadata: new List<OcrMetadataInputDto>()
        );

        // Act
        var result = _ocrValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Reference);
    }

    // --- Tests pour DocumentValidationValidator (Agent BO) ---

    [Fact]
    public void AgentValidator_ShouldHaveError_WhenMetadataCorrectionIsMissingId()
    {
        // Arrange
        var request = new DocumentValidationRequest(
            CategoryId: Guid.NewGuid(),
            Reference: "CORRECTED-REF",
            TotalAmount: 1500m,
            MetadataCorrections: new List<OcrMetadataUpdateDto>
            {
                // ❌ L'ID est manquant (Empty Guid), ce qui est interdit par ta règle
                new OcrMetadataUpdateDto(Guid.Empty, "Nouvelle Valeur")
            }
        );

        // Act
        var result = _agentValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor("MetadataCorrections[0].Id")
              .WithErrorMessage("L'ID de la métadonnée est requis pour la correction.");
    }
}