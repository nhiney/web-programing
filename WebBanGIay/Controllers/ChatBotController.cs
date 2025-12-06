using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Mvc;

namespace WebBanGIay.Controllers
{
    public class ChatBotController : Controller
    {
        private string connStr = "Server=DESKTOP-J6K452J;Database=QLGIAY;Trusted_Connection=True;";

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public JsonResult Ask(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return Json(new { reply = "Bạn chưa hỏi gì mà! Hỏi mình đi ạ" });

            string msg = " " + message.Trim().ToLower() + " ";
            string reply = "";

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    // 1. Chào hỏi
                    if (msg.Contains(" chào ") || msg.Contains(" hi ") || msg.Contains(" hello ") || msg.Contains(" hế lô "))
                        reply = "Chào bạn ơi! Shop giày đây ❤️\nBạn đang tìm giày nào hôm nay ạ? (Nike, Adidas, dưới 2 triệu, từ 1-3 triệu…)";

                    // 2. Hỏi giá sản phẩm cụ thể
                    else if (msg.Contains("giá") || msg.Contains("bao nhiêu") || msg.Contains("nhiêu") || msg.Contains("cost"))
                    {
                        reply = HoiGiaSanPham(conn, msg);
                    }

                    // 3. Tìm theo từ khóa (thương hiệu, tên giày)
                    else if (msg.Contains("nike") || msg.Contains("adidas") || msg.Contains("converse") ||
                             msg.Contains("crocs") || msg.Contains("fila") || msg.Contains("vans") || msg.Contains("plus") ||
                             msg.Contains("air force") || msg.Contains("jordan") || msg.Contains("dunk") || msg.Contains("ultraboo"))
                    {
                        reply = TimSanPhamTheoTuKhoa(conn, msg);
                    }

                    // 4. TÌM THEO KHOẢNG GIÁ
                    else if (Regex.IsMatch(msg, @"(dưới|từ|trên|khoảng|khoang|đến|tới|-)"))
                    {
                        reply = TimSanPhamTheoKhoangGia(conn, msg);
                    }

                    // 5. Gợi ý mặc định
                    else
                    {
                        reply = GoiYSanPhamHot(conn);
                    }
                }
            }
            catch (Exception)
            {
                reply = "Bot đang hơi mệt, bạn thử lại sau 1 phút nha!";
            }

            return Json(new { reply = reply.Trim() }, JsonRequestBehavior.AllowGet);
        }

        // HỎI GIÁ 
        private string HoiGiaSanPham(SqlConnection conn, string msg)
        {
            // loại bỏ từ thừa
            string clean = msg
                .Replace("giá", "")
                .Replace("bao nhiêu", "")
                .Replace("nhiêu", "")
                .Replace("là", "")
                .Replace("?", "")
                .Trim();

            if (clean.Length < 2)
                return "Bạn muốn hỏi giá đôi nào ạ? (Ví dụ: giá nike air force 1)";

            string sql = @"
                SELECT TOP 1 TENSANPHAM, GIA, GIAKHUYENMAI, HINHANH
                FROM SANPHAM
                WHERE TENSANPHAM LIKE @key
            ";

            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@key", "%" + clean + "%");

                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (!r.HasRows)
                        return $"Mình không tìm thấy sản phẩm '{clean}' rồi ạ, bạn thử ghi rõ hơn nha.";

                    r.Read();

                    string ten = r["TENSANPHAM"].ToString();
                    decimal gia = Convert.ToDecimal(r["GIA"]);
                    decimal? giaKM = r.IsDBNull(r.GetOrdinal("GIAKHUYENMAI")) ? (decimal?)null : Convert.ToDecimal(r["GIAKHUYENMAI"]);
                    string anh = r["HINHANH"]?.ToString();

                    string giaHienThi = giaKM.HasValue && giaKM < gia
                        ? $"<s>{gia:N0}₫</s> → <b>{giaKM:N0}₫</b> (giảm giá)"
                        : $"{gia:N0}₫";

                    var sb = new StringBuilder();
                    sb.AppendLine($"Giá của **{ten}** là:");
                    sb.AppendLine($"👉 {giaHienThi}");
                    if (!string.IsNullOrEmpty(anh))
                        sb.AppendLine($"Ảnh: /source/images/Products/{anh}");

                    sb.AppendLine("\nBạn muốn kiểm tra size không nè?");
                    return sb.ToString();
                }
            }
        }

        // TÌM THEO TỪ KHÓA
        private string TimSanPhamTheoTuKhoa(SqlConnection conn, string keyword)
        {
            string sql = @"
                SELECT TOP 6 TENSANPHAM, GIA, GIAKHUYENMAI, HINHANH, SOLUONGTON
                FROM SANPHAM
                WHERE (TENSANPHAM LIKE @key OR TENSANPHAM LIKE @key2)
                  AND SOLUONGTON > 0
                ORDER BY CASE WHEN GIAKHUYENMAI > 0 THEN GIAKHUYENMAI ELSE GIA END";

            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@key", $"%{keyword}%");
                cmd.Parameters.AddWithValue("@key2", $"%{keyword.Replace(" ", "")}%");

                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (!r.HasRows) return "Hic, hết hàng mẫu bạn cần rồi ạ!";

                    var sb = new StringBuilder();
                    sb.AppendLine("Mình tìm thấy mấy đôi nè:\n");

                    while (r.Read())
                    {
                        string ten = r["TENSANPHAM"].ToString();
                        decimal gia = Convert.ToDecimal(r["GIA"]);
                        decimal? giaKM = r.IsDBNull(r.GetOrdinal("GIAKHUYENMAI")) ? (decimal?)null : Convert.ToDecimal(r["GIAKHUYENMAI"]);
                        string anh = r["HINHANH"]?.ToString();
                        int ton = Convert.ToInt32(r["SOLUONGTON"]);

                        string giaHienThi = giaKM.HasValue && giaKM < gia
                            ? $"<s>{gia:N0}₫</s> → <b>{giaKM:N0}₫</b>"
                            : $"{gia:N0}₫";

                        sb.AppendLine($"• {ten}");
                        sb.AppendLine($"   Giá: {giaHienThi}");
                        sb.AppendLine($"   Còn: {ton} đôi");
                        if (!string.IsNullOrEmpty(anh))
                            sb.AppendLine($"Ảnh: /source/images/Products/{anh}");
                        sb.AppendLine();
                    }

                    return sb.ToString();
                }
            }
        }

        //TÌM THEO KHOẢNG GIÁ
        private string TimSanPhamTheoKhoangGia(SqlConnection conn, string msg)
        {
            decimal giaMin = 0;
            decimal giaMax = decimal.MaxValue;

            string cleanMsg = msg.Replace("triệu", "tr").Replace("k", "000").Replace(".", "")
                                 .Replace(",", "").Replace(" ", "");

            var priceMatches = Regex.Matches(cleanMsg, @"\d+(?:\.\d+)?tr|\d{6,}");
            var prices = new List<decimal>();

            foreach (Match m in priceMatches)
            {
                string val = m.Value.Replace("tr", "");
                if (decimal.TryParse(val, out decimal p))
                    prices.Add(m.Value.Contains("tr") ? p * 1000000 : p);
            }

            if (cleanMsg.Contains("dưới"))
            {
                giaMax = prices.Any() ? prices.Max() : 2000000;
            }
            else if (cleanMsg.Contains("trên") || cleanMsg.Contains("từ") && !cleanMsg.Contains("đến"))
            {
                giaMin = prices.Any() ? prices.Min() : 3000000;
            }
            else if (cleanMsg.Contains("đến") || cleanMsg.Contains("tới") || cleanMsg.Contains("-") || cleanMsg.Contains("khoảng"))
            {
                if (prices.Count >= 2)
                {
                    giaMin = prices.Min();
                    giaMax = prices.Max();
                }
                else if (prices.Count == 1)
                {
                    giaMin = prices[0] - 500000;
                    giaMax = prices[0] + 500000;
                }
            }

            string sql = @"
                SELECT TOP 6 TENSANPHAM, GIA, GIAKHUYENMAI, HINHANH, SOLUONGTON
                FROM SANPHAM
                WHERE SOLUONGTON > 0
                  AND (
                        (GIAKHUYENMAI > 0 AND GIAKHUYENMAI BETWEEN @min AND @max)
                     OR (GIAKHUYENMAI IS NULL AND GIA BETWEEN @min AND @max)
                  )
            ";

            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@min", giaMin);
                cmd.Parameters.AddWithValue("@max", giaMax);

                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (!r.HasRows)
                        return "Không có đôi nào trong khoảng giá này còn hàng ạ";

                    var sb = new StringBuilder();
                    sb.AppendLine("Mấy đôi trong tầm giá bạn hỏi nè:\n");

                    while (r.Read())
                    {
                        string ten = r["TENSANPHAM"].ToString();
                        decimal giaHien = r.IsDBNull(r.GetOrdinal("GIAKHUYENMAI"))
                            ? Convert.ToDecimal(r["GIA"])
                            : Convert.ToDecimal(r["GIAKHUYENMAI"]);
                        string anh = r["HINHANH"]?.ToString();

                        sb.AppendLine($"• {ten} → {giaHien:N0}₫");
                        if (!string.IsNullOrEmpty(anh))
                            sb.AppendLine($"Ảnh: /source/images/Products/{anh}");
                        sb.AppendLine();
                    }

                    return sb.ToString();
                }
            }
        }

        // GỢI Ý HOT
        private string GoiYSanPhamHot(SqlConnection conn)
        {
            string sql = @"
                SELECT TOP 4 TENSANPHAM, GIAKHUYENMAI, HINHANH
                FROM SANPHAM
                WHERE SOLUONGTON > 0 AND GIAKHUYENMAI > 0
                ORDER BY NEWID()";

            using (SqlCommand cmd = new SqlCommand(sql, conn))
            using (SqlDataReader r = cmd.ExecuteReader())
            {
                var sb = new StringBuilder();
                sb.AppendLine("Hôm nay shop đang bán chạy mấy đôi này nè:\n");

                while (r.Read())
                {
                    string ten = r["TENSANPHAM"].ToString();
                    decimal gia = Convert.ToDecimal(r["GIAKHUYENMAI"]);
                    string anh = r["HINHANH"]?.ToString();

                    sb.AppendLine($"• {ten} → {gia:N0}₫");
                    if (!string.IsNullOrEmpty(anh))
                        sb.AppendLine($"Ảnh: /source/images/Products/{anh}");
                    sb.AppendLine();
                }

                return sb.ToString();
            }
        }
    }
}
