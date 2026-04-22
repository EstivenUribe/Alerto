using Alerto.Application.DTOs.Requests;
using Alerto.Application.DTOs.Responses;
using Alerto.Domain.Entities;
using AutoMapper;

namespace Alerto.Application.Profiles;

/// <summary>
/// AutoMapper profile para mapeos de alertas.
/// </summary>
public class AlertProfile : Profile
{
    public AlertProfile()
    {
        // CreateAlertRequest → Alert
        CreateMap<CreateAlertRequest, Alert>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Estado, opt => opt.Ignore())
            .ForMember(dest => dest.TimestampGeneracion, opt => opt.Ignore())
            .ForMember(dest => dest.TimestampDifusion, opt => opt.Ignore())
            .ForMember(dest => dest.ConfianzaScore, opt => opt.Ignore())
            .ForMember(dest => dest.OperadorUsuarioId, opt => opt.Ignore())
            .ForMember(dest => dest.PoblacionAlcanzada, opt => opt.MapFrom(_ => 0))
            .ForMember(dest => dest.Geocerca, opt => opt.Ignore())
            .ForMember(dest => dest.Operador, opt => opt.Ignore());

        // Alert → AlertResponse
        CreateMap<Alert, AlertResponse>()
            .ForMember(dest => dest.Estado, opt => opt.MapFrom(src => src.Estado.ToString()))
            .ForMember(dest => dest.GeocercaNombre, opt => opt.MapFrom(src => src.Geocerca != null ? src.Geocerca.Nombre : string.Empty))
            .ForMember(dest => dest.OperadorNombre, opt => opt.MapFrom(src => src.Operador != null ? src.Operador.Nombre : null));
    }
}
