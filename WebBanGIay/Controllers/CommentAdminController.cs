using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebBanGIay.Models;

namespace WebBanGIay.Controllers
{
    public class CommentAdminController : BaseAdminController
    {
        private QuanLyBanGiayEntities1 db = new QuanLyBanGiayEntities1();

        // GET: CommentAdmin
        public ActionResult Index(string status = "all")
        {
            if (Session["UserRole"] == null || (Session["UserRole"].ToString() != "QUẢN TRỊ" && Session["UserRole"].ToString() != "NHÂN VIÊN"))
            {
                return RedirectToAction("Login", "Account");
            }

            var query = db.DANHGIASANPHAM.Include(d => d.KHACHHANG).Include(d => d.SANPHAM).OrderByDescending(d => d.NGAYDANHGIA);

            if (status == "hidden")
            {
                // Chỉ lấy bình luận đã ẩn (TRANGTHAI = 0)
                return View(query.Where(d => d.TRANGTHAI == 0).ToList());
            }
            else if (status == "visible")
            {
                 // Lấy bình luận hiện (TRANGTHAI != 0 hoặc null)
                return View(query.Where(d => d.TRANGTHAI != 0 || d.TRANGTHAI == null).ToList());
            }

            return View(query.ToList());
        }

        [HttpPost]
        public ActionResult Reply(string id, string replyContent)
        {
            if (Session["UserRole"] == null || (Session["UserRole"].ToString() != "QUẢN TRỊ" && Session["UserRole"].ToString() != "NHÂN VIÊN"))
                return Json(new { success = false, message = "Unauthorized" });

            var comment = db.DANHGIASANPHAM.Find(id);
            if (comment != null)
            {
                comment.PHANHOI = replyContent;
                comment.NGAYPHANHOI = DateTime.Now;
                db.SaveChanges();
                return Json(new { success = true, message = "Đã gửi phản hồi!" });
            }
            return Json(new { success = false, message = "Không tìm thấy bình luận" });
        }

        [HttpPost]
        public ActionResult ToggleStatus(string id)
        {
            if (Session["UserRole"] == null || (Session["UserRole"].ToString() != "QUẢN TRỊ" && Session["UserRole"].ToString() != "NHÂN VIÊN"))
                 return Json(new { success = false, message = "Unauthorized" });

            var comment = db.DANHGIASANPHAM.Find(id);
            if (comment != null)
            {
                // Toggle status: If 0 -> 1, else -> 0
                // Default handling: null is treated as 1 (visible)
                if (comment.TRANGTHAI == 0)
                {
                    comment.TRANGTHAI = 1; // Show
                }
                else
                {
                    comment.TRANGTHAI = 0; // Hide
                }
                db.SaveChanges();
                return Json(new { success = true, isHidden = comment.TRANGTHAI == 0 });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public ActionResult Delete(string id)
        {
            if (Session["UserRole"] == null || (Session["UserRole"].ToString() != "QUẢN TRỊ" && Session["UserRole"].ToString() != "NHÂN VIÊN"))
                return Json(new { success = false, message = "Unauthorized" });

            var comment = db.DANHGIASANPHAM.Find(id);
            if (comment != null)
            {
                db.DANHGIASANPHAM.Remove(comment);
                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Not found" });
        }
    }
}
