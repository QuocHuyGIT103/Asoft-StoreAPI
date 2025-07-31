using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace StoreAPI.Models
{
    public class Product
    {
        [JsonPropertyName("productID")]
        [Required(ErrorMessage = "Mã sản phẩm là bắt buộc.")]
        public string ProductID { get; set; }

        [JsonPropertyName("productName")]
        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc.")]
        public string ProductName { get; set; }

        [JsonPropertyName("price")]
        [Required(ErrorMessage = "Giá là bắt buộc.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0.")]
        public decimal Price { get; set; }
    }
}
