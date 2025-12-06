using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Web.Mvc;
using WebBanGIay.Models;
public class OrderController : Controller
{

    [HttpPost]
    public ActionResult Checkout()
    {
        // Đọc dữ liệu JSON từ request body
        Request.InputStream.Position = 0;
        string jsonData = new StreamReader(Request.InputStream).ReadToEnd();

        // Deserialize sang ViewModel
        var model = JsonConvert.DeserializeObject<CheckoutViewModel>(jsonData);

        if (model.Cart == null || model.Cart.Count == 0)
            return Json(new { success = false, message = "Giỏ hàng trống" });
        int voucherValue = 0;
        if (!string.IsNullOrEmpty(model.Voucher))
        {
            int.TryParse(model.Voucher, out voucherValue);
        }

        using (var db = new QuanLyBanGiayEntities1())
        {
            foreach (var item in model.Cart)
            {
                db.Database.ExecuteSqlCommand("EXEC InsertOrderItem @MaSP, @SoLuong, @Voucher, @Address, @PaymentMethod",
                    new SqlParameter("@MaSP", item.MaSP),
                    new SqlParameter("@SoLuong", item.SoLuong),

                    new SqlParameter("@Voucher", voucherValue),

                    new SqlParameter("@Address", model.Address ?? ""),
                    new SqlParameter("@PaymentMethod", model.PaymentMethod ?? ""));
            }
        }

        TempData["Message"] = "Đặt hàng thành công!";
        return RedirectToAction("Cart");
    }

}
