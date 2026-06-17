using System.ComponentModel.DataAnnotations;

namespace TinMI.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tài khoản.")]
    [Display(Name = "Tài khoản")]
    public string User { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Pass { get; set; } = string.Empty;
}
