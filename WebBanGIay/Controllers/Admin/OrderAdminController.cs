using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using WebBanGIay.Models;

namespace WebBanGIay.Controllers
{
    [RoutePrefix("OrderAdmin")]
    public class OrderAdminController : BaseAdminController
    {
        private readonly QuanLyBanGiayEntities1 db = new QuanLyBanGiayEntities1();



        // ======================= LIST ORDERS =======================
        [Route("")]
        [Route("Index")]
        public ActionResult Index(string search = "", string status = "", int page = 1, int pageSize = 15)
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
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(o => 
                    o.MAHOADON.Contains(search) || 
                    (o.KHACHHANG != null && o.KHACHHANG.HOTEN.Contains(search)) ||
                    o.TENNGUOINHAN.Contains(search)
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

            // Fallback for KHACHHANG padding issues
            foreach (var order in orders)
            {
                if (order.KHACHHANG == null && !string.IsNullOrEmpty(order.MAKHACHHANG))
                {
                    string mkh = order.MAKHACHHANG.Trim();
                    order.KHACHHANG = db.KHACHHANG.FirstOrDefault(k => k.MAKHACHHANG.Trim() == mkh);
                }
            }

            ViewBag.CurrentSearch = search;
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);

            return View(orders);
        }

        // ======================= DETAILS ORDER =======================
        [Route("Details/{id}")]
        public ActionResult Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return HttpNotFound();

            id = id.Replace(" ", "");
            var order = db.HOADON.Include(h => h.KHACHHANG)
                                 .Include(h => h.CHITIET_HOADON.Select(d => d.SANPHAM))
                                 .FirstOrDefault(h => h.MAHOADON == id);

            if (order == null) return HttpNotFound();

            // Fallback for KHACHHANG padding issues
            if (order.KHACHHANG == null && !string.IsNullOrEmpty(order.MAKHACHHANG))
            {
                string mkh = order.MAKHACHHANG.Trim();
                order.KHACHHANG = db.KHACHHANG.FirstOrDefault(k => k.MAKHACHHANG.Trim() == mkh);
            }

            return View(order);
        }

        // ======================= EDIT ORDER (STATUS) =======================
        [Route("Edit/{id}")]
        public ActionResult Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return HttpNotFound();

            id = id.Replace(" ", "");
            var order = db.HOADON.Include(h => h.KHACHHANG)
                                 .FirstOrDefault(h => h.MAHOADON == id);

            if (order == null) 
            {
                TempData["Error"] = "Không tìm thấy đơn hàng #" + id;
                return RedirectToAction("Index");
            }

            return View(order);
        }

        // ======================= UPDATE STATUS =======================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("UpdateStatus")]
        public ActionResult UpdateStatus(string id, string status)
        {
            id = id?.Replace(" ", "");
            var order = db.HOADON.Find(id);
            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng #" + id;
                return RedirectToAction("Index");
            }

            order.TRANGTHAI = status;
            
            db.SaveChanges();
            TempData["Success"] = $"Cập nhật đơn hàng {id} sang trạng thái {status} thành công!";
            
            return RedirectToAction("Details", new { id = id });
        }



        // ======================= APPROVE ORDER (QUICK ACTION) =======================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Approve")]
        public ActionResult Approve(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return new HttpStatusCodeResult(400);

            id = id.Replace(" ", "");
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
