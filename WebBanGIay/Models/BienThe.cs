using System;
using System.Collections.Generic;

namespace WebBanGIay.Models
{
    public class BienThe
    {
        public int IDBienThe { get; set; }      // ID biến thể
        public string MauSac { get; set; }      // Màu
        public string Size { get; set; }        // Size
        public int SoLuong { get; set; }        // Số lượng tồn
        public List<int> SizeList { get; set; } = new List<int>();  // List size nếu cần
    }
}
