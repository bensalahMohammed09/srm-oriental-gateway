using AutoMapper;
using Srm.Gateway.Domain.Entities;
using Srm.Gateway.Application.DTOs;

namespace Srm.Gateway.Application.Mappings;

public class DocumentMappingProfile : Profile
{
    public DocumentMappingProfile()
    {
        CreateMap<DocumentFieldValue, MetadataValueDto>();

        // 🌟 FIX ANTI-CRASH : On force la construction manuelle pour éviter le bug des "records" d'AutoMapper
        CreateMap<Document, DocumentResponse>()
            .ConstructUsing(src => new DocumentResponse(
                src.Id,
                src.Reference,
                src.Status != null ? src.Status.Code : "UNKNOWN",
                src.Category != null ? src.Category.Name : null,
                src.CreatedAt,
                null
            ));

        CreateMap<Document, DocumentDetailsResponse>()
            .ForCtorParam("Status", opt => opt.MapFrom(src =>
                src.Status != null ? src.Status.Code : "UNKNOWN"))
            .ForCtorParam("Category", opt => opt.MapFrom(src =>
                src.Category != null ? src.Category.Name : null));

        CreateMap<Document, DocumentIndexationResponse>();
    }
}