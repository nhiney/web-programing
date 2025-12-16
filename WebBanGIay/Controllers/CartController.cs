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

            ViewBag.TongTien = cartService.TongTien();

            // Luôn gợi ý sản phẩm (Random hoặc mới nhất)
            var sanPhamGoiY = db.SANPHAM
                .Where(s => s.SOLUONGTON > 0)
                .OrderBy(x => Guid.NewGuid()) // Random
                .Take(4)
                .ToList();
            ViewBag.SanPhamGoiY = sanPhamGoiY;

            return View(cart);
        }
        public ActionResult Add(string id, int soLuong, int? bienTheId, int? size)
        {
            int finalSize = size ?? 0;

            if (!bienTheId.HasValue || finalSize == 0)
            {
                TempData["Error"] = "Vui lòng chọn màu và size!";
                return RedirectToAction("ChiTiet", "Products", new { id = id });
            }

            int btId = bienTheId.Value;
            int sz = finalSize;

            // Lấy sản phẩm
            var sp = db.SANPHAM.FirstOrDefault(x => x.MASANPHAM.Trim() == id.Trim());
            if (sp == null) return HttpNotFound();

            var bienThe = db.BIEN_THE_SAN_PHAM.FirstOrDefault(b => b.ID == btId);
            if (bienThe == null) return new HttpStatusCodeResult(400);

            var tonKho = db.TONKHO_SIZE.FirstOrDefault(tk => tk.IDBienThe == bienThe.ID && tk.SIZE == sz);
            if (tonKho == null) return new HttpStatusCodeResult(400);

            string sizeStr = tonKho.SIZE.ToString();
            string mau = bienThe.MAUSAC;

            string tenHienThi = $"{sp.TENSANPHAM} (Size: {sizeStr}, Màu: {mau})";
            decimal gia = bienThe.GIATHEOMAU;

            cartService.Add(sp.MASANPHAM.Trim(), tenHienThi, sp.HINHANH, gia, soLuong, sizeStr, mau);

            if (Request.UrlReferrer != null)
                return Redirect(Request.UrlReferrer.ToString());

            return RedirectToAction("Index", "Home");
        }



        public ActionResult AddNOW(string id, int soLuong, int bienTheId, int size)
        {
            // Kiểm tra đăng nhập
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account", new
                {
                    returnUrl = Request.UrlReferrer?.ToString() ?? Url.Action("Index", "TrangChu")
                });
            }

            if (string.IsNullOrWhiteSpace(id))
                return new HttpStatusCodeResult(400);

            var sp = db.SANPHAM.FirstOrDefault(x => x.MASANPHAM.Trim() == id.Trim());
            if (sp == null) return HttpNotFound();

            var bienThe = db.BIEN_THE_SAN_PHAM.FirstOrDefault(b => b.ID == bienTheId);
            if (bienThe == null) return new HttpStatusCodeResult(400);

            var tonKho = db.TONKHO_SIZE.FirstOrDefault(tk => tk.IDBienThe == bienThe.ID && tk.SIZE == size);
            if (tonKho == null) return new HttpStatusCodeResult(400);

            string sizeStr = tonKho.SIZE.ToString();
            string mau = bienThe.MAUSAC;

            string tenHienThi = $"{sp.TENSANPHAM} (Size: {sizeStr}, Màu: {mau})";
            decimal gia = bienThe.GIATHEOMAU;

            cartService.Add(
                sp.MASANPHAM.Trim(),
                tenHienThi,
                sp.HINHANH,
                gia,
                soLuong,
                sizeStr,
                mau
            );

            return RedirectToAction("Index", "Cart");
        }

        // Mua ngay (Thêm vào giỏ + Chuyển đến thanh toán)
        [HttpPost]
        public ActionResult AddNOW(string id, int soLuong, int? bienTheId, int? size)
        {
             int finalSize = size ?? 0;
             // Validations
             if (!bienTheId.HasValue || finalSize == 0) 
             {
                 TempData["Error"] = "Vui lòng chọn màu và size!";
                 return RedirectToAction("ChiTiet", "Products", new { id = id });
             }

             // Logic retrieval
             int btId = bienTheId.Value;
             var sp = db.SANPHAM.FirstOrDefault(x => x.MASANPHAM.Trim() == id.Trim());
             if (sp == null) return HttpNotFound();
             
             var bienThe = db.BIEN_THE_SAN_PHAM.FirstOrDefault(b => b.ID == btId);
             string mau = bienThe?.MAUSAC?.Trim() ?? "Mặc định";
             string sizeStr = finalSize.ToString();
             
             decimal gia = sp.GIAKHUYENMAI ?? sp.GIA;
             string tenHienThi = sp.TENSANPHAM;

             // Add using service
             cartService.Add(sp.MASANPHAM.Trim(), tenHienThi, sp.HINHANH, gia, soLuong, sizeStr, mau);
             
             // Redirect to Payment
             return RedirectToAction("Index", "Payment");
        }






        // Cập nhật số lượng sản phẩm
        [HttpPost]
        public ActionResult Update(string id, string size, string mau, int soLuong)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(size) || string.IsNullOrWhiteSpace(mau))
                return new HttpStatusCodeResult(400);

            if (soLuong > 0)
                cartService.Update(id.Trim(), size.Trim(), mau.Trim(), soLuong);
            else
                cartService.Remove(id.Trim(), size.Trim(), mau.Trim());

            return RedirectToAction("Index");
        }

        // Xóa 1 sản phẩm
        public ActionResult Remove(string id, string size, string mau)
        {
            if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(size) && !string.IsNullOrWhiteSpace(mau))
            {
                cartService.Remove(id.Trim(), size.Trim(), mau.Trim());
            }
            return RedirectToAction("Index");
        }

        // Xóa toàn bộ giỏ hàng
        public ActionResult Clear()
        {
            cartService.Clear();
            return RedirectToAction("Index");
        }

        // Checkout
        public ActionResult Checkout()
        {
            var cart = cartService.GetCart();
            if (cart == null || cart.Count == 0)
                return RedirectToAction("Index");

            // Redirect to Payment Gateway Selection instead of view
            return RedirectToAction("Index", "Payment");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();
            base.Dispose(disposing);
        }
    }
}
