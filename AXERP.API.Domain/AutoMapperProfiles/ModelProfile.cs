using AutoMapper;
using AXERP.API.Domain.Entities;
using AXERP.API.Domain.GoogleSheetModels;

namespace AXERP.API.Domain.AutoMapperProfiles
{
    public class ModelProfile : Profile
    {
        public ModelProfile()
        {
            CreateMap<GasTransaction, GasTransactionSheetModel>().ReverseMap();
        }
    }
}
