using System;
using System.ComponentModel.DataAnnotations;

namespace LMS.ViewModels.LoginViewModel
{

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        public string? Username { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }
    }
}