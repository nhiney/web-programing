using System.Linq;
using System.Web.Mvc;
using WebBanGIay.Models;

namespace WebBanGIay.Controllers
{
    public class BaseAdminController : Controller
    {
        // Protected db context so derived classes can use it if they want, 
        // though usually they satisfy their own db requirements. 
        // Better to just instantiate a separate context for notifications to avoid conflict or just use a using block.
        
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            // Fetch notifications
            using (var db = new QuanLyBanGiayEntities1())
            {
                // 1. New Orders (CHỜ XỬ LÝ)
                int newOrders = db.HOADON.Count(o => o.TRANGTHAI == "CHỜ XỬ LÝ");
                
                // 2. New Comments (Not Replied)
                // Assuming "New" means PHANHOI is null or empty
                int newComments = db.DANHGIASANPHAM.Count(c => c.PHANHOI == null || c.PHANHOI == "");

                
                // 3. New Messages (TrangThai = false or null)
                int newMessages = 0;
                try {
                    newMessages = db.LIENHE.Count(m => m.TrangThai == false || m.TrangThai == null);
                } catch { }

                ViewBag.NewOrdersCount = newOrders;
                ViewBag.NewCommentsCount = newComments;
                ViewBag.NewMessagesCount = newMessages;
                ViewBag.TotalNotifications = newOrders + newComments + newMessages;
            }
        }
    }
}
