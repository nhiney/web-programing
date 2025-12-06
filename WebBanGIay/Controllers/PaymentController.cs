using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using PayOS;
using WebBanGIay.Models;

namespace WebBanGIay.Controllers
{
    public class PaymentController : Controller
    {
        private string clientId = "639b671b-873b-4967-b234-33c2c689191b";
        private string apiKey = "6f3f0c32-be70-4b6a-bdc9-e82a13e6da3f";
        private string checksumKey = "b3c65329b34e21cf5059f7ef4721c69f68a72ac6192a9a50da071a96d1d95a62";

        private string endpoint = "https://api-merchant.payos.vn/v2/payment-requests";

        private string returnUrl = "https://localhost:44300/Payment/Return";
        private string cancelUrl = "https://localhost:44300/Payment/Cancel";

        // ✅ TẠO LINK THANH TOÁN
        public async Task<ActionResult> Payment(long amount)
        {
            var paymentBody = new
            {
                orderCode = DateTime.Now.Ticks,
                amount = amount,
                description = "Thanh toán đơn hàng",
                // Chú ý: Nên dùng biến returnUrl/cancelUrl đã khai báo ở đầu class để dễ quản lý
                returnUrl = this.returnUrl,
                cancelUrl = this.cancelUrl
            };

            // 2. TÍNH TOÁN SIGNATURE
            string signature = CreateSignature(paymentBody, checksumKey);

            // 3. TẠO REQUEST OBJECT CHUNG
            // Dữ liệu PayOS cần là tất cả các trường order (amount, orderCode,...) 
            // VÀ signature, TẤT CẢ Ở CÙNG CẤP ĐỘ GỐC.

            // Chuyển paymentBody thành JObject
            var jsonBody = JObject.FromObject(paymentBody);

            // Thêm Signature vào JObject gốc
            jsonBody.Add("signature", signature); // <<< THÊM SIGNATURE NGANG HÀNG

            // Sử dụng JObject này cho StringContent
            string jsonContent = jsonBody.ToString(Formatting.None); // Bỏ format để gọn gàng hơn

            // 4. GỬI REQUEST
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("x-client-id", clientId);
                client.DefaultRequestHeaders.Add("x-api-key", apiKey);

                var content = new StringContent(
                    jsonContent, // SỬ DỤNG JSON ĐƯỢC CHỈNH SỬA Ở BƯỚC 3
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PostAsync(
                    "https://api-merchant.payos.vn/v2/payment-requests",
                    content);

                // ... (Phần xử lý response tiếp theo không đổi)
                var json = await response.Content.ReadAsStringAsync();
                var data = JObject.Parse(json);

                // Log debug (nên kiểm tra file log này sau khi chạy)
                System.IO.File.WriteAllText(
                    Server.MapPath("~/App_Data/payos_log.txt"),
                    "Gửi đi: " + jsonContent + "\n" + "Nhận về: " + json);


                if (data["code"].ToString() != "00")
                    throw new Exception("PayOS không tạo được link: " + json);

                // data["data"] sẽ là một JObject chứa link thanh toán, cần trích xuất giá trị URL 
                // Dựa trên tài liệu mới, nó là data["data"]["checkoutUrl"]

                // Cần kiểm tra cấu trúc của response, nếu data["data"] là object:
                if (data["data"] is JObject dataObject && dataObject.ContainsKey("checkoutUrl"))
                {
                    return Redirect(dataObject["checkoutUrl"].ToString());
                }

                // Nếu cấu trúc cũ (data["data"] là string URL)
                // return Redirect(data["data"].ToString()); 

                throw new Exception("Cấu trúc phản hồi PayOS không hợp lệ: " + json);
            }
        }

        // ---------- RETURN URL ----------
        public ActionResult Return()
        {
            var code = Request.QueryString["code"];
            var status = Request.QueryString["status"];
            var orderCode = Request.QueryString["orderCode"];

            if (code == "00" && status == "PAID")
            {
                ViewBag.Message = "Thanh toán thành công!";
                ViewBag.OrderCode = orderCode;
            }
            else
            {
                ViewBag.Message = "Thanh toán thất bại hoặc bị hủy";
            }

            return View();
        }

        public ActionResult Cancel()
        {
            ViewBag.Message = "Bạn đã hủy thanh toán";
            return View();
        }
        private string CreateSignature(object body, string checksumKey)
        {
            // 1. Phân tích Object thành JObject để dễ dàng thao tác với thuộc tính
            var json = JObject.Parse(JsonConvert.SerializeObject(body));

            // 2. Sắp xếp các thuộc tính theo thứ tự Alphabet và tạo cặp key=value
            var sorted = json.Properties()
                             // Bắt buộc sắp xếp theo tên thuộc tính
                             .OrderBy(p => p.Name)
                             .Select(p =>
                             {
                                 // Lấy giá trị chuỗi (cần đảm bảo không phải là null)
                                 string value = p.Value.ToString();

                                 // *** CHỈNH SỬA QUAN TRỌNG: URL Encode giá trị (value) ***
                                 // Điều này là cần thiết để xử lý các ký tự đặc biệt trong URL (như returnUrl) 
                                 // hoặc description để PayOS xác thực chuỗi đúng.
                                 string encodedValue = HttpUtility.UrlEncode(value);

                                 // Thay thế "+" thành "%20" nếu cần thiết (vì UrlEncode dùng "+")
                                 // Nếu PayOS không yêu cầu thay thế, bạn có thể bỏ qua dòng này.
                                 // encodedValue = encodedValue.Replace("+", "%20"); 

                                 return $"{p.Name}={encodedValue}";
                             });

            // 3. Nối các cặp key=value lại bằng ký tự "&" để tạo chuỗi RAW
            string raw = string.Join("&", sorted);

            // 4. Băm chuỗi RAW bằng HMACSHA256 với Checksum Key
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(checksumKey)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(raw));

                // 5. Chuyển kết quả băm thành chuỗi thập lục phân (hex) và chuyển sang chữ thường (lowercase)
                return BitConverter.ToString(hash)
                                   .Replace("-", "")
                                   .ToLower();
            }
        }

    }


}