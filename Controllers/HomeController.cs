using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TinMI.Models;
using TinMI.Services;

namespace TinMI.Controllers;

public class HomeController : Controller
{
    private static readonly TimeSpan BookingDuration = TimeSpan.FromHours(2);
    private static readonly TimeSpan[] SlotTimes =
    [
        new(8, 0, 0),
        new(9, 0, 0),
        new(10, 0, 0),
        new(11, 0, 0),
        new(13, 0, 0),
        new(14, 0, 0),
        new(15, 0, 0),
        new(16, 0, 0),
        new(17, 0, 0),
        new(18, 0, 0),
        new(19, 0, 0),
        new(20, 0, 0)
    ];

    private readonly ILogger<HomeController> _logger;
    private readonly MiBookingRepository _repository;
    private readonly BookingEmailService _emailService;

    public HomeController(
        ILogger<HomeController> logger,
        MiBookingRepository repository,
        BookingEmailService emailService)
    {
        _logger = logger;
        _repository = repository;
        _emailService = emailService;
    }

    public async Task<IActionResult> Index()
    {
        var defaultDate = DateTime.Today.AddDays(1);

        ViewData["SelectedTime"] = "09:00";
        ViewData["Slots"] = await BuildSlotsAsync(defaultDate);

        return View(new KhachHang
        {
            NgayDK = defaultDate
        });
    }

    [HttpGet]
    public async Task<IActionResult> Slots(DateTime date)
    {
        var slots = await BuildSlotsAsync(date == default ? DateTime.Today : date);
        return Json(slots);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(KhachHang khachHang, string? gioDK)
    {
        ViewData["SelectedTime"] = gioDK;

        if (string.IsNullOrWhiteSpace(gioDK) || !TimeSpan.TryParse(gioDK, out var time))
        {
            ModelState.AddModelError("gioDK", "Vui lòng chọn giờ làm mi.");
        }
        else
        {
            khachHang.NgayDK = khachHang.NgayDK.Date.Add(time);

            var slots = await BuildSlotsAsync(khachHang.NgayDK.Date);
            var selectedSlot = slots.FirstOrDefault(slot => slot.Time == gioDK);
            if (selectedSlot is null)
            {
                ModelState.AddModelError("gioDK", "Giờ này không nằm trong khung nhận lịch.");
            }
            else if (!selectedSlot.IsAvailable)
            {
                ModelState.AddModelError("gioDK", "Giờ này đã có khách đặt. Vui lòng chọn giờ khác.");
            }
        }

        if (khachHang.NgayDK < DateTime.Now.AddMinutes(30))
        {
            ModelState.AddModelError(nameof(KhachHang.NgayDK), "Vui lòng chọn lịch cách hiện tại ít nhất 30 phút.");
        }

        ViewData["Slots"] = await BuildSlotsAsync(khachHang.NgayDK.Date);

        if (!ModelState.IsValid)
        {
            return View(khachHang);
        }

        await _repository.AddKhachHangAsync(khachHang);
        await _emailService.SendBookingCreatedAsync(khachHang);

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

    private async Task<IReadOnlyList<BookingSlotViewModel>> BuildSlotsAsync(DateTime date)
    {
        IReadOnlyList<DateTime> bookedTimes;
        try
        {
            bookedTimes = await _repository.GetBookingTimesByDateAsync(date);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not load booked slots.");
            bookedTimes = [];
        }

        return SlotTimes
            .Select(time =>
            {
                var start = date.Date.Add(time);
                var end = start.Add(BookingDuration);
                var isBooked = bookedTimes.Any(booked =>
                {
                    var bookedEnd = booked.Add(BookingDuration);
                    return start < bookedEnd && booked < end;
                });

                return new BookingSlotViewModel
                {
                    Time = time.ToString(@"hh\:mm"),
                    Label = start.ToString("HH:mm"),
                    Session = time.Hours < 12 ? "Sáng" : "Chiều",
                    IsAvailable = !isBooked && start >= DateTime.Now.AddMinutes(30)
                };
            })
            .ToList();
    }
}
