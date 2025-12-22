using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using WebBanGIay.Models;

namespace WebBanGIay.Controllers
{
    public class OrderAdminController : BaseAdminController
    {
        private readonly QuanLyBanGiayEntities1 db = new QuanLyBanGiayEntities1();



        // ======================= LIST ORDERS =======================
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
        public ActionResult Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return HttpNotFound();

            // Self-correct URL if it has spaces
            if (id.Contains(" "))
            {
                return Redirect("/OrderAdmin/Details/" + id.Trim());
            }

            var idTrimmed = id.Trim();
            var order = db.HOADON.Include(h => h.KHACHHANG)
                                 .Include(h => h.CHITIET_HOADON.Select(d => d.SANPHAM))
                                 .AsEnumerable()
                                 .FirstOrDefault(h => h.MAHOADON != null && h.MAHOADON.Trim() == idTrimmed);

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
        public ActionResult Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return HttpNotFound();

            // Self-correct URL if it has spaces
            if (id.Contains(" "))
            {
                return Redirect("/OrderAdmin/Edit/" + id.Trim());
            }

            var idTrimmed = id.Trim();
            var order = db.HOADON.Include(h => h.KHACHHANG)
                                 .AsEnumerable()
                                 .FirstOrDefault(h => h.MAHOADON != null && h.MAHOADON.Trim() == idTrimmed);

            if (order == null) 
            {
                TempData["Error"] = "Không tìm thấy đơn hàng #" + idTrimmed;
                return RedirectToAction("Index");
            }

            return View(order);
        }

        // ======================= UPDATE STATUS =======================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStatus(string id, string status)
        {
            var idTrimmed = id?.Trim();
            // Find logic for CHAR PK
            var order = db.HOADON.AsEnumerable().FirstOrDefault(h => h.MAHOADON != null && h.MAHOADON.Trim() == idTrimmed);
            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng #" + id;
                return RedirectToAction("Index");
            }

            order.TRANGTHAI = status;
            
            db.SaveChanges();
            var idForRedirect = order.MAHOADON.Trim();
            TempData["Success"] = $"Cập nhật đơn hàng {idForRedirect} sang trạng thái {status} thành công!";
            
            return Redirect("/OrderAdmin/Details/" + idForRedirect);
        }



        // ======================= APPROVE ORDER (QUICK ACTION) =======================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Approve(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return new HttpStatusCodeResult(400);

            var idTrimmed = id.Trim();
            var order = db.HOADON.AsEnumerable().FirstOrDefault(h => h.MAHOADON != null && h.MAHOADON.Trim() == idTrimmed);
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

            return Redirect("/OrderAdmin/Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
