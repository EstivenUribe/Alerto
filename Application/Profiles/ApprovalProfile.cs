using Alerto.Application.DTOs.Responses;
using Alerto.Domain.Entities;
using AutoMapper;

namespace Alerto.Application.Profiles;

/// <summary>
/// AutoMapper profile para mapeos de aprobación de alertas.
/// </summary>
public class ApprovalProfile : Profile
{
    public ApprovalProfile()
    {
        // Alert → ApproveAlertResponse
        CreateMap<Alert, ApproveAlertResponse>()
            .ForMember(dest => dest.Estado, opt => opt.MapFrom(_ => "Aprobada"))
            .ForMember(dest => dest.OperadorNombre, opt => opt.MapFrom(src => src.Operador != null ? src.Operador.Nombre : string.Empty))
            .ForMember(dest => dest.TiempoRespuestaSegundos, opt => opt.Ignore())
            .ForMember(dest => dest.TimestampAprobacion, opt => opt.Ignore())
            .ForMember(dest => dest.Difusion, opt => opt.Ignore());
    }
}
