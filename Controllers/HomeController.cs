using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TinMI.Models;
using TinMI.Services;

namespace TinMI.Controllers;

public class HomeController : Controller
{
    private static readonly string[] BookingTimes =
    [
        "08:00", "09:00", "10:00", "11:00", "13:00", "14:00", "15:00",
        "16:00", "17:00", "18:00", "19:00", "20:00"
    ];

    private readonly ILogger<HomeController> _logger;
    private readonly MiBookingRepository _repository;

    public HomeController(ILogger<HomeController> logger, MiBookingRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    public IActionResult Index()
    {
        ViewData["BookingTimes"] = BookingTimes;
        ViewData["SelectedTime"] = "09:00";

        return View(new KhachHang
        {
            NgayDK = DateTime.Today.AddDays(1)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(KhachHang khachHang, string? gioDK)
    {
        ViewData["BookingTimes"] = BookingTimes;
        ViewData["SelectedTime"] = gioDK;

        if (string.IsNullOrWhiteSpace(gioDK) || !BookingTimes.Contains(gioDK))
        {
            ModelState.AddModelError("gioDK", "Vui lòng chọn giờ làm mi.");
        }
        else if (TimeSpan.TryParse(gioDK, out var time))
        {
            khachHang.NgayDK = khachHang.NgayDK.Date.Add(time);
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
