using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using OSBLE.Attributes;

namespace OSBLE.Models
{
    public class ChangePasswordModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string OldPassword { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [System.Web.Mvc.Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class LogOnModel
    {
        [Required]
        [Display(Name = "User name")]
        public string UserName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }

    public class RegisterModel
    {
        [Required]
        [StringLength(256)]
        [Email(ErrorMessage = "Email Address is not valid!")]
        [DataType(DataType.EmailAddress)]
        [Display(Name = "Email address")]
        public string Email { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm email address")]
        [System.Web.Mvc.Compare("Email", ErrorMessage = "Email address do not match.")]
        public string ConfirmEmail { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [System.Web.Mvc.Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "First name")]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Last name")]
        public string LastName { get; set; }

        [Required]
        [Display(Name = "School")]
        public int SchoolID { get; set; }

        [Display(Name = "School")]
        public School School { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Student, Faculty, or Staff ID Number")]
        public string Identification { get; set; }

        [Required]
        [System.Web.Mvc.Compare("Identification", ErrorMessage = "The school ID number and confirmation school ID number do not match.")]
        [Display(Name = "Confirm Student, Faculty, or Staff ID Number")]
        public string ConfirmIdentification { get; set; }
    }

    public class ResetPasswordModel
    {
        [Required]
        [Email(ErrorMessage = "Email Address is not valid!")]
        [DataType(DataType.EmailAddress)]
        [Display(Name = "Email address")]
        public string EmailAddress { get; set; }
    }

    public class FindUsername
    {
        [Required]
        [Email(ErrorMessage = "Email Address is not valid!")]
        [DataType(DataType.EmailAddress)]
        [Display(Name = "Email address")]
        public string EmailAddress { get; set; }
    }

    public class ContactUsModel
    {
        [Required(ErrorMessage = "Your name is required")]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email Required")]
        [Email(ErrorMessage = "Email Address is not valid!")]
        [DataType(DataType.EmailAddress)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Description Required")]
        [Display(Name = "Message body")]
        public string Message { get; set; }
    }
}