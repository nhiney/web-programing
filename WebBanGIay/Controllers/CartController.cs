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
        QuanLyBanGiayEntities1 db = new QuanLyBanGiayEntities1();
        // GET: Cart
        private readonly CartService cartService = new CartService();

        public ActionResult Index()
        {
            var cart = cartService.GetCart();
            ViewBag.TongTien = cartService.TongTien();
            if (cart == null || cart.Count == 0)
            {
                // === LOGIC GỢI Ý KHI GIỎ RỖNG ===
                // Lấy 4 sản phẩm khuyến mãi, còn hàng
                var sanPhamGoiY = db.SANPHAM
                    .Where(s => s.GIAKHUYENMAI > 0 && s.SOLUONGTON > 0)
                    .OrderByDescending(s => s.GIAKHUYENMAI)
                    .Take(4)
                    .ToList();

                ViewBag.SanPhamGoiY = sanPhamGoiY;
            }
            return View(cart);
        }

      
        public ActionResult Add(string id, int soLuong)
        {
            if (string.IsNullOrWhiteSpace(id))
                return new HttpStatusCodeResult(400);

            var sp = db.SANPHAM.FirstOrDefault(x => x.MASANPHAM.Trim() == id.Trim());

            if (sp == null) return HttpNotFound();

            decimal gia = sp.GIAKHUYENMAI ?? sp.GIA;

            cartService.Add(sp.MASANPHAM.Trim(), sp.TENSANPHAM, sp.HINHANH, gia, soLuong);

            if (Request.UrlReferrer != null)
            {
                // Quay lại chính trang đó (Trang chi tiết hoặc Trang chủ)
                return Redirect(Request.UrlReferrer.ToString());
            }
            else
                // Nếu không tìm được trang cũ (hiếm gặp), mới về trang chủ
                return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public ActionResult Update(string id, int soLuong)
        {
            if (string.IsNullOrWhiteSpace(id))
                return new HttpStatusCodeResult(400);

            if (soLuong > 0)
            {
                cartService.Update(id.Trim(), soLuong);
            }
            else
            {
                cartService.Remove(id.Trim());
            }

            return RedirectToAction("Index");
        }

        public ActionResult Remove(string id)
        {
            if (!string.IsNullOrWhiteSpace(id))
                cartService.Remove(id.Trim());

            return RedirectToAction("Index");
        }

        public ActionResult Clear()
        {
            cartService.Clear();
            return RedirectToAction("Index");
        }
        public ActionResult Checkout()
        {
            var cart = cartService.GetCart();   // ✅ LẤY ĐÚNG DỮ LIỆU

            if (cart == null || cart.Count == 0)
                return RedirectToAction("Index");   // tránh null / giỏ hàng rỗng

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