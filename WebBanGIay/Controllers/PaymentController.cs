using System;
using System.Linq;
using System.Web.Mvc;
using WebBanGIay.Models;
using WebBanGIay.Helpers;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using System.Configuration; // For Web.config
using System.Collections.Generic;

namespace WebBanGIay.Controllers
{
    public class PaymentController : Controller
    {
        private readonly QuanLyBanGiayEntities1 db = new QuanLyBanGiayEntities1();
        private readonly CartService cartService = new CartService();

        // GET: Payment/Index
        // Screen to select payment method
        public ActionResult Index()
        {
            var cart = cartService.GetCart();
            if (cart == null || cart.Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }
            
            ViewBag.TongTien = cartService.TongTien();
            return View();
        }

        // GET: Payment/Gateway_QR
        public ActionResult Gateway_QR()
        {
            var total = cartService.TongTien();
            ViewBag.TongTien = total;
            // Generate a fake transaction ID for the QR
            ViewBag.TxnId = "VNPAY" + DateTime.Now.Ticks.ToString().Substring(10);
            return View();
        }

        // GET: Payment/Gateway_BankingQR
        public ActionResult Gateway_BankingQR()
        {
            var total = cartService.TongTien();
            ViewBag.TongTien = total;
            
            // Generate VietQR Link
            string content = "PAY" + DateTime.Now.ToString("ddHHmm");
            ViewBag.QRUrl = $"https://img.vietqr.io/image/MB-0909000111-compact.png?amount={total}&addInfo={content}";

            // Pre-fill user info
            if (Session["UserID"] != null)
            {
                string userId = Session["UserID"] as string;
                var user = db.TAIKHOAN.FirstOrDefault(u => u.MATAIKHOAN == userId);
                if (user != null && user.KHACHHANG != null)
                {
                    ViewBag.UserEmail = user.KHACHHANG.EMAIL;
                    ViewBag.UserAddress = user.KHACHHANG.DIACHI;
                }
            }
            return View();
        }

        [HttpPost]
        public ActionResult ConfirmTransfer(string email, string address)
        {
            // Sends OTP to user email (provided or default)
            string otp = new Random().Next(100000, 999999).ToString();
            Session["OTP_Code"] = otp;
            Session["PendingPaymentMethod"] = "BankingQR"; 
            Session["PendingOTP"] = true; 
            
            // Store new address for order later if needed
            if (!string.IsNullOrEmpty(address))
            {
                Session["DeliveryAddress"] = address;
            }

            // Send Email
            try 
            {
                 // If email not provided in form, fallback to DB (redundancy)
                 if (string.IsNullOrEmpty(email))
                 {
                     string userId = Session["UserID"] as string;
                     var user = db.TAIKHOAN.FirstOrDefault(u => u.MATAIKHOAN == userId);
                     email = user?.KHACHHANG?.EMAIL ?? "test@example.com";
                 }

                 string subject = "Mã xác thực thanh toán - MEODIGIAY";
                 string body = $"<h3>Mã OTP của bạn là: <b style='color:red; font-size: 20px;'>{otp}</b></h3><p>Mã có hiệu lực trong 5 phút.</p>";
                 
                 MailHelper.SendMail(email, subject, body);
            } 
            catch { /* Log error */ }

            return RedirectToAction("Gateway_OTP");
        }

        // GET: Payment/Gateway_Card
        public ActionResult Gateway_Card()
        {
            var total = cartService.TongTien();
            ViewBag.TongTien = total;
            return View();
        }

        // GET: Payment/Gateway_OTP
        // Simulates OTP screen
        public ActionResult Gateway_OTP()
        {
            // Allow access if either Card or BankingQR is pending
            if (Session["PendingOTP"] == null && Session["PendingCardAuth"] == null)
                return RedirectToAction("Index");

            return View();
        }

        [HttpPost]
        public ActionResult ProcessCard(string cardNumber, string holder, string expiry, string cvv)
        {
            // Simple validation simulation
            if (string.IsNullOrEmpty(cardNumber) || string.IsNullOrEmpty(holder))
            {
                 ModelState.AddModelError("", "Vui lòng nhập đầy đủ thông tin thẻ");
                 var total = cartService.TongTien();
                 ViewBag.TongTien = total;
                 return View("Gateway_Card");
            }

            // Save state to session
            Session["PendingPaymentMethod"] = "VISA/MASTER";
            Session["PendingOTP"] = true;
            
            // Generate OTP for Card too (simulated SMS)
            Session["OTP_Code"] = "123456"; // Hardcoded for Card demo

            return RedirectToAction("Gateway_OTP");
        }

        [HttpPost]
        public ActionResult VerifyOTP(string otp1, string otp2, string otp3, string otp4, string otp5, string otp6)
        {
            string inputOtp = otp1 + otp2 + otp3 + otp4 + otp5 + otp6;
            string sessionOtp = Session["OTP_Code"] as string;

            // Verify
            if (inputOtp == sessionOtp || inputOtp == "123456") // Allow 123456 as master key for testing
            {
                Session.Remove("PendingOTP");
                Session.Remove("OTP_Code");
                Session.Remove("PendingCardAuth"); // Cleanup old flag
                
                string method = Session["PendingPaymentMethod"] as string ?? "Unknown";
                return RedirectToAction("CompleteOrder", new { method = method });
            }
            
            ViewBag.Error = "Mã OTP không chính xác!";
            return View("Gateway_OTP");
        }

        // Action called by all gateways upon success
        public ActionResult CompleteOrder(string method)
        {
             // Check login
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            string taiKhoanId = Session["UserID"] as string;
            var cart = cartService.GetCart();
            var total = cartService.TongTien();

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // Get Customer ID from Account
                    var tk = db.TAIKHOAN.FirstOrDefault(u => u.MATAIKHOAN == taiKhoanId);
                    if (tk == null) return RedirectToAction("Login", "Account");

                    string maKH = tk.MAKHACHHANG;

                    // Generate Order ID
                    string orderId = "HD" + DateTime.Now.ToString("yyyyMMddHHmmss");

                    // Create Order (HOADON)
                    var order = new HOADON
                    {
                        MAHOADON = orderId,
                        MAKHACHHANG = maKH,
                        MANHANVIEN = null, // Online order
                        NGAYLAP = DateTime.Now,
                        TONGTIEN = total,
                        TRANGTHAI = "Chờ xử lý", // String status
                        DIACHIGIAO = tk.KHACHHANG != null ? tk.KHACHHANG.DIACHI : "Địa chỉ mặc định",
                        DIENTHOAINGIAO = tk.KHACHHANG != null ? tk.KHACHHANG.SODIENTHOAI : "0900000000",
                        TENNGUOINHAN = tk.KHACHHANG != null ? tk.KHACHHANG.HOTEN : "Khách hàng",
                        GHICHU = "Thanh toán qua: " + (method ?? "COD")
                    };
                    
                    db.HOADON.Add(order);
                    db.SaveChanges();

                    // Add Details (CHITIET_HOADON)
                    foreach (var item in cart)
                    {
                        var detail = new CHITIET_HOADON
                        {
                            MAHOADON = order.MAHOADON,
                            MASANPHAM = item.MaSP,
                            SOLUONG = item.SoLuong,
                            DONGIA = item.DonGia
                        };
                         db.CHITIET_HOADON.Add(detail);
                    }
                    db.SaveChanges();
                    transaction.Commit();

                    // 2. Clear Cart
                    cartService.Clear();

                    // 3. Send Email (Async or Fire-and-forget)
                    try {
                        // MailHelper.SendMail(userEmail, subject, content); 
                    } catch {}

                    return RedirectToAction("Success", new { id = order.MAHOADON });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    // Log error
                    return Content("Lỗi thanh toán: " + ex.Message);
                }
            }
        }

        public ActionResult Success(string id)
        {
            ViewBag.OrderId = id; // Pass string ID
            return View(); // View accesses Model? Need to fix View too if it expects int
        }
    }
}