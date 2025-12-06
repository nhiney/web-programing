using System;

namespace WebBanGIay.Models
{
    public class UserProfileViewModel
    {
        public string UserName { get; set; }
        public string AccountType { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string AvatarUrl { get; set; }

        // Change Password
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmNewPassword { get; set; }
    }
}
