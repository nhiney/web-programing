using System;
using System.Linq;
using System.Web.Mvc;
using WebBanGIay.Models;

namespace WebBanGIay.Controllers
{
    public class ImportAdminController : Controller
    {
        private readonly QuanLyBanGiayEntities1 db = new QuanLyBanGiayEntities1();

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            ViewBag.NewOrdersCount = db.HOADON.Count(o => o.TRANGTHAI == "CHỜ XỬ LÝ");
        }

        // ======================= CREATE IMPORT SLIP =======================
        public ActionResult Create()
        {
            // Use SelectListItem instead of anonymous type for Razor compatibility
            ViewBag.Products = db.SANPHAM.ToList().Select(s => new SelectListItem
            {
                Value = s.MASANPHAM,
                Text = s.TENSANPHAM + " (Tồn: " + (s.SOLUONGTON ?? 0) + ")"
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

            if (GIANHAP != null && GIANHAP < 0)
                ModelState.AddModelError("GIANHAP", "Giá nhập không hợp lệ!");

            if (!ModelState.IsValid)
            {
                ViewBag.Products = db.SANPHAM.ToList().Select(s => new SelectListItem
                {
                    Value = s.MASANPHAM,
                    Text = s.TENSANPHAM + " (Tồn: " + (s.SOLUONGTON ?? 0) + ")"
                }).ToList();
                return View();
            }

            var product = db.SANPHAM.Find(MASANPHAM);
            if (product == null) return HttpNotFound();

            // Cập nhật số lượng tồn
            product.SOLUONGTON = (product.SOLUONGTON ?? 0) + SOLUONG;

            db.SaveChanges();

            TempData["Success"] = $"Nhập hàng thành công! Đã thêm {SOLUONG} sản phẩm '{product.TENSANPHAM}' vào kho.";
            return RedirectToAction("Index", "ProductAdmin");
        }
    }
}
