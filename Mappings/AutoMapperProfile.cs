using AutoMapper;
using CrudCloud.api.DTOs;
using CrudCloud.api.Models;
using CrudCloud.api.Data.Entities;

namespace CrudCloud.api.Mappings;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<DatabaseInstance, DatabaseInstanceDto>();
    }
}