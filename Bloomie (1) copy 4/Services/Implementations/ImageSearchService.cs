using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Bloomie.Data;
using Bloomie.Models.Entities;
using SixLabors.ImageSharp;
using Bloomie.Services;

namespace Bloomie.Services.Implementations
{
    public class ImageSearchService : IImageSearchService
    {
        private readonly ApplicationDbContext _context;

        public ImageSearchService(ApplicationDbContext context)
        {
            _context = context;
            // ĐÃ LOẠI BỎ ML.NET CODE GÂY LỖI
        }

        public async Task<List<ImageSearchResult>> SearchSimilarProductsAsync(byte[] imageBytes)
        {
            return await SearchSimilarProductsAsync(imageBytes, 10);
        }

        public async Task<List<ImageSearchResult>> SearchSimilarProductsAsync(byte[] imageBytes, int topResults = 10)
        {
            try
            {
                // Tạm thời trả về danh sách sản phẩm ngẫu nhiên để test
                var randomProducts = await _context.Products
                    .Where(p => p.IsActive)
                    .OrderBy(p => Guid.NewGuid()) // Random order
                    .Take(topResults)
                    .Select(p => new ImageSearchResult
                    {
                        ProductId = p.Id,
                        ProductName = p.Name ?? "Unknown Product",
                        ImageUrl = p.ImageUrl ?? "/images/default-product.jpg",
                        Price = p.Price,
                        SimilarityScore = 0.8f // Score mặc định
                    })
                    .ToListAsync();

                return randomProducts;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SearchSimilarProductsAsync: {ex.Message}");
                return new List<ImageSearchResult>();
            }
        }

        public async Task ExtractAndSaveFeaturesAsync()
        {
            try
            {
                // Tạm thời không làm gì - đã loại bỏ ML.NET
                Console.WriteLine("ExtractAndSaveFeaturesAsync is temporarily disabled");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ExtractAndSaveFeaturesAsync: {ex.Message}");
            }
        }

        public async Task<bool> IsImageFlower(byte[] imageBytes)
        {
            try
            {
                // Kiểm tra kích thước file
                if (imageBytes.Length < 1000) // File quá nhỏ
                    return false;

                // Kiểm tra định dạng ảnh
                try
                {
                    using var image = Image.Load(imageBytes);
                    // Nếu load được ảnh và có kích thước hợp lý, coi như là ảnh hoa
                    return image.Width > 50 && image.Height > 50;
                }
                catch
                {
                    return false; // Không phải ảnh hợp lệ
                }
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> RecognizeFlowerAsync(byte[] imageBytes)
        {
            try
            {
                // Tạm thời trả về tên hoa cố định để test
                // Sau này có thể tích hợp AI model thực tế
                return "hoa hồng";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RecognizeFlowerAsync: {ex.Message}");
                return "hoa";
            }
        }

        // Phương thức helper tính similarity tạm thời không cần thiết
    }

    // Giữ nguyên các class khác nếu cần
    public class InputImage
    {
        public string ImagePath { get; set; }
        public byte[] ImageBytes { get; set; }
    }

    public class ImageFeatures
    {
        public float[] Features { get; set; }
    }
}