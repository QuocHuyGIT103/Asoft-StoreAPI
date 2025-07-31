using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace StoreAPI.Models
{
    public class InvoiceDetails
    {
        [JsonPropertyName("invoiceDetailID")]
        public int InvoiceDetailID { get; set; }

        [JsonPropertyName("invoiceID")]
        [Required(ErrorMessage = "InvoiceID không được để trống")]
        public string InvoiceID { get; set; }

        [JsonPropertyName("productID")]
        [Required(ErrorMessage = "ProductID không được để trống")]
        public string ProductID { get; set; }

        [JsonPropertyName("quantity")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity phải lớn hơn hoặc bằng 1")]
        public int Quantity { get; set; }

        [JsonPropertyName("totalPrice")]
        public decimal TotalPrice { get; set; }
    }
}
