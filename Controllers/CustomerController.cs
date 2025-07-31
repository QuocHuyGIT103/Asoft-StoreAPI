using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
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
    public class CustomerController : ControllerBase
    {
        private readonly DataAccess _da;

        public CustomerController(DataAccess da)
        {
            _da = da;
        }

        [HttpGet]
        public IActionResult GetCustomers()
        {
            var dt = _da.ExecuteQuery("SELECT * FROM Customer");
            var customers = new List<Customer>();
            foreach (DataRow row in dt.Rows)
            {
                customers.Add(new Customer
                {
                    CustomerID = row["CustomerID"].ToString(),
                    CustomerName = row["CustomerName"].ToString(),
                    Phone = row["Phone"].ToString()
                });
            }
            return Ok(customers);
        }

        [HttpPost]
        public IActionResult AddCustomer([FromBody] Customer model)
        {
            if (string.IsNullOrWhiteSpace(model.CustomerID) || string.IsNullOrWhiteSpace(model.CustomerName))
            {
                return BadRequest("Dữ liệu không hợp lệ");
            }

            string query = "SELECT COUNT (*) FROM Customer WHERE CustomerID = @CustomerID";
            var checkParam = new[] { new SqlParameter("@CustomerID", model.CustomerID.Trim().ToUpper()) };

            var dt = _da.ExecuteQuery(query, checkParam);

            if (Convert.ToInt32(dt.Rows[0][0]) > 0)
                return BadRequest("Mã khách hàng đã tồn tại!");

            string insertQuery = "INSERT INTO Customer (CustomerID, CustomerName, Phone) VALUES (@CustomerID, @CustomerName, @Phone)";
            var insertParams = new[]
            {
                 new SqlParameter("@CustomerID", model.CustomerID.Trim().ToUpper()),
                 new SqlParameter("@CustomerName", model.CustomerName),
                 new SqlParameter("@Phone", (object) model.Phone ?? DBNull.Value)
             };

            _da.ExecuteQuery(insertQuery, insertParams);

            return Ok("Thêm khách hàng thành công!");

        }

        [HttpGet("{id}")]
        public IActionResult GetCustomerById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("Mã khách hàng không hợp lệ.");

            string query = "SELECT * FROM Customer WHERE CustomerID = @CustomerID";
            var param = new[] { new SqlParameter("@CustomerID", id.Trim().ToUpper()) };
            var dt = _da.ExecuteQuery(query, param);

            if (dt.Rows.Count == 0)
                return NotFound("Không tìm thấy khách hàng.");

            var row = dt.Rows[0];
            var customer = new Customer
            {
                CustomerID = row["CustomerID"].ToString(),
                CustomerName = row["CustomerName"].ToString(),
                Phone = row["Phone"].ToString()
            };

            return Ok(customer);
        }

        [HttpPut("{id}")]
        public IActionResult UpdateCustomer(string id, [FromBody] Customer model)
        {
            if (id != model.CustomerID || string.IsNullOrWhiteSpace(model.CustomerName))
                return BadRequest("Dữ liệu không hợp lệ.");

            string updateQuery = "UPDATE Customer SET CustomerName = @CustomerName, Phone = @Phone WHERE CustomerID = @CustomerID";
            var updateParams = new[]
            {
                 new SqlParameter("@CustomerName", model.CustomerName),
                 new SqlParameter("@Phone", (object) model.Phone ?? DBNull.Value),
                 new SqlParameter("@CustomerID", model.CustomerID)
             };

            _da.ExecuteQuery(updateQuery, updateParams);
            return Ok("Cập nhật khách hàng thành công.");

        }

        [HttpDelete("{id}")]
        public IActionResult DeleteCustomer(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("Mã khách hàng không hợp lệ.");

            string checkQuery = "SELECT COUNT(*) FROM Customer WHERE CustomerID = @CustomerID";
            var checkExistParams = new[] { new SqlParameter("@CustomerID", id) };
            var dt = _da.ExecuteQuery(checkQuery, checkExistParams);
            if (dt.Rows.Count == 0 || Convert.ToInt32(dt.Rows[0][0]) == 0)
                return NotFound("Khách hàng không tồn tại.");


            string query = "SELECT COUNT(*) FROM Invoice WHERE CustomerID = @CustomerID";
            var checkParams = new[] { new SqlParameter("@CustomerID", id) };

            dt = _da.ExecuteQuery(query, checkParams);   
            if (Convert.ToInt32(dt.Rows[0][0]) > 0)
                return BadRequest("Không thể xóa khách hàng vì đã có hóa đơn liên quan.");

            string deleteQuery = "DELETE FROM Customer WHERE CustomerID = @CustomerID";
            var deleteParams = new[] { new SqlParameter("@CustomerID", id) };

            _da.ExecuteQuery(deleteQuery, deleteParams);

            return Ok("Xóa khách hàng thành công.");


        }

    }
}
