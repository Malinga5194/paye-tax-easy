using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayeTaxEasy.Infrastructure.Data;
using PayeTaxEasy.Infrastructure.Entities;

namespace PayeTaxEasy.Api.Controllers;

[ApiController]
[Route("admin")]
[Authorize(Roles = "SystemAdmin")]
public class AdminController : ControllerBase
{
    private readonly PayeTaxEasyDbContext _db;

    // Protected demo accounts — cannot be deleted, password-reset, or deactivated
    private static readonly HashSet<string> ProtectedEmails = new(StringComparer.OrdinalIgnoreCase)
    {
        "employer@test.com", "ird@test.com",
        "admin@test.com", "admin@payetaxeasy.lk",
        "amal.perera@test.com", "bhagya.silva@test.com",
        "chaminda.fernando@test.com", "dilini.jayasinghe@test.com",
        "eranga.bandara@test.com", "fathima.rizna@test.com",
        "gayan.wickrama@test.com", "harsha.rathnayake@test.com"
    };

    public AdminController(PayeTaxEasyDbContext db) => _db = db;

    /// <summary>GET /admin/users — List all users</summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _db.AppUsers
            .OrderBy(u => u.Role).ThenBy(u => u.FullName)
            .Select(u => new {
                u.Id, u.Email, u.FullName, u.Role, u.TIN, u.IsActive, u.CreatedAt,
                IsProtected = ProtectedEmails.Contains(u.Email)
            }).ToListAsync();
        return Ok(users);
    }

    /// <summary>POST /admin/users — Create a new user</summary>
    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password)
            || string.IsNullOrWhiteSpace(req.FullName) || string.IsNullOrWhiteSpace(req.Role))
            return UnprocessableEntity(new { errorCode = "REG_001", message = "All fields are required." });

        var validRoles = new[] { "Employer", "Employee", "IRD_Officer", "SystemAdmin" };
        if (!validRoles.Contains(req.Role))
            return UnprocessableEntity(new { errorCode = "REG_002", message = "Invalid role." });

        // TIN is mandatory for Employer and Employee roles only
        var tinRequiredRoles = new[] { "Employer", "Employee" };
        if (tinRequiredRoles.Contains(req.Role) &&
            (string.IsNullOrWhiteSpace(req.TIN) || req.TIN.Trim().Length != 9 || !req.TIN.Trim().All(char.IsDigit)))
            return UnprocessableEntity(new { errorCode = "REG_004", message = "A valid 9-digit TIN is required for Employer and Employee roles." });

        var allUsers = await _db.AppUsers.ToListAsync();
        var exists = allUsers.Any(u => u.Email.Equals(req.Email, StringComparison.OrdinalIgnoreCase));
        if (exists)
            return Conflict(new { errorCode = "REG_003", message = "Email already exists." });

        // Check TIN uniqueness for Employer/Employee roles
        if (tinRequiredRoles.Contains(req.Role))
        {
            var tinTrimmed = req.TIN!.Trim();
            var tinExistsInUsers = allUsers.Any(u => u.TIN == tinTrimmed);
            var tinExistsInEmployees = await _db.Employees.AnyAsync(e => e.TIN == tinTrimmed);
            var tinExistsInEmployers = await _db.Employers.AnyAsync(e => e.TIN == tinTrimmed);
            if (tinExistsInUsers || tinExistsInEmployees || tinExistsInEmployers)
                return Conflict(new { errorCode = "REG_005", message = $"TIN {tinTrimmed} is already registered in the system." });
        }

        // Auto-generate staff ID for IRD_Officer and SystemAdmin
        string assignedTin;
        if (req.Role == "IRD_Officer" || req.Role == "SystemAdmin")
        {
            var prefix = req.Role == "IRD_Officer" ? "IRD" : "ADM";
            var existingCount = allUsers.Count(u => u.Role == req.Role);
            assignedTin = $"{prefix}-{(existingCount + 1):D4}";
        }
        else
        {
            assignedTin = req.TIN!.Trim();
        }

        var user = new AppUser
        {
            Email = req.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role = req.Role,
            FullName = req.FullName,
            TIN = assignedTin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            _db.AppUsers.Add(user);
            await _db.SaveChangesAsync();

            // Auto-create Employee entity so they can be used in payroll
            if (req.Role == "Employee")
            {
                var employeeExists = await _db.Employees.AnyAsync(e => e.TIN == assignedTin);
                if (!employeeExists)
                {
                    _db.Employees.Add(new PayeTaxEasy.Infrastructure.Entities.Employee
                    {
                        TIN = assignedTin,
                        FullName = req.FullName,
                        NICNumber = $"NIC-{assignedTin}",
                        ContactEmail = req.Email.ToLower(),
                        ContactPhone = string.Empty,
                        CreatedAt = DateTime.UtcNow
                    });
                    await _db.SaveChangesAsync();
                }
            }

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return UnprocessableEntity(new { errorCode = "REG_006", message = $"Failed to create user: {ex.InnerException?.Message ?? ex.Message}" });
        }

        return Ok(new { message = "User created.", userId = user.Id });
    }

    /// <summary>PUT /admin/users/{id}/toggle — Activate or deactivate user</summary>
    [HttpPut("users/{id:guid}/toggle")]
    public async Task<IActionResult> ToggleUser(Guid id)
    {
        var user = await _db.AppUsers.FindAsync(id);
        if (user == null) return NotFound(new { message = "User not found." });
        if (ProtectedEmails.Contains(user.Email))
            return BadRequest(new { message = "This is a protected demo account and cannot be modified." });
        user.IsActive = !user.IsActive;
        await _db.SaveChangesAsync();
        return Ok(new { message = $"User {(user.IsActive ? "activated" : "deactivated")}.", isActive = user.IsActive });
    }

    /// <summary>PUT /admin/users/{id}/reset-password — Reset user password</summary>
    [HttpPut("users/{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.NewPassword) || req.NewPassword.Length < 6)
            return UnprocessableEntity(new { message = "Password must be at least 6 characters." });

        var user = await _db.AppUsers.FindAsync(id);
        if (user == null) return NotFound(new { message = "User not found." });
        if (ProtectedEmails.Contains(user.Email))
            return BadRequest(new { message = "This is a protected demo account. Password cannot be reset." });
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Password reset successfully." });
    }

    /// <summary>DELETE /admin/users/{id} — Delete user</summary>
    [HttpDelete("users/{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        // Prevent admin from deleting themselves
        var currentUserId = User.FindFirst("sub")?.Value;
        if (currentUserId == id.ToString())
            return BadRequest(new { message = "You cannot delete your own account." });

        var user = await _db.AppUsers.FindAsync(id);
        if (user == null) return NotFound(new { message = "User not found." });
        if (ProtectedEmails.Contains(user.Email))
            return BadRequest(new { message = "This is a protected demo account and cannot be deleted." });
        _db.AppUsers.Remove(user);
        await _db.SaveChangesAsync();
        return Ok(new { message = "User deleted." });
    }
}

public record CreateUserRequest(string Email, string Password, string FullName, string Role, string? TIN);
public record ResetPasswordRequest(string NewPassword);
