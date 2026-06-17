using System.ComponentModel.DataAnnotations;

namespace TinMI.Models;

public class KhachHang
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(30, ErrorMessage = "Họ tên tối đa 30 ký tự.")]
    [Display(Name = "Họ tên")]
    public string TenKh { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [StringLength(15, ErrorMessage = "Số điện thoại tối đa 15 ký tự.")]
    [RegularExpression(@"^[0-9+\-\s]{8,15}$", ErrorMessage = "Số điện thoại không hợp lệ.")]
    [Display(Name = "Số điện thoại")]
    public string Sdt { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn lịch làm mi.")]
    [Display(Name = "Ngày làm mi")]
    public DateTime NgayDK { get; set; }
}
