using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using WebBanGIay.Models;

namespace WebBanGIay.Controllers
{
    public class OrderAdminController : Controller
    {
        private readonly QuanLyBanGiayEntities1 db = new QuanLyBanGiayEntities1();

        // ======================= LIST ORDERS =======================
        public ActionResult Index(string q = "", string status = "", int page = 1, int pageSize = 15)
        {
            var query = db.HOADON.Include(h => h.KHACHHANG).AsQueryable();

            // Search (Mã hóa đơn, tên KH)
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(o => 
                    o.MAHOADON.Contains(q) || 
                    (o.KHACHHANG != null && o.KHACHHANG.HOTEN.Contains(q))
                );
            }

            // Filter status
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.TRANGTHAI == status);
            }

            int total = query.Count();

            var orders = query.OrderByDescending(o => o.NGAYLAP)
                              .Skip((page - 1) * pageSize)
                              .Take(pageSize)
                              .ToList();

            ViewBag.Query = q;
            ViewBag.Status = status;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);

            return View(orders);
        }

        // ======================= DETAILS ORDER =======================
        public ActionResult Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return HttpNotFound();

            var order = db.HOADON.Include(h => h.KHACHHANG)
                                 .Include(h => h.CHITIET_HOADON.Select(d => d.SANPHAM))
                                 .FirstOrDefault(h => h.MAHOADON == id);

            if (order == null) return HttpNotFound();

            return View(order);
        }

        // ======================= UPDATE STATUS =======================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStatus(string id, string status)
        {
            var order = db.HOADON.Find(id);
            if (order == null) return HttpNotFound();

            order.TRANGTHAI = status;
            
            // Nếu hoàn thành, có thể update ngày giao hàng thực tế nếu cần
            if (status == "ĐÃ GIAO" || status == "HOÀN THÀNH")
            {
                // Logic thêm nếu cần
            }

            db.SaveChanges();
            TempData["Success"] = $"Cập nhật đơn hàng {id} sang trạng thái {status} thành công!";
            
            return RedirectToAction("Details", new { id = id });
        }
    }
}
