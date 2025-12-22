using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebBanGIay.Models;
using System.Data.Entity;


namespace WebBanGIay.Controllers
{
    public class TrangChuController : Controller
    {
        // GET: TrangChu
        QuanLyBanGiayEntities1 db = new QuanLyBanGiayEntities1();
        public ActionResult Index(string hang = "", string giaTu = "", string giaDen = "", string TenSP = "", string MucGia = "", string SapXep = "")
        {
            var query = db.SANPHAM.Include(s => s.KHUYENMAI).AsQueryable();

            // 1. TÌM KIẾM THEO TÊN (TenSP)
            if (!string.IsNullOrEmpty(TenSP))
            {
                string k = TenSP.Trim();
                query = query.Where(s => s.TENSANPHAM.Contains(k));
                ViewBag.TenSP = TenSP; // Giu lai gia tri input
            }

            // 2. LỌC THEO HÃNG (hang)
            if (!string.IsNullOrEmpty(hang))
            {
                query = query.Where(s => s.NHACUNGCAP.TENNHACUNGCAP.Contains(hang));
                ViewBag.HangSelected = hang;
            }

            // 3. LỌC THEO GIÁ (MucGia & giaTu/giaDen)
            decimal minPrice = 0;
            decimal maxPrice = decimal.MaxValue;

            // Uu tien Dropdown MucGia
            if (!string.IsNullOrEmpty(MucGia))
            {
                switch (MucGia)
                {
                    case "duoi-500":
                        maxPrice = 500000;
                        break;
                    case "500-1tr":
                        minPrice = 500000;
                        maxPrice = 1000000;
                        break;
                    case "tren-1tr":
                        minPrice = 1000000;
                        break;
                }
            }
            // Neu khong co MucGia thi check input tay giaTu/giaDen (ho tro legacy)
            else
            {
                if (decimal.TryParse(giaTu, out decimal tu)) minPrice = tu;
                if (decimal.TryParse(giaDen, out decimal den)) maxPrice = den;
            }

            if (maxPrice >= 999999999) maxPrice = decimal.MaxValue;

            // Logic loc gia (tinh ca gia khuyen mai)
            query = query.Where(s =>
                ((s.GIAKHUYENMAI != null && s.GIAKHUYENMAI > 0) ? s.GIAKHUYENMAI : s.GIA) >= minPrice &&
                ((s.GIAKHUYENMAI != null && s.GIAKHUYENMAI > 0) ? s.GIAKHUYENMAI : s.GIA) <= maxPrice
            );

            // 4. SẮP XẾP (SapXep) - Ưu tiên Mới nhất / Cập nhật mới nhất
            switch (SapXep)
            {
                case "gia-tang":
                    query = query.OrderBy(s => (s.GIAKHUYENMAI != null && s.GIAKHUYENMAI > 0) ? s.GIAKHUYENMAI : s.GIA);
                    break;
                case "gia-giam":
                    query = query.OrderByDescending(s => (s.GIAKHUYENMAI != null && s.GIAKHUYENMAI > 0) ? s.GIAKHUYENMAI : s.GIA);
                    break;
                case "moi-nhat":
                    query = query.OrderByDescending(s => s.NGAYTAO).ThenByDescending(s => s.MASANPHAM);
                    break;
                default:
                    // Mặc định luôn ưu tiên sản phẩm mới thêm hoặc mới cập nhật
                    query = query.OrderByDescending(s => s.NGAYTAO).ThenByDescending(s => s.MASANPHAM);
                    break;
            }

            // Lấy kết quả (tăng lên 40 sản phẩm để đảm bảo không bị sót sản phẩm mới)
            var danhSachSanPham = query.Take(40).ToList();

            ViewBag.SanPhamBanChay = danhSachSanPham;
            ViewBag.HangList = db.NHACUNGCAP.Select(n => n.TENNHACUNGCAP).Distinct().ToList();

            // ViewBag giu state filter
            ViewBag.HangSelected = hang;
            ViewBag.GiaTu = giaTu;  
            ViewBag.GiaDen = giaDen;
            ViewBag.MucGia = MucGia;
            ViewBag.SapXep = SapXep;

            // FEATURED PROMOTION (for Banner)
            var featuredPromo = db.KHUYENMAI
                .Where(p => p.TRANGTHAI == true && p.NGAYBATDAU <= DateTime.Now && p.NGAYKETTHUC >= DateTime.Now)
                .OrderByDescending(p => p.PHANTRAMGIAM)
                .FirstOrDefault();
            ViewBag.FeaturedPromotion = featuredPromo;  

            return View();
        }
        public ActionResult MenuThuongHieu()
        {
            var danhSachNCC = db.NHACUNGCAP.OrderBy(n => n.TENNHACUNGCAP).ToList();

            return PartialView(danhSachNCC);
        }
        public ActionResult HuongDan()
        {

            return View();
        }
        public ActionResult LienHe()
        {

            return View();
        }
        public ActionResult TuyenDung()
        {

            return View();
        }
        public ActionResult KhuyenMai()
        {

            return View();
        }
        public ActionResult TimKiem(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return RedirectToAction("Index");
            }

            keyword = keyword.Trim();

            var tatCaSanPham = db.SANPHAM.AsEnumerable();

            var dsSanPham = tatCaSanPham
                .Where(s => RemoveDiacritics(s.TENSANPHAM).IndexOf(RemoveDiacritics(keyword), StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderByDescending(s => s.NGAYTAO)
                .ToList();

            ViewBag.Keyword = keyword;
            return View(dsSanPham);
        }

        // Hàm loại bỏ dấu (giữ nguyên)
        private string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
            var stringBuilder = new System.Text.StringBuilder();

            foreach (char c in normalizedString)
            {
                var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }

    }
}