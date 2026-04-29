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

    public AdminController(PayeTaxEasyDbContext db) => _db = db;

    /// <summary>GET /admin/users — List all users</summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _db.AppUsers
            .OrderBy(u => u.Role).ThenBy(u => u.FullName)
            .Select(u => new {
                u.Id, u.Email, u.FullName, u.Role, u.TIN, u.IsActive, u.CreatedAt
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

        var allUsers = await _db.AppUsers.ToListAsync();
        var exists = allUsers.Any(u => u.Email.Equals(req.Email, StringComparison.OrdinalIgnoreCase));
        if (exists)
            return Conflict(new { errorCode = "REG_003", message = "Email already exists." });

        var user = new AppUser
        {
            Email = req.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role = req.Role,
            FullName = req.FullName,
            TIN = req.TIN ?? string.Empty,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();
        return Ok(new { message = "User created.", userId = user.Id });
    }

    /// <summary>PUT /admin/users/{id}/toggle — Activate or deactivate user</summary>
    [HttpPut("users/{id:guid}/toggle")]
    public async Task<IActionResult> ToggleUser(Guid id)
    {
        var user = await _db.AppUsers.FindAsync(id);
        if (user == null) return NotFound(new { message = "User not found." });
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
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Password reset successfully." });
    }

    /// <summary>DELETE /admin/users/{id} — Delete user</summary>
    [HttpDelete("users/{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await _db.AppUsers.FindAsync(id);
        if (user == null) return NotFound(new { message = "User not found." });
        _db.AppUsers.Remove(user);
        await _db.SaveChangesAsync();
        return Ok(new { message = "User deleted." });
    }
}

public record CreateUserRequest(string Email, string Password, string FullName, string Role, string? TIN);
public record ResetPasswordRequest(string NewPassword);
