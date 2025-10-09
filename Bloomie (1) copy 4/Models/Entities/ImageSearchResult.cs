namespace Bloomie.Models.Entities
{
    public class ImageSearchResult
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ImageUrl { get; set; }
        public decimal Price { get; set; }
        public float SimilarityScore { get; set; }
    }
}