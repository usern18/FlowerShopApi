using FlowerShopApi.DTOs.StoreInfo;
using FlowerShopApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowerShopApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StoreInfoController : ControllerBase
    {
        private readonly StoreInfoService _storeInfoService;

        public StoreInfoController(StoreInfoService storeInfoService)
        {
            _storeInfoService = storeInfoService;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var info = await _storeInfoService.GetAsync();
            return Ok(info);
        }

        [Authorize(Roles = "admin")]
        [HttpPut]
        public async Task<IActionResult> Update(UpdateStoreInfoDto dto)
        {
            var message = await _storeInfoService.UpdateAsync(dto);
            return Ok(new { message });
        }
    }
}