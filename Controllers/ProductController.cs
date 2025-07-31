using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using StoreAPI.Data;
using StoreAPI.Models;
using System.Data;

namespace StoreAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly DataAccess _da;

        public ProductController(DataAccess da)
        {
            _da = da;
        }

        [HttpGet]
        public IActionResult GetProducts()
        {
            var dt = _da.ExecuteQuery("SELECT * FROM Product");
            var products = new List<Product>();
            foreach (DataRow row in dt.Rows)
            {
                products.Add(new Product
                {
                    ProductID = row["ProductID"].ToString(),
                    ProductName = row["ProductName"].ToString(),
                    Price = Convert.ToDecimal(row["Price"])
                });
            }
            return Ok(products);
        }

        [HttpPost]
        public IActionResult AddProduct([FromBody] Product model)
        {
            if (string.IsNullOrWhiteSpace(model.ProductID) || string.IsNullOrWhiteSpace(model.ProductName) || model.Price <= 0)
                return BadRequest("Dữ liệu không hợp lệ.");

            string checkQuery = "SELECT COUNT(*) FROM Product WHERE ProductID = @ProductID";
            var checkParams = new[] { new SqlParameter("@ProductID", model.ProductID.Trim().ToUpper()) };
            var dt = _da.ExecuteQuery(checkQuery, checkParams);

            if (Convert.ToInt32(dt.Rows[0][0]) > 0)
                return BadRequest("Mã sản phẩm đã tồn tại!");

            try
            {
                string insertQuery = "INSERT INTO Product (ProductID, ProductName, Price) VALUES (@ProductID, @ProductName, @Price)";
                var insertParams = new[]
                {
            new SqlParameter("@ProductID", model.ProductID.Trim().ToUpper()),
            new SqlParameter("@ProductName", model.ProductName),
            new SqlParameter("@Price", model.Price)
        };
                _da.ExecuteNonQuery(insertQuery, insertParams);
                return Ok(new { message = "Thêm sản phẩm thành công." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, new { message = "Lỗi khi thêm sản phẩm: " + ex.Message });
            }
        }

        [HttpGet("{id}")]
        public IActionResult GetProductById(string id)
        {
            string query = "SELECT * FROM Product WHERE ProductID = @ProductID";
            var parameters = new[] { new SqlParameter("@ProductID", id.Trim().ToUpper()) };
            var dt = _da.ExecuteQuery(query, parameters);

            if (dt.Rows.Count == 0)
                return NotFound();

            var row = dt.Rows[0];
            var product = new Product
            {
                ProductID = row["ProductID"].ToString(),
                ProductName = row["ProductName"].ToString(),
                Price = Convert.ToDecimal(row["Price"])
            };

            return Ok(product);
        }


        [HttpPut("{id}")]
        public IActionResult UpdateProduct(string id, [FromBody] Product model)
        {
            if (id != model.ProductID || string.IsNullOrWhiteSpace(model.ProductName) || model.Price <= 0)
                return BadRequest("Dữ liệu không hợp lệ.");

            string checkQuery = "SELECT COUNT(*) FROM Product WHERE ProductID = @ProductID";
            var checkParams = new[] { new SqlParameter("@ProductID", model.ProductID.Trim().ToUpper()) };
            var dt = _da.ExecuteQuery(checkQuery, checkParams);

            if (Convert.ToInt32(dt.Rows[0][0]) == 0)
                return BadRequest("Mã sản phẩm không tồn tại!");


            // Cập nhật sản phẩm
            string updateQuery = "UPDATE Product SET ProductName = @ProductName, Price = @Price WHERE ProductID = @ProductID";
            var updateParams = new[]
            {
                new SqlParameter("@ProductName", model.ProductName),
                new SqlParameter("@Price", model.Price),
                new SqlParameter("@ProductID", model.ProductID.Trim().ToUpper())
            };

            _da.ExecuteNonQuery(updateQuery, updateParams);

            return Ok("Cập nhật sản phẩm thành công.");
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteProduct(string id)
        {
            string checkExitsQuery = "SELECT COUNT(*) FROM Product WHERE ProductID = @ProductID";
            var checkExitsParams = new[] { new SqlParameter("@ProductID", id) };
            var dt = _da.ExecuteQuery(checkExitsQuery, checkExitsParams);

            if (Convert.ToInt32(dt.Rows[0][0]) == 0)
                return BadRequest("Mã sản phẩm không tồn tại!");


            string checkQuery = "SELECT COUNT(*) FROM InvoiceDetails WHERE ProductID = @ProductID";
            var checkParams = new[] { new SqlParameter("@ProductID", id) };
            dt = _da.ExecuteQuery(checkQuery, checkParams);

            if (Convert.ToInt32(dt.Rows[0][0]) > 0)
                return BadRequest("Sản phẩm đã được sử dụng trong hóa đơn, không thể xóa!");

            string deleteQuery = "DELETE FROM Product WHERE ProductID = @ProductID";
            var deleteParams = new[] { new SqlParameter("@ProductID", id) };
            _da.ExecuteNonQuery(deleteQuery, deleteParams);
            return Ok("Xóa sản phẩm thành công.");
        }
    }
}
