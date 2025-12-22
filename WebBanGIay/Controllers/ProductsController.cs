using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using WebBanGIay.Models;
using System.Collections.Generic;

namespace WebBanGIay.Controllers
{
    public class ProductsController : Controller
    {
        private readonly QuanLyBanGiayEntities1 db = new QuanLyBanGiayEntities1();

        public ActionResult ChiTiet(string id)
        {
            // 1. Check Login
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Request.Url.AbsoluteUri });
            }

            if (string.IsNullOrEmpty(id))
            {
                return HttpNotFound("Không tìm thấy sản phẩm!");
            }
            id = id.Trim();

            // Tìm mã sản phẩm chính xác từ DB (bỏ qua khoảng trắng thừa)
            var validId = db.Database.SqlQuery<string>("SELECT MASANPHAM FROM SANPHAM WHERE LTRIM(RTRIM(MASANPHAM)) = {0}", id).FirstOrDefault();

            if (validId == null) return HttpNotFound("Sản phẩm không tồn tại!");

            // Lấy sản phẩm theo mã chính xác tìm được
            var sanPham = db.SANPHAM
                .Include(s => s.NHACUNGCAP)
                .Include(s => s.KHUYENMAI)
                .FirstOrDefault(s => s.MASANPHAM == validId);

            if (sanPham == null) return HttpNotFound("Sản phẩm không tồn tại!");

            // Giá gốc hoặc giá khuyến mãi
            decimal giaGoc = sanPham.GIAKHUYENMAI ?? sanPham.GIA;

            // Sản phẩm liên quan theo giá
            var minPrice = giaGoc * 0.8m;
            var maxPrice = giaGoc * 1.2m;
            var listLienQuan = db.SANPHAM
                .Where(s => s.MASANPHAM != id && s.GIA >= minPrice && s.GIA <= maxPrice)
                .ToList();

            // Nếu không có sản phẩm cùng giá, lấy theo nhà cung cấp
            if (listLienQuan.Count == 0)
            {
                listLienQuan = db.SANPHAM
                    .Where(s => s.MANHACUNGCAP == sanPham.MANHACUNGCAP && s.MASANPHAM != id)
                    .Take(10)
                    .ToList();
            }

            ViewBag.SanPhamLienQuan = listLienQuan.OrderBy(x => Guid.NewGuid()).Take(4).ToList();
            ViewBag.HangList = db.NHACUNGCAP.Select(n => n.TENNHACUNGCAP).Distinct().ToList();

            // Danh sách đánh giá
            var danhGiaList = db.DANHGIASANPHAM
                .Where(d => d.MASANPHAM == id && d.TRANGTHAI != 0)
                .Include(d => d.KHACHHANG)
                .ToList();
            ViewBag.DanhGiaList = danhGiaList;
            ViewBag.AverageRating = danhGiaList.Any() ? danhGiaList.Average(d => d.DIEM) : 0;
            ViewBag.TotalReviews = danhGiaList.Count;

            // Lấy danh sách biến thể an toàn
            var bienTheList = db.BIEN_THE_SAN_PHAM
                .Where(bt => bt.MASANPHAM == id)
                .Select(bt => new
                {
                    bt.ID,
                    MauSac = bt.MAUSAC != null ? bt.MAUSAC.Trim() : "",
                    Sizes = bt.TONKHO_SIZE.Select(tk => new
                    {
                        Size = tk.SIZE,
                        SoLuong = tk.SOLUONG,
                        BienTheId = bt.ID
                    }).ToList()
                }).ToList();

            // Colors list
            var colors = bienTheList.Select(bt => bt.MauSac).Distinct().ToList();

            ViewBag.Colors = colors;
            ViewBag.ListBienThe = bienTheList;

            return View(sanPham);
        }




        public ActionResult DanhSach()
        {
            var sanPhams = db.SANPHAM
                .Include(s => s.NHACUNGCAP)
                .Include(s => s.KHUYENMAI)
                .OrderByDescending(s => s.NGAYTAO)
                .ToList();
            return View(sanPhams);
        }


        public async Task<ActionResult> SPhamTHuongHieu(string maNhaCungCap)
        {
            var query = db.SANPHAM.AsQueryable();
            if (!string.IsNullOrEmpty(maNhaCungCap))
            {
                string maChuan = maNhaCungCap.Trim();
                query = query.Where(p => p.MANHACUNGCAP == maChuan);
                var tenHang = db.NHACUNGCAP
                                      .Where(n => n.MANHACUNGCAP == maChuan)
                                      .Select(n => n.TENNHACUNGCAP)
                                      .FirstOrDefault();
                ViewBag.TenThuongHieu = tenHang;
            }
            query = query.Include(p => p.KHUYENMAI).OrderByDescending(p => p.NGAYTAO);
            var listSanPham = await query.ToListAsync();

            return View("SPhamTHuongHieu", listSanPham);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemDanhGia(string MASANPHAM, int DIEM, string NOIDUNG)
        {
            // 1. Kiểm tra đăng nhập
            if (Session["UserRole"]?.ToString() != "KHÁCH HÀNG")
            {
                TempData["Error"] = "Vui lòng đăng nhập tài khoản khách hàng!";
                return RedirectToAction("Login", "Account");
            }

            // 2. Lấy MAKHACHHANG từ UserID
            string userId = Session["UserID"]?.ToString();
            var taiKhoan = db.TAIKHOAN.FirstOrDefault(x => x.MATAIKHOAN == userId);
            if (taiKhoan == null || taiKhoan.MAKHACHHANG == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin khách hàng!";
                return RedirectToAction("Login", "Account");
            }

            string maKH = taiKhoan.MAKHACHHANG;

            // ←←←← ĐOẠN QUAN TRỌNG NHẤT – LOẠI BỎ DẤU CÁCH THỪA ←←←←
            string maSP = (MASANPHAM ?? "").Trim();

            if (string.IsNullOrEmpty(maSP))
            {
                TempData["Error"] = "Không tìm thấy sản phẩm!";
                return RedirectToAction("Index", "Home");
            }

            // Thêm đánh giá
            try
            {
                var dg = new DANHGIASANPHAM
                {
                    MADANHGIA = "DG" + DateTime.Now.ToString("yyyyMMddHHmmssfff"),
                    MASANPHAM = maSP,
                    MAKHACHHANG = maKH,
                    DIEM = DIEM,
                    BINHLUAN = string.IsNullOrWhiteSpace(NOIDUNG) ? "Không có bình luận" : NOIDUNG.Trim(),
                    NGAYDANHGIA = DateTime.Now
                };
                db.DANHGIASANPHAM.Add(dg);
                db.SaveChanges();

                TempData["Success"] = "Cảm ơn bạn! Đánh giá đã được gửi thành công!";
            }
            catch
            {
                TempData["Error"] = "Có lỗi khi gửi đánh giá. Vui lòng thử lại!";
            }

            // ←←←← DÒNG QUAN TRỌNG NHẤT – DÙNG maSP ĐÃ TRIM → KHÔNG BAO GIỜ BỊ %20 NỮA!
            return RedirectToAction("ChiTiet", "Products", new { id = maSP });
        }

    }
}