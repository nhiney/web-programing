using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebBanGIay.Models;

namespace WebBanGIay.Controllers
{
    public class TrangChuController : Controller
    {
        // GET: TrangChu
        QuanLyBanGiayEntities1 db = new QuanLyBanGiayEntities1();
        public ActionResult Index(string hang = "", string giaTu = "", string giaDen = "")
        {
            var query = db.SANPHAM.AsQueryable();

            // LỌC THEO HÃNG
            if (!string.IsNullOrEmpty(hang))
            {
                query = query.Where(s => s.NHACUNGCAP.TENNHACUNGCAP.Contains(hang));
            }

            // CHUYỂN ĐỔI GIÁ TỪ STRING → DECIMAL
            decimal minPrice = 0;
            decimal maxPrice = decimal.MaxValue;

            if (decimal.TryParse(giaTu, out decimal tu))
                minPrice = tu;

            if (decimal.TryParse(giaDen, out decimal den))
                maxPrice = den;

            // Nếu chọn trên 4 triệu
            if (maxPrice >= 999999999)
                maxPrice = decimal.MaxValue;

            // LỌC THEO GIÁ (ưu tiên GIAKHUYENMAI nếu có)
            query = query.Where(s =>
                (s.GIAKHUYENMAI ?? 0) > 0 ?
                (s.GIAKHUYENMAI ?? 0) >= minPrice && (s.GIAKHUYENMAI ?? 0) <= maxPrice :
                s.GIA >= minPrice && s.GIA <= maxPrice
            );

            var sanPhamBanChay = query.OrderByDescending(s => s.SOLUONGTON).Take(30).ToList();

            // TRUYỀN DỮ LIỆU
            ViewBag.SanPhamBanChay = sanPhamBanChay;
            ViewBag.HangList = db.NHACUNGCAP.Select(n => n.TENNHACUNGCAP).Distinct().ToList();

            // GIỮ GIÁ TRỊ ĐÃ CHỌN
            ViewBag.HangSelected = hang;
            ViewBag.GiaTu = giaTu;  
            ViewBag.GiaDen = giaDen;  

            return View();
        }
        public ActionResult MenuThuongHieu()
        {
            var danhSachNCC = db.NHACUNGCAP.OrderBy(n => n.TENNHACUNGCAP).ToList();

            return PartialView(danhSachNCC);
        }

    }
}