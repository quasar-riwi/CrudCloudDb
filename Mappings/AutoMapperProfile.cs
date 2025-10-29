using AutoMapper;
using CrudCloud.api.DTOs;
using CrudCloud.api.Models;

namespace CrudCloud.api.Mappings;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<DatabaseInstance, DatabaseInstanceDto>();
    }
}