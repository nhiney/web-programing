using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using WebBanGIay.Models;

namespace WebBanGIay.Controllers
{
    public class ProductsController : Controller
    {
        private readonly QuanLyBanGiayEntities1 db = new QuanLyBanGiayEntities1();

        public ActionResult ChiTiet(string id)
        {
            var sanPham = db.SANPHAM.Include(s => s.NHACUNGCAP).FirstOrDefault(s => s.MASANPHAM == id);
            var query = db.SANPHAM.AsQueryable();
            var sanPhamBanChay = query.OrderByDescending(s => s.SOLUONGTON).Take(5).ToList();

            decimal giaGoc = sanPham.GIAKHUYENMAI ?? 0; // Lấy giá, nếu null thì coi là 0
            decimal minPrice = giaGoc * 0.8m;  // Giá thấp nhất (-20%)
            decimal maxPrice = giaGoc * 1.2m;  // Giá cao nhất (+20%)
                                               // TRUYỀN DỮ LIỆU
            var listLienQuan = db.SANPHAM
                    .Where(s => s.MASANPHAM != id                       // Điều kiện 2: Trừ sản phẩm đang xem
                             && s.GIA >= minPrice && s.GIA <= maxPrice) // Điều kiện 3: Giá trong khoảng cho phép
                    .ToList();
            // 2b. Xử lý Random và lấy 4 sản phẩm ở phía C# (An toàn tuyệt đối)
            if (listLienQuan.Count == 0)
            {
                listLienQuan = db.SANPHAM
                    .Where(s => s.MANHACUNGCAP   == sanPham.MANHACUNGCAP && s.MASANPHAM != id)
                    .Take(10) // Lấy tạm 10 cái để random
                    .ToList();
            }
            ViewBag.SanPhamLienQuan = listLienQuan
                    .OrderBy(x => Guid.NewGuid())
                    .Take(4)
                    .ToList();
            ViewBag.HangList = db.NHACUNGCAP.Select(n => n.TENNHACUNGCAP).Distinct().ToList();

            if (string.IsNullOrEmpty(id))
            {
                return HttpNotFound("Không tìm thấy sản phẩm!");
            }

            // Lấy sản phẩm theo mã (bao gồm Nhà Cung Cấp)


            if (sanPham == null)
            {
                return HttpNotFound("Sản phẩm không tồn tại!");
            }

            // Lấy danh sách đánh giá (nếu có)
            var danhGiaList = db.DANHGIASANPHAM.Where(d => d.MASANPHAM == id).Include(d => d.KHACHHANG).ToList();

            // Tính điểm trung bình & tổng số đánh giá
            ViewBag.DanhGiaList = danhGiaList;
            ViewBag.AverageRating = danhGiaList.Any() ? danhGiaList.Average(d => d.DIEM) : 0;
            ViewBag.TotalReviews = danhGiaList.Count;

            return View(sanPham);
        }

        public ActionResult DanhSach()
        {
            var sanPhams = db.SANPHAM.Include(s => s.NHACUNGCAP).ToList();
            return View(sanPhams);
        }
        public async Task<ActionResult> SPhamTHuongHieu(string maNhaCungCap)
        {
            var query = db.SANPHAM.AsQueryable();

            if (!string.IsNullOrEmpty(maNhaCungCap))
            {
                string maChuan = maNhaCungCap.Trim();
                query = query.Where(p => p.MANHACUNGCAP == maChuan);

                // Lấy tên thương hiệu để hiển thị tiêu đề cho đẹp (Optional)
                var tenHang = db.NHACUNGCAP
                                      .Where(n => n.MANHACUNGCAP == maChuan)
                                      .Select(n => n.TENNHACUNGCAP)
                                      .FirstOrDefault();
                ViewBag.TenThuongHieu = tenHang;
            }

            var listSanPham = await query.ToListAsync();

            // Trả về đúng View
            return View("SPhamTHuongHieu", listSanPham);
        }
    }
}