using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace StoreAPI.MvcControllers
{
    [Authorize]
    public class InvoiceMvcController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<InvoiceMvcController> _logger;

        public InvoiceMvcController(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<InvoiceMvcController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_configuration["ApiBaseUrl"] ?? "https://localhost:5000");
            var token = Request.Cookies["token"];
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await client.GetAsync("/api/invoice");
            if (response.IsSuccessStatusCode)
            {
                var invoices = JsonSerializer.Deserialize<List<StoreAPI.Models.Invoice>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return View(invoices ?? new List<StoreAPI.Models.Invoice>());
            }
            return View(new List<StoreAPI.Models.Invoice>());
        }

        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Create GET method called");
            await LoadSelectListData();
            return View(new StoreAPI.Models.Invoice { Details = new List<StoreAPI.Models.InvoiceDetails>() });
        }

        [HttpPost]
        public async Task<IActionResult> Create(StoreAPI.Models.Invoice model)
        {
            _logger.LogInformation("Create POST method called with InvoiceID: {InvoiceID}", model?.InvoiceID);

            try
            {
                // Đảm bảo Details có InvoiceID
                if (model?.Details != null)
                {
                    foreach (var detail in model.Details)
                    {
                        detail.InvoiceID = model.InvoiceID;
                    }
                }

                // Debug form data
                _logger.LogInformation("Model state valid: {IsValid}", ModelState.IsValid);
                _logger.LogInformation("Details count: {Count}", model?.Details?.Count ?? 0);

                if (model?.Details == null || !model.Details.Any())
                {
                    ModelState.AddModelError("", "Phải có ít nhất một chi tiết hóa đơn");
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Model state is invalid");
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        _logger.LogWarning("Validation error: {Error}", error.ErrorMessage);
                    }
                    await LoadSelectListData();
                    return View(model ?? new StoreAPI.Models.Invoice { Details = new List<StoreAPI.Models.InvoiceDetails>() });
                }

                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(_configuration["ApiBaseUrl"] ?? "https://localhost:5000");
                var token = Request.Cookies["token"];

                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogError("No authentication token found");
                    ModelState.AddModelError("", "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
                    await LoadSelectListData();
                    return View(model ?? new StoreAPI.Models.Invoice { Details = new List<StoreAPI.Models.InvoiceDetails>() });
                }

                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var jsonContent = JsonSerializer.Serialize(model, options);
                _logger.LogInformation("Sending JSON: {Json}", jsonContent);

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await client.PostAsync("/api/invoice", content);

                _logger.LogInformation("API response status: {StatusCode}", response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Thêm hóa đơn thành công!";
                    return RedirectToAction("Index");
                }

                var errorMessage = await response.Content.ReadAsStringAsync();
                _logger.LogError("API error: {Error}", errorMessage);

                ModelState.AddModelError("", $"Không thể thêm hóa đơn: {errorMessage}");
                await LoadSelectListData();
                return View(model ?? new StoreAPI.Models.Invoice { Details = new List<StoreAPI.Models.InvoiceDetails>() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in Create POST");
                ModelState.AddModelError("", $"Lỗi hệ thống: {ex.Message}");
                await LoadSelectListData();
                return View(model ?? new StoreAPI.Models.Invoice { Details = new List<StoreAPI.Models.InvoiceDetails>() });
            }
        }

        public async Task<IActionResult> Edit(string id)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_configuration["ApiBaseUrl"] ?? "https://localhost:5000");
            var token = Request.Cookies["token"];
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"/api/invoice/{id}");
            if (response.IsSuccessStatusCode)
            {
                var invoice = JsonSerializer.Deserialize<StoreAPI.Models.Invoice>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (invoice == null) return NotFound();
                await LoadSelectListData();
                return View(invoice);
            }
            TempData["Error"] = "Không tìm thấy hóa đơn.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, StoreAPI.Models.Invoice model)
        {
            _logger.LogInformation("Edit POST called with URL ID: {Id}, Model InvoiceID: {InvoiceID}", id, model?.InvoiceID);

            // Chuẩn hóa dữ liệu từ form
            if (model != null)
            {
                model.InvoiceID = model.InvoiceID?.Trim().ToUpper();
                model.CustomerID = model.CustomerID?.Trim().ToUpper();
                if (model.Details != null)
                {
                    foreach (var detail in model.Details)
                    {
                        detail.InvoiceID = model.InvoiceID;
                        detail.ProductID = detail.ProductID?.Trim().ToUpper();
                    }
                }
            }

            if (id != model?.InvoiceID)
            {
                _logger.LogWarning("InvoiceID mismatch: URL ID = {UrlId}, Model ID = {ModelId}", id, model?.InvoiceID);
                return BadRequest();
            }

            if (model?.Details != null)
            {
                foreach (var detail in model.Details)
                {
                    detail.InvoiceID = model.InvoiceID;
                    _logger.LogInformation("Detail: InvoiceID={InvoiceID}, ProductID={ProductID}, Quantity={Quantity}", detail.InvoiceID, detail.ProductID, detail.Quantity);
                }
            }
            else
            {
                _logger.LogWarning("Details is null for InvoiceID: {InvoiceID}", model?.InvoiceID);
                ModelState.AddModelError("", "Phải có ít nhất một chi tiết hóa đơn.");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for InvoiceID: {InvoiceID}", model?.InvoiceID);
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning("Validation error: {Error}", error.ErrorMessage);
                }
                await LoadSelectListData();
                return View(model ?? new StoreAPI.Models.Invoice { Details = new List<StoreAPI.Models.InvoiceDetails>() });
            }

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_configuration["ApiBaseUrl"] ?? "https://localhost:5000");
            var token = Request.Cookies["token"];
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("No authentication token found");
                ModelState.AddModelError("", "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
                await LoadSelectListData();
                return View(model ?? new StoreAPI.Models.Invoice { Details = new List<StoreAPI.Models.InvoiceDetails>() });
            }

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var content = new StringContent(JsonSerializer.Serialize(model, options), Encoding.UTF8, "application/json");
            _logger.LogInformation("Sending JSON to API: {Json}", await content.ReadAsStringAsync());
            var response = await client.PutAsync($"/api/invoice/{id}", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Cập nhật hóa đơn thành công!";
                return RedirectToAction("Index");
            }

            var errorMessage = await response.Content.ReadAsStringAsync();
            _logger.LogError("API error for InvoiceID {InvoiceID}: StatusCode={StatusCode}, Message={Error}", id, response.StatusCode, errorMessage);
            ModelState.AddModelError("", $"Không thể cập nhật hóa đơn: {errorMessage}");
            await LoadSelectListData();
            return View(model ?? new StoreAPI.Models.Invoice { Details = new List<StoreAPI.Models.InvoiceDetails>() });
        }

        public async Task<IActionResult> Delete(string id)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_configuration["ApiBaseUrl"] ?? "https://localhost:5000");
            var token = Request.Cookies["token"];
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await client.DeleteAsync($"/api/invoice/{id}");
            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Xóa hóa đơn thành công!";
                return RedirectToAction("Index");
            }
            TempData["Error"] = "Không thể xóa hóa đơn.";
            return RedirectToAction("Index");
        }

        private async Task LoadSelectListData()
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_configuration["ApiBaseUrl"] ?? "https://localhost:5000");
            var token = Request.Cookies["token"];
            
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("No token found when loading select list data");
                ViewBag.Customers = new List<StoreAPI.Models.Customer>();
                ViewBag.Products = new List<StoreAPI.Models.Product>();
                ViewData["Error"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                return;
            }

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            
            try
            {
                var customerResponse = await client.GetAsync("/api/customer");
                var productResponse = await client.GetAsync("/api/product");
                
                _logger.LogInformation("Customer API response: {StatusCode}", customerResponse.StatusCode);
                _logger.LogInformation("Product API response: {StatusCode}", productResponse.StatusCode);
                
                if (customerResponse.IsSuccessStatusCode)
                {
                    var customersJson = await customerResponse.Content.ReadAsStringAsync();
                    var customers = JsonSerializer.Deserialize<List<StoreAPI.Models.Customer>>(customersJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    ViewBag.Customers = customers ?? new List<StoreAPI.Models.Customer>();
                    _logger.LogInformation("Loaded {Count} customers", customers?.Count ?? 0);
                }
                else
                {
                    ViewBag.Customers = new List<StoreAPI.Models.Customer>();
                }
                
                if (productResponse.IsSuccessStatusCode)
                {
                    var productsJson = await productResponse.Content.ReadAsStringAsync();
                    var products = JsonSerializer.Deserialize<List<StoreAPI.Models.Product>>(productsJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    ViewBag.Products = products ?? new List<StoreAPI.Models.Product>();
                    _logger.LogInformation("Loaded {Count} products", products?.Count ?? 0);
                }
                else
                {
                    ViewBag.Products = new List<StoreAPI.Models.Product>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading select list data");
                ViewBag.Customers = new List<StoreAPI.Models.Customer>();
                ViewBag.Products = new List<StoreAPI.Models.Product>();
                ViewData["Error"] = $"Không thể tải dữ liệu: {ex.Message}";
            }
        }
    }
}
