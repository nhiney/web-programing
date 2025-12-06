using System;
using System.Linq;
using System.Web.Mvc;
using WebBanGIay.Models;

namespace WebBanGIay.Controllers
{
    public class ImportAdminController : Controller
    {
        private readonly QuanLyBanGiayEntities1 db = new QuanLyBanGiayEntities1();

        // ======================= CREATE IMPORT SLIP =======================
        public ActionResult Create()
        {
            ViewBag.Products = db.SANPHAM.Select(s => new {
                s.MASANPHAM,
                DisplayText = s.TENSANPHAM + " (" + s.SOLUONGTON + " available)"
            }).ToList();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(string MASANPHAM, int? SOLUONG, decimal? GIANHAP)
        {
            if (string.IsNullOrEmpty(MASANPHAM))
                ModelState.AddModelError("MASANPHAM", "Vui lòng chọn sản phẩm!");

            if (SOLUONG == null || SOLUONG <= 0)
                ModelState.AddModelError("SOLUONG", "Số lượng nhập phải lớn hơn 0!");

            // Giá nhập có thể tùy chọn, nhưng nếu có thì validates
            if (GIANHAP != null && GIANHAP < 0)
                ModelState.AddModelError("GIANHAP", "Giá nhập không hợp lệ!");

            if (!ModelState.IsValid)
            {
                ViewBag.Products = db.SANPHAM.Select(s => new {
                    s.MASANPHAM,
                    DisplayText = s.TENSANPHAM + " (" + s.SOLUONGTON + " available)"
                }).ToList();
                return View();
            }

            var product = db.SANPHAM.Find(MASANPHAM);
            if (product == null) return HttpNotFound();

            // Cập nhật số lượng tồn
            product.SOLUONGTON = (product.SOLUONGTON ?? 0) + SOLUONG;

            // Nếu muốn cập nhật giá bán theo giá nhập? (Thường là không, chỉ update stock)
            // product.GIA = ...

            db.SaveChanges();

            TempData["Success"] = $"Nhập hàng thành công! Đã thêm {SOLUONG} sản phẩm vào kho.";
            return RedirectToAction("Index", "ProductAdmin"); // Quay về danh sách SP
        }
    }
}
