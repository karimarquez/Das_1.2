using DAS.Models.DTOs;

namespace DAS.Services.Interfaces;

public interface IProductService
{
    IReadOnlyList<ProductDto> GetActive();
    IReadOnlyList<ProductDto> GetAll();
    ProductDto? GetById(int id);
    bool NameExists(string name, int? excludeId = null);
    ServiceResult<ProductDto> Create(SaveProductDto product);
    ServiceResult<ProductDto> Update(int id, SaveProductDto product);
    ServiceResult<ProductDto> SetActive(int id, bool isActive);
}
