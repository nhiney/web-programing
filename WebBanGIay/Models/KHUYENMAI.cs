
namespace WebBanGIay.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class KHUYENMAI
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public KHUYENMAI()
        {
            this.SANPHAM = new HashSet<SANPHAM>();
        }
    
        public int MAKHUYENMAI { get; set; }
        public string TENKHUYENMAI { get; set; }
        public System.DateTime NGAYBATDAU { get; set; }
        public System.DateTime NGAYKETTHUC { get; set; }
        public int PHANTRAMGIAM { get; set; }
        public Nullable<bool> TRANGTHAI { get; set; }
        public string MOTA { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<SANPHAM> SANPHAM { get; set; }
    }
}
