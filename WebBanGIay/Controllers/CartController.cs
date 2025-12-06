using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebBanGIay.Models;

namespace WebBanGIay.Controllers
{
    public class CartController : Controller
    {
        private readonly QuanLyBanGiayEntities1 db = new QuanLyBanGiayEntities1();
        private readonly CartService cartService = new CartService();

        // GET: Cart
        public ActionResult Index()
        {
            var cart = cartService.GetCart();
            ViewBag.TongTien = cartService.TongTien();

            if (cart == null || cart.Count == 0)
            {
                var sanPhamGoiY = db.SANPHAM
                    .Where(s => s.GIAKHUYENMAI > 0 && s.SOLUONGTON > 0)
                    .OrderByDescending(s => s.GIAKHUYENMAI)
                    .Take(4)
                    .ToList();
                ViewBag.SanPhamGoiY = sanPhamGoiY;
            }

            return View(cart);
        }

        // [HttpPost] - NHẬN TỪ FORM (có AntiForgeryToken + sizeId)
        // [HttpPost] - Thêm vào giỏ hành (Nhận từ Form)
        // [HttpPost] - Thêm vào giỏ hành (Nhận từ Form)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Add(string id, int soLuong = 1, string sizeId = null)
        {
            // Kiểm tra đăng nhập
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Request.UrlReferrer?.ToString() ?? Url.Action("Index", "TrangChu") });
            }

            // Bắt buộc phải có id và sizeId
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(sizeId))
            {
                TempData["Error"] = "Vui lòng chọn kích cỡ sản phẩm!";
                return Redirect(Request.UrlReferrer != null ? Request.UrlReferrer.ToString() : Url.Action("Index", "TrangChu"));
            }

            var sp = db.SANPHAM.FirstOrDefault(x => x.MASANPHAM.Trim() == id.Trim());
            if (sp == null)
                return HttpNotFound();

            // Tạo ID duy nhất cho giỏ hàng theo biến thể (sản phẩm + size)
            string cartItemId = $"{sp.MASANPHAM.Trim()}-{sizeId.Trim()}";
            string tenHienThi = $"{sp.TENSANPHAM} (Size: {sizeId.Trim()})";
            decimal gia = sp.GIAKHUYENMAI ?? sp.GIA;

            cartService.Add(cartItemId, tenHienThi, sp.HINHANH, gia, soLuong);

            TempData["Success"] = "Đã thêm vào giỏ hàng!";
            // Quay lại trang trước đó (chi tiết sản phẩm hoặc danh sách)
            return Redirect(Request.UrlReferrer?.ToString() ?? Url.Action("Index", "TrangChu"));
        }
        // POST: /Cart/AddAndCheckout → Dành riêng cho nút "Mua ngay"
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddAndCheckout(string id, int soLuong = 1, string sizeId = null)
        {
            // Kiểm tra đăng nhập
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Request.UrlReferrer?.ToString() ?? Url.Action("Index", "TrangChu") });
            }

            // === PHẦN NÀY GIỐNG HỆT ACTION ADD CỦA BẠN ===
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(sizeId))
            {
                TempData["Error"] = "Vui lòng chọn kích cỡ để mua ngay!";
                return Redirect(Request.UrlReferrer != null ? Request.UrlReferrer.ToString() : Url.Action("Index", "TrangChu"));
            }

            var sp = db.SANPHAM.FirstOrDefault(x => x.MASANPHAM != null && x.MASANPHAM.Trim() == id.Trim());
            if (sp == null)
                return HttpNotFound();

            string cartItemId = $"{sp.MASANPHAM.Trim()}-{sizeId.Trim()}";
            string tenHienThi = $"{sp.TENSANPHAM} (Size: {sizeId})";
            decimal gia = sp.GIAKHUYENMAI ?? sp.GIA;

            cartService.Add(cartItemId, tenHienThi, sp.HINHANH, gia, soLuong);

            // === QUAN TRỌNG: ĐI THẲNG VÀO GIỎ HÀNG ===
            return RedirectToAction("Index", "Cart");
        }
        // [HttpPost] Cập nhật số lượng trong giỏ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(string id, int soLuong)
        {
            if (string.IsNullOrWhiteSpace(id))
                return new HttpStatusCodeResult(400);

            if (soLuong > 0)
                cartService.Update(id.Trim(), soLuong);
            else
                cartService.Remove(id.Trim());

            return RedirectToAction("Index");
        }

        // Xóa 1 sản phẩm
        public ActionResult Remove(string id)
        {
            if (!string.IsNullOrWhiteSpace(id))
                cartService.Remove(id.Trim());

            return RedirectToAction("Index");
        }

        // Làm trống giỏ hàng
        public ActionResult Clear()
        {
            cartService.Clear();
            return RedirectToAction("Index");
        }

        // Trang thanh toán
        public ActionResult Checkout()
        {
            var cart = cartService.GetCart();
            if (cart == null || cart.Count == 0)
                return RedirectToAction("Index");

            return View(cart);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}