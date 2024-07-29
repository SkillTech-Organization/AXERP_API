using AutoMapper;
using AXERP.API.Domain.Entities;

namespace AXERP.API.Domain.AutoMapperProfiles
{
    public class ModelProfile : Profile
    {
        public ModelProfile()
        {
            CreateMap<Delivery, Transaction>().ReverseMap();
        }
    }
}
