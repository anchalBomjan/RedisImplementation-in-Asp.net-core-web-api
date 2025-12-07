using AutoMapper;
using RedisCrudAPI.DTOs;
using RedisCrudAPI.Models;

namespace RedisCrudAPI.Mapping
{


    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<Product, ProductDTO>();
            CreateMap<CreateProductDTO, Product>();
            CreateMap<UpdateProductDTO, Product>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }


}
