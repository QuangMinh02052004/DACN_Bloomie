using Bloomie.Models.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bloomie.Services
{
    public interface IImageSearchService
    {
        Task<List<ImageSearchResult>> SearchSimilarProductsAsync(byte[] imageBytes);
        Task<string> RecognizeFlowerAsync(byte[] imageBytes);

        Task ExtractAndSaveFeaturesAsync();
    }
}