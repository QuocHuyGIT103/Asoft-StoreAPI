using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace StoreAPI.MvcControllers
{
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<HomeController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public IActionResult Index()
        {
            // Debug cookie trong Index
            var token = Request.Cookies["token"];
            ViewBag.HasToken = !string.IsNullOrEmpty(token);
           
            return View();
        }

        public IActionResult Login()
        {
            // Debug cookie trong Login
            var existingToken = Request.Cookies["token"];
            _logger.LogInformation("Login page accessed, existing token: {HasToken}", !string.IsNullOrEmpty(existingToken));
            
            if (!string.IsNullOrEmpty(existingToken))
            {
                _logger.LogInformation("Redirecting to Index due to existing token");
                return RedirectToAction("Index");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            try
            {
                _logger.LogInformation("Login attempt for username: {Username}", username);

                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(_configuration["ApiBaseUrl"] ?? "https://localhost:5000");
                
                var loginData = new { Username = username, Password = password };
                var json = JsonSerializer.Serialize(loginData);
                _logger.LogInformation("Sending login data: {Json}", json);
                
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("/api/auth/login", content);
                _logger.LogInformation("Login API response status: {StatusCode}", response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync(); // SỬA LỖI TẠI ĐÂY
                    _logger.LogInformation("Login API response: {Response}", result);

                    // Parse JSON response với case-insensitive
                    var tokenResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(result, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (tokenResponse != null && tokenResponse.ContainsKey("token"))
                    {
                        var token = tokenResponse["token"].ToString();
                        _logger.LogInformation("Token received successfully: {TokenLength} characters", token?.Length ?? 0);

                        // Set cookie với settings phù hợp
                        var cookieOptions = new CookieOptions
                        {
                            HttpOnly = false, // TẠM THỜI SET FALSE ĐỂ DEBUG
                            Secure = false,   // TẠM THỜI SET FALSE ĐỂ DEBUG
                            SameSite = SameSiteMode.Lax,
                            Expires = DateTimeOffset.Now.AddHours(8),
                            Path = "/" // Đảm bảo cookie có sẵn trên toàn bộ site
                        };

                        Response.Cookies.Append("token", token, cookieOptions);
                        _logger.LogInformation("Cookie set successfully with options: HttpOnly={HttpOnly}, Secure={Secure}", 
                            cookieOptions.HttpOnly, cookieOptions.Secure);

                        // Verify cookie được set
                        var verifyToken = Request.Cookies["token"];
                        _logger.LogInformation("Cookie verification after set: {Exists}", !string.IsNullOrEmpty(verifyToken));

                        TempData["Success"] = "Đăng nhập thành công!";
                        return RedirectToAction("Index");
                    }
                    else
                    { 
                        ModelState.AddModelError("", "Phản hồi không hợp lệ từ server.");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Login failed with status {StatusCode}: {Error}", response.StatusCode, errorContent);
                    ModelState.AddModelError("", "Đăng nhập thất bại. Vui lòng kiểm tra lại thông tin.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during login");
                ModelState.AddModelError("", $"Lỗi hệ thống: {ex.Message}");
            }

            return View();
        }

        public IActionResult Logout()
        {
            var tokenExists = Request.Cookies["token"];
            _logger.LogInformation("Logout called, token exists: {HasToken}", !string.IsNullOrEmpty(tokenExists));
            
            Response.Cookies.Delete("token");
            TempData["Success"] = "Đăng xuất thành công!";
            return RedirectToAction("Login");
        }
    }
}
