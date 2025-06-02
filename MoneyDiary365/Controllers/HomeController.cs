using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneyDiary365.Models;

namespace MoneyDiary365.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly Random _random = new Random();

    public HomeController(
        ILogger<HomeController> logger, 
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
    }    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (string.IsNullOrEmpty(userId))
        {
            TempData["ErrorMessage"] = "請先登入";
            return RedirectToAction("Index", "Welcome");
        }
        
        var viewModel = new DashboardViewModel
        {
            CurrentUserId = userId,
            TotalSavings = await _context.SavingRecords
                .Where(s => s.UserId == userId)
                .SumAsync(s => s.Amount),
                
            TotalDays = await _context.SavingRecords
                .Where(s => s.UserId == userId)
                .CountAsync(),
                
            ProgressPercentage = await _context.SavingRecords
                .Where(s => s.UserId == userId)
                .SumAsync(s => s.Amount) / 66795.0 * 100,
                
            NewSaving = new SavingRecord
            { 
                SaveDate = DateTime.Today,
                UserId = userId,
                User = await _userManager.FindByIdAsync(userId)
            },
            
            RecentSavings = await _context.SavingRecords
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.SaveDate)
                .Take(5)
                .ToListAsync()
        };

        return View(viewModel);
    }[HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSaving(DashboardViewModel model)
    {
        if (model?.NewSaving == null)
        {
            TempData["ErrorMessage"] = "無效的表單數據";
            return RedirectToAction(nameof(Index));
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            TempData["ErrorMessage"] = "請先登入";
            return RedirectToAction(nameof(Index));
        }        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            TempData["ErrorMessage"] = "找不到用戶資料";
            return RedirectToAction(nameof(Index));
        }

        var savingRecord = new SavingRecord
        {
            Amount = model.NewSaving.Amount,
            Note = model.NewSaving.Note ?? "",
            SaveDate = DateTime.Today,
            UserId = userId
        };
        
        // 檢查金額是否在有效範圍內
        if (savingRecord.Amount <= 0 || savingRecord.Amount > 365)
        {
            TempData["ErrorMessage"] = "金額必須介於1到365之間";
            return RedirectToAction(nameof(Index));        }
        
        // 檢查金額是否重複
        bool isDuplicate = await _context.SavingRecords
            .Where(s => s.UserId == userId && s.Amount == savingRecord.Amount)
            .AnyAsync();
            
        if (isDuplicate)
        {
            TempData["ErrorMessage"] = "這個金額已經存過了，請選擇其他金額";
            return RedirectToAction(nameof(Index));
        }
        
        // 移除 User 屬性的驗證，因為它是導航屬性，會在資料庫載入時自動填入
        ModelState.Remove("NewSaving.User");
        
        // 檢查模型狀態
        if (!ModelState.IsValid)
        {
            // 收集錯誤信息以供調試
            var errors = string.Join(" | ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));
            TempData["ErrorMessage"] = $"表單驗證失敗: {errors}";
            ModelState.AddModelError("", $"表單驗證失敗: {errors}");
            return RedirectToAction(nameof(Index));
        }        try
        {
            savingRecord.UserId = userId;
            savingRecord.SaveDate = DateTime.Today; // 確保日期已設置
              _context.Add(savingRecord);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"成功存入 {savingRecord.Amount} 元！";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "存款時發生錯誤");
            TempData["ErrorMessage"] = $"存款時發生錯誤: {ex.Message}";
            // 如果有內部異常，也記錄它
            if (ex.InnerException != null)
            {
                TempData["DebugInfo"] += $" | 內部錯誤: {ex.InnerException.Message}";
            }              // 如果發生錯誤，重新載入首頁資料
            var viewModel = new DashboardViewModel
            {
                CurrentUserId = userId,
                TotalSavings = await _context.SavingRecords
                    .Where(s => s.UserId == userId)
                    .SumAsync(s => s.Amount),
                    
                TotalDays = await _context.SavingRecords
                    .Where(s => s.UserId == userId)
                    .CountAsync(),
                    
                ProgressPercentage = await _context.SavingRecords
                    .Where(s => s.UserId == userId)
                    .CountAsync() / 365.0 * 100,
                    
                NewSaving = savingRecord,
                
                RecentSavings = await _context.SavingRecords
                    .Where(s => s.UserId == userId)
                    .OrderByDescending(s => s.SaveDate)
                    .Take(5)
                    .ToListAsync()
            };
            return View("Index", viewModel);
        }
    }    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RandomSaving()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (string.IsNullOrEmpty(userId))
        {
            TempData["ErrorMessage"] = "請先登入";
            return RedirectToAction(nameof(Index));
        }
        
        // 查找用戶已經使用過的金額
        var existingAmounts = await _context.SavingRecords
            .Where(s => s.UserId == userId)
            .Select(s => s.Amount)
            .ToListAsync();
            
        // 找出一個尚未使用過的金額 (1-365)
        var availableAmounts = Enumerable.Range(1, 365)
            .Except(existingAmounts)
            .ToList();
            
        if (!availableAmounts.Any())
        {
            // 已經用完所有可能的金額
            TempData["ErrorMessage"] = "已經使用了所有 1 到 365 的金額，無法再進行隨機存款";
            return RedirectToAction(nameof(Index));
        }
        
        // 從可用金額中隨機選一個
        int randomIndex = _random.Next(0, availableAmounts.Count);
        int randomAmount = availableAmounts[randomIndex];
          // 重新載入首頁資料，但將隨機金額放入輸入框中
        var viewModel = new DashboardViewModel
        {
            CurrentUserId = userId,
            TotalSavings = await _context.SavingRecords
                .Where(s => s.UserId == userId)
                .SumAsync(s => s.Amount),
                
            TotalDays = await _context.SavingRecords
                .Where(s => s.UserId == userId)
                .CountAsync(),
                
            ProgressPercentage = await _context.SavingRecords
                .Where(s => s.UserId == userId)
                .CountAsync() / 365.0 * 100,
                
            NewSaving = new SavingRecord 
            { 
                SaveDate = DateTime.Today,
                Amount = randomAmount,
                Note = "隨機存款"
            },
            
            RecentSavings = await _context.SavingRecords
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.SaveDate)
                .Take(5)
                .ToListAsync()
        };
          TempData["SuccessMessage"] = $"已生成隨機金額：{randomAmount}，請按「存錢」按鈕確認";
        return View("Index", viewModel);
    }
      public async Task<IActionResult> History()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (string.IsNullOrEmpty(userId))
        {
            TempData["ErrorMessage"] = "請先登入";
            return RedirectToAction("Index", "Welcome");
        }
        
        var records = await _context.SavingRecords
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.SaveDate)
            .ToListAsync();
            
        return View(records);
    }

    public async Task<IActionResult> SavingImg()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (string.IsNullOrEmpty(userId))
        {
            TempData["ErrorMessage"] = "請先登入";
            return RedirectToAction("Index", "Welcome");
        }
        
        var records = await _context.SavingRecords
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.SaveDate)
            .ToListAsync();
            
        return View(records);
    }    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSaving(int id, string? returnUrl = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (string.IsNullOrEmpty(userId))
        {
            TempData["ErrorMessage"] = "請先登入";
            return RedirectToAction("Index", "Welcome");
        }
        
        var savingRecord = await _context.SavingRecords
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
            
        if (savingRecord == null)
        {
            TempData["ErrorMessage"] = "找不到要刪除的記錄，或您沒有權限刪除此記錄";
            return RedirectToAction(returnUrl ?? nameof(Index));
        }
        
        try
        {
            _context.SavingRecords.Remove(savingRecord);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"已成功刪除金額 {savingRecord.Amount} 元的記錄";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刪除存款記錄時發生錯誤");
            TempData["ErrorMessage"] = "刪除記錄時發生錯誤，請稍後再試";
        }
          // 根據 returnUrl 決定重導向位置
        if (!string.IsNullOrEmpty(returnUrl))
        {
            if (returnUrl.Equals("History", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(History));
            }
            else if (returnUrl.Equals("SavingImg", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(SavingImg));
            }
        }
        
        return RedirectToAction(nameof(Index));
    }
    
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
