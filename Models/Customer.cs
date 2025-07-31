using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace StoreAPI.Models
{
    public class Customer
    {
        [JsonPropertyName("customerID")]
        [Required(ErrorMessage = "Mã khách hàng là bắt buộc.")]
        public string CustomerID { get; set; }

        [JsonPropertyName("customerName")]
        [Required(ErrorMessage = "Tên khách hàng là bắt buộc.")]
        public string CustomerName { get; set; }

        [JsonPropertyName("phone")]
        [RegularExpression(@"^(0|\+84)[0-9]{9}$", ErrorMessage = "Số điện thoại không hợp lệ")]
        public string Phone { get; set; }
    }
}
