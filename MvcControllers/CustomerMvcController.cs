using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreAPI.Models;
using System.Text;
using System.Text.Json;

namespace StoreAPI.MvcControllers
{

    [Authorize]
    public class CustomerMvcController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public CustomerMvcController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
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
            var response = await client.GetAsync("/api/customer");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var customers = JsonSerializer.Deserialize<List<Customer>>(content);
                return View(customers);
            }

            TempData["Error"] = "Không thể tải danh sách khách hàng.";
            return View(new List<Customer>());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Customer model)
        {
            if (string.IsNullOrWhiteSpace(model.CustomerID) || string.IsNullOrWhiteSpace(model.CustomerName))
            {
                ViewData["Error"] = "Mã khách hàng và tên không được để trống.";
                return View(model);
            }

            var apiModel = new
            {
                customerID = model.CustomerID,
                customerName = model.CustomerName,
                phone = model.Phone
            };

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_configuration["ApiBaseUrl"] ?? "https://localhost:5000");
            var token = Request.Cookies["token"];
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var content = new StringContent(JsonSerializer.Serialize(apiModel), Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/api/customer", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            ViewData["Error"] = "Không thể thêm khách hàng: " + responseContent;
            return View(model);
        }

        public async Task<IActionResult> Edit(string id)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_configuration["ApiBaseUrl"] ?? "https://localhost:5000");
            var token = Request.Cookies["token"];
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await client.GetAsync($"/api/customer/{id}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var customer = JsonSerializer.Deserialize<Customer>(json);

                if (customer == null) return NotFound();
                return View(customer);

            }

            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Customer model)
        {
            if (string.IsNullOrWhiteSpace(model.CustomerName))
            {
                ViewData["Error"] = "Tên khách hàng không được để trống.";
                return View(model);
            }

            var apiModel = new
            {
                customerID = model.CustomerID,
                customerName = model.CustomerName,
                phone = model.Phone
            };

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_configuration["ApiBaseUrl"] ?? "https://localhost:5000");
            var token = Request.Cookies["token"];
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var content = new StringContent(JsonSerializer.Serialize(apiModel), Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"/api/customer/{model.CustomerID}", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            ViewData["Error"] = "Không thể cập nhật khách hàng: " + responseContent;
            return View(model);
        }

        public async Task<IActionResult> Delete(string id)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_configuration["ApiBaseUrl"] ?? "https://localhost:5000");
            var token = Request.Cookies["token"];
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await client.DeleteAsync($"/api/customer/{id}");

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            TempData["Error"] = "Không thể xóa khách hàng.";
            return RedirectToAction("Index");
        }
    }
}
