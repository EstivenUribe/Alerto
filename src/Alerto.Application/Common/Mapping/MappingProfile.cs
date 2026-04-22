using Alerto.Application.Alerts;
using Alerto.Application.Geofences;
using Alerto.Application.Users;
using Alerto.Domain.Entities;

namespace Alerto.Application.Common.Mapping;

public sealed class MappingProfile : AutoMapper.Profile
{
    public MappingProfile()
    {
        CreateMap<AlertDispatch, AlertDispatchResponse>();
        CreateMap<Alert, AlertResponse>()
            .ForMember(destination => destination.Dispatches, options => options.MapFrom(source => source.Dispatches));

        CreateMap<Geofence, GeofenceResponse>();
        CreateMap<User, UserResponse>()
            .ForMember(destination => destination.Role, options => options.MapFrom(source => source.Role.ToString()));
    }
}
