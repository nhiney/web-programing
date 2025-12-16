using System;
using System.Linq;
using System.Web.Mvc;
using WebBanGIay.Models;

namespace WebBanGIay.Controllers.Admin
{
    public class MessageAdminController : BaseAdminController
    {
        private readonly QuanLyBanGiayEntities1 db = new QuanLyBanGiayEntities1();

        // GET: Admin/MessageAdmin
        public ActionResult Index(int page = 1, int pageSize = 10)
        {
            var query = db.LIENHE.OrderByDescending(x => x.NgayGui);
            
            int total = query.Count();
            var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);

            return View(items);
        }

        public ActionResult Details(int id)
        {
            var msg = db.LIENHE.Find(id);
            if (msg == null) return HttpNotFound();

            // Mark as read
            if (msg.TrangThai == false)
            {
                msg.TrangThai = true;
                db.SaveChanges();
            }

            return View(msg);
        }

        [HttpPost]
        public ActionResult Delete(int id)
        {
            var msg = db.LIENHE.Find(id);
            if (msg != null)
            {
                db.LIENHE.Remove(msg);
                db.SaveChanges();
                TempData["Success"] = "Đã xóa tin nhắn.";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Reply(int id, string phanHoi)
        {
            var msg = db.LIENHE.Find(id);
            if (msg != null)
            {
                msg.PhanHoi = phanHoi;
                msg.NgayPhanHoi = DateTime.Now;
                msg.TrangThai = true; // Mark as read/handled
                db.SaveChanges();
                TempData["Success"] = "Đã gửi phản hồi thành công!";
            }
            return RedirectToAction("Index");
        }
    }
}
