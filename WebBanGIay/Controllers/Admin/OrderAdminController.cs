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

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            ViewBag.NewOrdersCount = db.HOADON.Count(o => o.TRANGTHAI == "CHỜ XỬ LÝ");
        }

        // ======================= LIST ORDERS =======================
        public ActionResult Index(string q = "", string status = "", int page = 1, int pageSize = 15)
        {
            var query = db.HOADON.Include(h => h.KHACHHANG).AsQueryable();

            ViewBag.StatusList = new SelectList(new[]
            {
                new { Value = "CHỜ XỬ LÝ", Text = "CHỜ XỬ LÝ" },
                new { Value = "ĐANG GIAO", Text = "ĐANG GIAO" },
                new { Value = "ĐÃ GIAO", Text = "ĐÃ GIAO" },
                new { Value = "ĐÃ HỦY", Text = "ĐÃ HỦY" }
            }, "Value", "Text", status);

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

            id = id.Trim();
            var order = db.HOADON.Include(h => h.KHACHHANG)
                                 .Include(h => h.CHITIET_HOADON.Select(d => d.SANPHAM))
                                 .FirstOrDefault(h => h.MAHOADON == id);

            if (order == null) return HttpNotFound();

            return View(order);
        }

        // ======================= EDIT ORDER (STATUS) =======================
        public ActionResult Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return HttpNotFound();

            id = id.Trim();
            var order = db.HOADON.Include(h => h.KHACHHANG)
                                 .FirstOrDefault(h => h.MAHOADON == id);

            if (order == null) return HttpNotFound();

            return View(order);
        }

        // ======================= UPDATE STATUS =======================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStatus(string id, string status)
        {
            id = id?.Trim();
            var order = db.HOADON.Find(id);
            if (order == null) return HttpNotFound();

            order.TRANGTHAI = status;
            
            db.SaveChanges();
            TempData["Success"] = $"Cập nhật đơn hàng {id} sang trạng thái {status} thành công!";
            
            return RedirectToAction("Details", new { id = id });
        }



        // ======================= APPROVE ORDER (QUICK ACTION) =======================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Approve(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return new HttpStatusCodeResult(400);

            id = id.Trim();
            var order = db.HOADON.Find(id);
            if (order == null) return HttpNotFound();

            if (order.TRANGTHAI == "CHỜ XỬ LÝ")
            {
                order.TRANGTHAI = "ĐANG GIAO"; // Duyệt đơn
                db.SaveChanges();
                TempData["Success"] = $"Đã duyệt đơn hàng #{id} thành công!";
            }
            else
            {
                TempData["Error"] = $"Đơn hàng #{id} đang ở trạng thái {order.TRANGTHAI}, không thể duyệt lại!";
            }

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
