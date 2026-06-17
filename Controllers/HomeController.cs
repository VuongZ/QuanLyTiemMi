using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TinMI.Models;
using TinMI.Services;

namespace TinMI.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly MiBookingRepository _repository;

    public HomeController(ILogger<HomeController> logger, MiBookingRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    public IActionResult Index()
    {
        ViewData["SelectedSession"] = "Sáng";
        ViewData["SelectedTime"] = "09:00";

        return View(new KhachHang
        {
            NgayDK = DateTime.Today.AddDays(1)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(KhachHang khachHang, string? buoiDK, string? gioDK)
    {
        ViewData["SelectedSession"] = buoiDK;
        ViewData["SelectedTime"] = gioDK;

        var validSession = buoiDK is "Sáng" or "Chiều";
        if (!validSession)
        {
            ModelState.AddModelError("buoiDK", "Vui lòng chọn buổi làm mi.");
        }

        if (string.IsNullOrWhiteSpace(gioDK) || !TimeSpan.TryParse(gioDK, out var time))
        {
            ModelState.AddModelError("gioDK", "Vui lòng nhập giờ làm mi.");
        }
        else
        {
            khachHang.NgayDK = khachHang.NgayDK.Date.Add(time);

            var isMorning = time >= TimeSpan.FromHours(8) && time < TimeSpan.FromHours(12);
            var isAfternoon = time >= TimeSpan.FromHours(13) && time <= TimeSpan.FromHours(20);

            if (buoiDK == "Sáng" && !isMorning)
            {
                ModelState.AddModelError("gioDK", "Buổi sáng nhận lịch từ 08:00 đến 11:59.");
            }
            else if (buoiDK == "Chiều" && !isAfternoon)
            {
                ModelState.AddModelError("gioDK", "Buổi chiều nhận lịch từ 13:00 đến 20:00.");
            }
        }

        if (khachHang.NgayDK < DateTime.Now.AddMinutes(30))
        {
            ModelState.AddModelError(nameof(KhachHang.NgayDK), "Vui lòng chọn lịch cách hiện tại ít nhất 30 phút.");
        }

        if (!ModelState.IsValid)
        {
            return View(khachHang);
        }

        await _repository.AddKhachHangAsync(khachHang);
        TempData["SuccessMessage"] = "Đặt lịch thành công. TinMI sẽ liên hệ xác nhận sớm nhất.";

        return RedirectToAction(nameof(Index));
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
