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
        public ActionResult Index(string hang = "", string giaTu = "", string giaDen = "")
        {

            var query = db.SANPHAM.Include(s => s.KHUYENMAI).AsQueryable();


            // LỌC THEO HÃNG
            if (!string.IsNullOrEmpty(hang))
            {
                query = query.Where(s => s.NHACUNGCAP.TENNHACUNGCAP.Contains(hang));
            }

            decimal minPrice = 0;
            decimal maxPrice = decimal.MaxValue;

            if (decimal.TryParse(giaTu, out decimal tu))
                minPrice = tu;

            if (decimal.TryParse(giaDen, out decimal den))
                maxPrice = den;

            if (maxPrice >= 999999999)
                maxPrice = decimal.MaxValue;

            query = query.Where(s =>
                (s.GIAKHUYENMAI ?? 0) > 0 ?
                (s.GIAKHUYENMAI ?? 0) >= minPrice && (s.GIAKHUYENMAI ?? 0) <= maxPrice :
                s.GIA >= minPrice && s.GIA <= maxPrice
            );

            var sanPhamBanChay = query.OrderByDescending(s => s.SOLUONGTON).Take(30).ToList();

            ViewBag.SanPhamBanChay = sanPhamBanChay;
            ViewBag.HangList = db.NHACUNGCAP.Select(n => n.TENNHACUNGCAP).Distinct().ToList();

            ViewBag.HangSelected = hang;
            ViewBag.GiaTu = giaTu;  
            ViewBag.HangSelected = hang;
            ViewBag.GiaTu = giaTu;  
            ViewBag.GiaDen = giaDen;

            // FEATURED PROMOTION (for Banner)
            // Logic: Highest discount active promotion
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