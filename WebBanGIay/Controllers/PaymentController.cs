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

            // Ensure customer info is filled
            if (Session["CheckoutInfo"] == null)
            {
                return RedirectToAction("Checkout", "Cart");
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
            
            // Get Info from Checkout Session (the "Source of Truth")
            var checkoutInfo = Session["CheckoutInfo"] as CheckoutVM;
            string recipientName = checkoutInfo?.HoTen ?? "KHACH HANG";
            string recipientPhone = checkoutInfo?.DienThoai ?? "";

            // Generate VietQR Link with professional content
            // Format: MEODIGIAY [HOTEN] [PHONE] [TIME]
            string content = $"MEODIGIAY {recipientName.ToUpper()} {recipientPhone} {DateTime.Now:HHmm}".Trim();
            // URL encode the content for the QR URL
            string encodedContent = Uri.EscapeDataString(content);
            
            ViewBag.QRUrl = $"https://img.vietqr.io/image/MB-0909000111-compact.png?amount={total}&addInfo={encodedContent}";
            ViewBag.BankInfo = "Ngân hàng MB Bank - STK: 0909000111 - Chủ TK: NGUYEN THI YEN NHI";
            ViewBag.QRContent = content;

            // Pre-fill user info (Prioritize checkout info)
            if (checkoutInfo != null)
            {
                ViewBag.UserEmail = checkoutInfo.Email;
                ViewBag.UserAddress = checkoutInfo.DiaChi;
            }
            else if (Session["UserID"] != null)
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
            // Get Checkout Info for fallback
            var checkoutInfo = Session["CheckoutInfo"] as CheckoutVM;

            // Sends OTP to user email (provided or default)
            string otp = new Random().Next(100000, 999999).ToString();
            Session["OTP_Code"] = otp;
            Session["PendingPaymentMethod"] = "BankingQR"; 
            Session["PendingOTP"] = true; 
            
            // Update address in session if changed
            if (!string.IsNullOrEmpty(address) && checkoutInfo != null)
            {
                checkoutInfo.DiaChi = address;
                Session["CheckoutInfo"] = checkoutInfo;
            }

            // Send Email
            try 
            {
                 // If email not provided in form, fallback to session, then DB
                 if (string.IsNullOrEmpty(email))
                 {
                     email = checkoutInfo?.Email;
                     
                     if (string.IsNullOrEmpty(email))
                     {
                         string userId = Session["UserID"] as string;
                         var user = db.TAIKHOAN.FirstOrDefault(u => u.MATAIKHOAN == userId);
                         email = user?.KHACHHANG?.EMAIL;
                     }
                 }

                 if (!string.IsNullOrWhiteSpace(email))
                 {
                     Session["OTPTargetEmail"] = email;
                     string subject = "Xác thực thanh toán đơn hàng - MEODIGIAY";
                     string body = $@"
                        <div style='font-family: Arial, sans-serif; padding: 20px; color: #333;'>
                            <h2 style='color: #0d6efd;'>Chào bạn,</h2>
                            <p>Bạn vừa thực hiện xác nhận chuyển khoản cho đơn hàng tại <b>MEODIGIAY</b>.</p>
                            <p>Mã xác thực (OTP) của bạn là:</p>
                            <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; text-align: center; margin: 20px 0;'>
                                <span style='font-size: 32px; font-weight: bold; color: #dc3545; letter-spacing: 5px;'>{otp}</span>
                            </div>
                            <p style='color: #6c757d; font-size: 13px;'>* Lưu ý: Mã có hiệu lực trong 5 phút. Vui lòng không cung cấp mã này cho bất kỳ ai.</p>
                            <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>
                            <p style='font-size: 12px; color: #999;'>Đây là email tự động, vui lòng không trả lời email này.</p>
                        </div>";
                     
                     MailHelper.SendMail(email, subject, body);
                     Session["OTPSentSuccess"] = true;
                     Session.Remove("OTPError");
                 }
                 else {
                     Session["OTPError"] = "Không tìm thấy địa chỉ Email để gửi mã!";
                 }
            } 
            catch (Exception ex)
            { 
                if (ex.Message.Contains("SMTP_NOT_CONFIGURED"))
                {
                    Session["IsSimulationMode"] = true;
                    Session.Remove("OTPError");
                    Session["OTPSentSuccess"] = true;
                }
                else
                {
                    Session["OTPError"] = "Lỗi hệ thống: " + ex.Message;
                    Session["OTPSentSuccess"] = false;
                    Session["IsSimulationMode"] = false;
                }
            }

            return RedirectToAction("Gateway_OTP");
        }

        [HttpPost]
        public ActionResult ResendOTP()
        {
            if (Session["PendingOTP"] == null) return Json(new { success = false, message = "Phiên làm việc hết hạn." });

            var checkoutInfo = Session["CheckoutInfo"] as CheckoutVM;
            string email = Session["OTPTargetEmail"] as string;

            if (string.IsNullOrEmpty(email))
            {
                 email = checkoutInfo?.Email;
            }

            if (string.IsNullOrEmpty(email)) return Json(new { success = false, message = "Không tìm thấy email." });

            string otp = new Random().Next(100000, 999999).ToString();
            Session["OTP_Code"] = otp;

            try 
            {
                string subject = "Gửi lại mã xác thực - MEODIGIAY";
                string body = $"<h3>Mã OTP mới của bạn là: <b style='color:red; font-size: 24px;'>{otp}</b></h3>";
                MailHelper.SendMail(email, subject, body);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("SMTP_NOT_CONFIGURED"))
                {
                    return Json(new { success = true, isSimulation = true });
                }
                return Json(new { success = false, message = ex.Message });
            }
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

            if (Session["OTPError"] != null)
            {
                ViewBag.Error = Session["OTPError"];
            }

            ViewBag.TargetEmail = Session["OTPTargetEmail"];
            ViewBag.IsSimulationMode = Session["IsSimulationMode"] ?? false;

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

                    string maKH = tk.MAKHACHHANG?.Trim();

                    // Generate Order ID
                    string orderId = "HD" + DateTime.Now.ToString("yyyyMMddHHmmss");

                    // Get Checkout Info from Session
                    var checkoutInfo = Session["CheckoutInfo"] as CheckoutVM;
                    if (checkoutInfo == null)
                    {
                        return RedirectToAction("Checkout", "Cart");
                    }

                    // Create Order (HOADON)
                    var order = new HOADON
                    {
                        MAHOADON = orderId,
                        MAKHACHHANG = maKH,
                        MANHANVIEN = null, // Online order
                        NGAYLAP = DateTime.Now,
                        TONGTIEN = total,
                        TRANGTHAI = "Chờ xử lý", // String status
                        DIACHIGIAO = checkoutInfo != null ? checkoutInfo.DiaChi : (tk.KHACHHANG != null ? tk.KHACHHANG.DIACHI : "Địa chỉ mặc định"),
                        DIENTHOAINGIAO = checkoutInfo != null ? checkoutInfo.DienThoai : (tk.KHACHHANG != null ? tk.KHACHHANG.SODIENTHOAI : "0900000000"),
                        TENNGUOINHAN = checkoutInfo != null ? checkoutInfo.HoTen : (tk.KHACHHANG != null ? tk.KHACHHANG.HOTEN : "Khách hàng"),
                        GHICHU = (checkoutInfo != null && !string.IsNullOrEmpty(checkoutInfo.GhiChu) ? "Ghi chú: " + checkoutInfo.GhiChu + " | " : "") + "Thanh toán qua: " + (method ?? "COD")
                    };
                    
                    db.HOADON.Add(order);
                    db.SaveChanges();

                     // Add Details (CHITIET_HOADON) & Update Stock
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

                        // --- STOCK DEDUCTION LOGIC ---
                        // --- STOCK DEDUCTION LOGIC ---
                        if (item.BienTheId > 0 && !string.IsNullOrEmpty(item.Size))
                        {
                            // 1. Use BienTheId directly
                            int sizeInt = 0;
                            int.TryParse(item.Size, out sizeInt);

                            var stock = db.TONKHO_SIZE
                                .FirstOrDefault(t => t.IDBienThe == item.BienTheId && t.SIZE == sizeInt);

                            if (stock != null)
                            {
                                if (stock.SOLUONG >= item.SoLuong)
                                {
                                    stock.SOLUONG -= item.SoLuong;
                                }
                                else
                                {
                                    throw new Exception($"Sản phẩm {item.TenSP} không đủ số lượng tồn kho!");
                                }
                            }
                        }
                        // Fallback for old items without BienTheId
                        else if (!string.IsNullOrEmpty(item.MaSP) && !string.IsNullOrEmpty(item.Mau) && !string.IsNullOrEmpty(item.Size))
                        {
                            // 1. Find Variant by color matching
                            var variant = db.BIEN_THE_SAN_PHAM
                                .FirstOrDefault(v => v.MASANPHAM == item.MaSP && v.MAUSAC == item.Mau);

                            if (variant != null)
                            {
                                int sizeInt = 0;
                                int.TryParse(item.Size, out sizeInt);

                                // 2. Find Stock Record
                                var stock = db.TONKHO_SIZE
                                    .FirstOrDefault(t => t.IDBienThe == variant.ID && t.SIZE == sizeInt);

                                if (stock != null)
                                {
                                    if (stock.SOLUONG >= item.SoLuong)
                                        stock.SOLUONG -= item.SoLuong;
                                    else
                                        throw new Exception($"Sản phẩm {item.TenSP} không đủ số lượng tồn kho!");
                                }
                            }
                        }

                        // 3. Update Total Product Stock (Optional but good for sync)
                        var product = db.SANPHAM.FirstOrDefault(p => p.MASANPHAM == item.MaSP);
                        if (product != null)
                        {
                            product.SOLUONGTON = (product.SOLUONGTON ?? 0) - item.SoLuong;
                            if (product.SOLUONGTON < 0) product.SOLUONGTON = 0;
                        }
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