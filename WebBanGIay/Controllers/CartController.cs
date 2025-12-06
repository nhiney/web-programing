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


        public ActionResult Add(string id, int soLuong, string sizeId) // <<< THÊM string sizeId VÀO ĐÂY
        {
            // 1. Kiểm tra tham số đầu vào (id: Mã sản phẩm, sizeId: Kích cỡ)
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(sizeId))
                return new HttpStatusCodeResult(400);

            // 2. Tìm Sản phẩm Gốc
            var sp = db.SANPHAM.FirstOrDefault(x => x.MASANPHAM.Trim() == id.Trim());

            if (sp == null) return HttpNotFound();

            // 3. TẠO ID BIẾN THỂ DUY NHẤT CHO GIỎ HÀNG (Sản phẩm + Kích cỡ)
            // Ví dụ: Nếu id="SHOE001" và sizeId="40", cartItemId sẽ là "SHOE001-40"
            string cartItemId = sp.MASANPHAM.Trim() + "-" + sizeId.Trim();

            // 4. Tạo Tên Sản phẩm hiển thị trong giỏ hàng (bao gồm Kích cỡ)
            string tenHienThi = sp.TENSANPHAM + " (Size: " + sizeId.Trim() + ")";

            decimal gia = sp.GIAKHUYENMAI ?? sp.GIA;

            // 5. Thêm item vào giỏ hàng với ID biến thể mới
            cartService.Add(
                cartItemId,     // Dùng ID biến thể duy nhất
                tenHienThi,     // Dùng tên đã kèm size
                sp.HINHANH,
                gia,
                soLuong
            );

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

            // ... (Logic gọi cartService.Update/Remove không đổi, vì CartService sử dụng ID này làm khóa)
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
            var cart = cartService.GetCart();  

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