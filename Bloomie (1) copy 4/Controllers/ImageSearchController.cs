using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Bloomie.Services; // Changed from DACN_Bloomie.Services
using Bloomie.Models.Entities; // Changed from DACN_Bloomie.Models.Entities

namespace Bloomie.Controllers // Changed from DACN_Bloomie.Controllers
{
    public class ImageSearchController : Controller
    {
        private readonly IImageSearchService _imageSearchService;

        public ImageSearchController(IImageSearchService imageSearchService)
        {
            _imageSearchService = imageSearchService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Search(IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                ViewBag.Error = "Vui lòng chọn một hình ảnh.";
                return View("Index");
            }

            try
            {
                using var memoryStream = new MemoryStream();
                await image.CopyToAsync(memoryStream);
                var imageBytes = memoryStream.ToArray();

                var results = await _imageSearchService.SearchSimilarProductsAsync(imageBytes);

                if (results.Count == 0)
                {
                    ViewBag.Message = "Không tìm thấy sản phẩm tương tự. Vui lòng thử với hình ảnh khác.";
                    return View("Index");
                }

                return View("Results", results);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Đã xảy ra lỗi: {ex.Message}";
                return View("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ExtractFeatures()
        {
            try
            {
                await _imageSearchService.ExtractAndSaveFeaturesAsync();
                ViewBag.Message = "Đã trích xuất và lưu đặc trưng thành công!";
                return RedirectToAction("Index", "Admin");
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Đã xảy ra lỗi: {ex.Message}";
                return RedirectToAction("Index", "Admin");
            }
        }
    }
}