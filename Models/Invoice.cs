using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

using System;
using System.Collections.Generic;


namespace StoreAPI.Models
{
    public class Invoice
    {
        [JsonPropertyName("invoiceID")]
        public string InvoiceID { get; set; }

        [JsonPropertyName("customerID")]
        public string CustomerID { get; set; }

        [JsonPropertyName("invoiceDate")]
        public DateTime InvoiceDate { get; set; }

        [JsonPropertyName("totalPrice")]
        public decimal TotalPrice { get; set; }

        [JsonPropertyName("details")]
        public List<InvoiceDetails> Details { get; set; }
    }
}
