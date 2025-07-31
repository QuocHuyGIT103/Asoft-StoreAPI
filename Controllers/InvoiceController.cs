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
    public class InvoiceController : ControllerBase
    {
        private readonly DataAccess _da;

        public InvoiceController(DataAccess da)
        {
            _da = da;
        }

        [HttpGet]
        public IActionResult GetInvoices()
        {
            var dt = _da.ExecuteQuery("SELECT i.InvoiceID, i.CustomerID, c.CustomerName, i.InvoiceDate, i.TotalPrice FROM Invoice i JOIN Customer c ON i.CustomerID = c.CustomerID");
            var invoices = new List<Invoice>();

            foreach (DataRow row in dt.Rows)
            {
                var invoice = new Invoice
                {
                    InvoiceID = row["InvoiceID"].ToString(),
                    CustomerID = row["CustomerID"].ToString(),
                    InvoiceDate = Convert.ToDateTime(row["InvoiceDate"]),
                    TotalPrice = row["TotalPrice"] != DBNull.Value ? Convert.ToDecimal(row["TotalPrice"]) : 0,
                    Details = new List<InvoiceDetails>()
                };
                invoices.Add(invoice);
            }

            foreach (var invoice in invoices)
            {
                var detaillDt = _da.ExecuteQuery("SELECT * FROM InvoiceDetails WHERE InvoiceID = @InvoiceID", new[] { new SqlParameter("@InvoiceID", invoice.InvoiceID) });

                foreach (DataRow detailRow in detaillDt.Rows)
                {
                    invoice.Details.Add(new InvoiceDetails
                    {
                        InvoiceDetailID = Convert.ToInt32(detailRow["InvoiceDetailID"]),
                        InvoiceID = detailRow["InvoiceID"].ToString(),
                        ProductID = detailRow["ProductID"].ToString(),
                        Quantity = Convert.ToInt32(detailRow["Quantity"]),
                        TotalPrice = detailRow["TotalPrice"] != DBNull.Value ? Convert.ToDecimal(detailRow["TotalPrice"]) : 0

                    });
                }
            }
            return Ok(invoices);
        }

        [HttpPost]
        public IActionResult AddInvoice([FromBody] Invoice model)
        {
            if (string.IsNullOrWhiteSpace(model.InvoiceID) || string.IsNullOrWhiteSpace(model.CustomerID) || model.Details == null || !model.Details.Any())
                return BadRequest("Dữ liệu không hợp lệ.");

            // Kiểm tra CustomerID tồn tại
            var checkCustomerQuery = "SELECT COUNT(*) FROM Customer WHERE CustomerID = @CustomerID";
            var checkCustomerParams = new[] { new SqlParameter("@CustomerID", model.CustomerID.Trim().ToUpper()) };
            var customerCount = Convert.ToInt32(_da.ExecuteQuery(checkCustomerQuery, checkCustomerParams).Rows[0][0]);
            if (customerCount == 0)
                return BadRequest("CustomerID không hợp lệ.");

            // Kiểm tra InvoiceID trùng
            string checkQuery = "SELECT COUNT(*) FROM Invoice WHERE InvoiceID = @InvoiceID";
            var checkParams = new[] { new SqlParameter("@InvoiceID", model.InvoiceID.Trim().ToUpper()) };
            var dt = _da.ExecuteQuery(checkQuery, checkParams);
            if (Convert.ToInt32(dt.Rows[0][0]) > 0)
                return BadRequest("Mã hóa đơn đã tồn tại!");

            decimal totalPrice = 0;
            foreach (var detail in model.Details)
            {
                var priceDt = _da.ExecuteQuery("SELECT Price FROM Product WHERE ProductID = @ProductID", new[] { new SqlParameter("@ProductID", detail.ProductID.Trim().ToUpper()) });
                if (priceDt.Rows.Count == 0)
                    return BadRequest($"Sản phẩm {detail.ProductID} không tồn tại.");
                decimal price = Convert.ToDecimal(priceDt.Rows[0]["Price"]);
                if (detail.Quantity <= 0)
                    return BadRequest("Số lượng phải lớn hơn 0.");
                detail.TotalPrice = price * detail.Quantity;
                detail.ProductID = detail.ProductID.Trim().ToUpper();
                totalPrice += detail.TotalPrice;
            }

            using (var connection = new SqlConnection(_da.GetConnectionString()))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string insertInvoiceQuery = "INSERT INTO Invoice (InvoiceID, CustomerID, InvoiceDate, TotalPrice) VALUES (@InvoiceID, @CustomerID, @InvoiceDate, @TotalPrice)";
                        var invoiceParams = new[]
                        {
                            new SqlParameter("@InvoiceID", model.InvoiceID.Trim().ToUpper()),
                            new SqlParameter("@CustomerID", model.CustomerID.Trim().ToUpper()),
                            new SqlParameter("@InvoiceDate", DateTime.Now),
                            new SqlParameter("@TotalPrice", totalPrice)
                        };
                        _da.ExecuteNonQueryTrans(insertInvoiceQuery, invoiceParams, connection, transaction);

                        foreach (var detail in model.Details)
                        {
                            string insertDetailQuery = "INSERT INTO InvoiceDetails (InvoiceID, ProductID, Quantity, TotalPrice) VALUES (@InvoiceID, @ProductID, @Quantity, @TotalPrice)";
                            var detailParams = new[]
                            {
                                new SqlParameter("@InvoiceID", model.InvoiceID.Trim().ToUpper()),
                                new SqlParameter("@ProductID", detail.ProductID),
                                new SqlParameter("@Quantity", detail.Quantity),
                                new SqlParameter("@TotalPrice", detail.TotalPrice)
                            };
                            _da.ExecuteNonQueryTrans(insertDetailQuery, detailParams, connection, transaction);
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return StatusCode(500, $"Lỗi khi lưu hóa đơn: {ex.Message}");
                    }
                }
            }
            return Ok("Thêm hóa đơn thành công.");
        }

        [HttpGet("{id}")]
        public IActionResult GetInvoiceById(string id)
        {
            var dt = _da.ExecuteQuery("SELECT i.InvoiceID, i.CustomerID, c.CustomerName, i.InvoiceDate, i.TotalPrice FROM Invoice i JOIN Customer c ON i.CustomerID = c.CustomerID WHERE i.InvoiceID = @InvoiceID",
                new[] { new SqlParameter("@InvoiceID", id) });

            if (dt.Rows.Count == 0)
                return NotFound("Hóa đơn không tồn tại.");

            var invoice = new Invoice
            {
                InvoiceID = dt.Rows[0]["InvoiceID"].ToString(),
                CustomerID = dt.Rows[0]["CustomerID"].ToString(),
                InvoiceDate = Convert.ToDateTime(dt.Rows[0]["InvoiceDate"]),
                TotalPrice = dt.Rows[0]["TotalPrice"] != DBNull.Value ? Convert.ToDecimal(dt.Rows[0]["TotalPrice"]) : 0,
                Details = new List<InvoiceDetails>()
            };

            var detailDt = _da.ExecuteQuery("SELECT * FROM InvoiceDetails WHERE InvoiceID = @InvoiceID",
                new[] { new SqlParameter("@InvoiceID", id) });

            foreach (DataRow detailRow in detailDt.Rows)
            {
                invoice.Details.Add(new InvoiceDetails
                {
                    InvoiceDetailID = Convert.ToInt32(detailRow["InvoiceDetailID"]),
                    InvoiceID = detailRow["InvoiceID"].ToString(),
                    ProductID = detailRow["ProductID"].ToString(),
                    Quantity = Convert.ToInt32(detailRow["Quantity"]),
                    TotalPrice = detailRow["TotalPrice"] != DBNull.Value ? Convert.ToDecimal(detailRow["TotalPrice"]) : 0
                });
            }

            return Ok(invoice);
        }

        [HttpPut("{id}")]
        public IActionResult UpdateInvoice(string id, [FromBody] Invoice model)
        {
            if (id?.Trim().ToUpper() != model.InvoiceID?.Trim().ToUpper())
            {
                return BadRequest("Dữ liệu không hợp lệ: InvoiceID không khớp.");
            }


            if (string.IsNullOrWhiteSpace(model.CustomerID))
            {
                
                return BadRequest("Dữ liệu không hợp lệ: CustomerID không được để trống.");
            }

            if (model.Details == null || !model.Details.Any())
            {
                return BadRequest("Dữ liệu không hợp lệ: Phải có ít nhất một chi tiết hóa đơn.");
            }

            // Kiểm tra CustomerID tồn tại
            var checkCustomerQuery = "SELECT COUNT(*) FROM Customer WHERE CustomerID = @CustomerID";
            var checkCustomerParams = new[] { new SqlParameter("@CustomerID", model.CustomerID.Trim().ToUpper()) };
            var customerCount = Convert.ToInt32(_da.ExecuteQuery(checkCustomerQuery, checkCustomerParams).Rows[0][0]);
            if (customerCount == 0)
            {
                return BadRequest($"CustomerID {model.CustomerID} không hợp lệ.");
            }

            decimal totalPrice = 0;
            foreach (var detail in model.Details)
            {
                var priceDt = _da.ExecuteQuery("SELECT Price FROM Product WHERE ProductID = @ProductID", new[] { new SqlParameter("@ProductID", detail.ProductID.Trim().ToUpper()) });
                if (priceDt.Rows.Count == 0)
                {
                    return BadRequest($"Sản phẩm {detail.ProductID} không tồn tại.");
                }
                decimal price = Convert.ToDecimal(priceDt.Rows[0]["Price"]);
                if (detail.Quantity <= 0)
                {
                    return BadRequest("Số lượng phải lớn hơn 0.");
                }
                detail.TotalPrice = price * detail.Quantity;
                detail.ProductID = detail.ProductID.Trim().ToUpper();
                totalPrice += detail.TotalPrice;
            }

            using (var connection = new SqlConnection(_da.GetConnectionString()))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string deleteDetailsQuery = "DELETE FROM InvoiceDetails WHERE InvoiceID = @InvoiceID";
                        _da.ExecuteNonQueryTrans(deleteDetailsQuery, new[] { new SqlParameter("@InvoiceID", id) }, connection, transaction);

                        string updateInvoiceQuery = "UPDATE Invoice SET CustomerID = @CustomerID, InvoiceDate = @InvoiceDate, TotalPrice = @TotalPrice WHERE InvoiceID = @InvoiceID";
                        var updateParams = new[]
                        {
                    new SqlParameter("@CustomerID", model.CustomerID.Trim().ToUpper()),
                    new SqlParameter("@InvoiceDate", DateTime.Now),
                    new SqlParameter("@TotalPrice", totalPrice),
                    new SqlParameter("@InvoiceID", model.InvoiceID.Trim().ToUpper())
                };
                        _da.ExecuteNonQueryTrans(updateInvoiceQuery, updateParams, connection, transaction);

                        foreach (var detail in model.Details)
                        {
                            string insertDetailQuery = "INSERT INTO InvoiceDetails (InvoiceID, ProductID, Quantity, TotalPrice) VALUES (@InvoiceID, @ProductID, @Quantity, @TotalPrice)";
                            var detailParams = new[]
                            {
                        new SqlParameter("@InvoiceID", model.InvoiceID.Trim().ToUpper()),
                        new SqlParameter("@ProductID", detail.ProductID),
                        new SqlParameter("@Quantity", detail.Quantity),
                        new SqlParameter("@TotalPrice", detail.TotalPrice)
                    };
                            _da.ExecuteNonQueryTrans(insertDetailQuery, detailParams, connection, transaction);
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        
                        return StatusCode(500, $"Lỗi khi cập nhật hóa đơn: {ex.Message}");
                    }
                }
            }
            return Ok("Cập nhật hóa đơn thành công.");
        }


        [HttpDelete("{id}")]
        public IActionResult DeleteInvoice(string id)
        {
            // Kiểm tra InvoiceID tồn tại
            var checkQuery = "SELECT COUNT(*) FROM Invoice WHERE InvoiceID = @InvoiceID";
            var checkParams = new[] { new SqlParameter("@InvoiceID", id) };
            var dt = _da.ExecuteQuery(checkQuery, checkParams);
            if (Convert.ToInt32(dt.Rows[0][0]) == 0)
                return NotFound("Hóa đơn không tồn tại.");

            using (var connection = new SqlConnection(_da.GetConnectionString()))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string deleteDetailsQuery = "DELETE FROM InvoiceDetails WHERE InvoiceID = @InvoiceID";
                        _da.ExecuteNonQueryTrans(deleteDetailsQuery, new[] { new SqlParameter("@InvoiceID", id) }, connection, transaction);

                        string deleteQuery = "DELETE FROM Invoice WHERE InvoiceID = @InvoiceID";
                        _da.ExecuteNonQueryTrans(deleteQuery, new[] { new SqlParameter("@InvoiceID", id) }, connection, transaction);

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return StatusCode(500, $"Lỗi khi xóa hóa đơn: {ex.Message}");
                    }
                }
            }
            return Ok("Xóa hóa đơn thành công.");
        }
    }
}
