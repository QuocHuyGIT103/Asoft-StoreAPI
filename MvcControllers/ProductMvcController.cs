using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace StoreAPI.MvcControllers
{
    [Authorize]
    public class ProductMvcController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public ProductMvcController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_configuration["ApiBaseUrl"] ?? "https://localhost:5000");
            var token = Request.Cookies["token"];
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await client.GetAsync("/api/product");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var products = JsonSerializer.Deserialize<List<StoreAPI.Models.Product>>(content);
                return View(products);
            }
            return View(new List<StoreAPI.Models.Product>());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(StoreAPI.Models.Product model)
        {
            // Debug dữ liệu nhận được
            Console.WriteLine($"ProductID: {model.ProductID}, ProductName: {model.ProductName}, Price: {model.Price}");

            // Server-side validation
            if (string.IsNullOrWhiteSpace(model.ProductID) || string.IsNullOrWhiteSpace(model.ProductName) || model.Price <= 0)
            {
                ViewData["Error"] = "Dữ liệu không hợp lệ. Mã sản phẩm, tên sản phẩm là bắt buộc và giá phải lớn hơn 0.";
                return View(model);
            }

            var apiModel = new
            {
                productID = model.ProductID,
                productName = model.ProductName,
                price = model.Price
            };

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_configuration["ApiBaseUrl"] ?? "https://localhost:5000");
            var token = Request.Cookies["token"];
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var content = new StringContent(JsonSerializer.Serialize(apiModel), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/api/product", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }
            ViewData["Error"] = "Không thể thêm sản phẩm: " + responseContent;
            return View(model);
        }

        public async Task<IActionResult> Edit(string id)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_configuration["ApiBaseUrl"] ?? "https://localhost:5000");
            var token = Request.Cookies["token"];
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await client.GetAsync($"/api/product/{id}");
            if (response.IsSuccessStatusCode)
            {
                var product = JsonSerializer.Deserialize<StoreAPI.Models.Product>(await response.Content.ReadAsStringAsync());
                if (product == null) return NotFound();
                return View(product);
            }
            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Edit(StoreAPI.Models.Product model)
        {
            
            string id = model.ProductID;
            if (string.IsNullOrWhiteSpace(model.ProductName) || model.Price <= 0)
            {
                ViewData["Error"] = "Dữ liệu không hợp lệ. Tên sản phẩm là bắt buộc và giá phải lớn hơn 0.";
                return View(model);
            }

            var apiModel = new { productID = id, productName = model.ProductName, price = model.Price };
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_configuration["ApiBaseUrl"] ?? "https://localhost:5000");
            var token = Request.Cookies["token"];
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var content = new StringContent(JsonSerializer.Serialize(apiModel), Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"/api/product/{id}", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }
            ViewData["Error"] = "Không thể cập nhật sản phẩm: " + responseContent;
            return View(model);
        }

        public async Task<IActionResult> Delete(string id)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_configuration["ApiBaseUrl"] ?? "https://localhost:5000");
            var token = Request.Cookies["token"];
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await client.DeleteAsync($"/api/product/{id}");
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }
            TempData["Error"] = "Không thể xóa sản phẩm do đã tồn tại trong hóa đơn.";
            return RedirectToAction("Index");
        }
    }
}
