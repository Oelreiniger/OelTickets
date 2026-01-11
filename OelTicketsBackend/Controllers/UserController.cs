using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OelTicketsBackend.Auth;
using System.Security.Claims;

namespace OelTicketsBackend.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    //TODO: Implement multiple roles
    private readonly UserManager<ApplicationUser> _users;

    public UsersController(UserManager<ApplicationUser> users)
    {
        _users = users;
    }

    public sealed record CreateUserDto(string Email, string Password, string FirstName, string LastName, string Role);
    public sealed record UpdateUserDto(string? Email, string? FirstName, string? LastName, string? Role, string? NewPassword);
    public sealed record UserViewDto(string Id, string Email, string FirstName, string LastName, string[] Roles);

    // -------------------------
    // Current user endpoints
    // -------------------------

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new
        {
            userId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            email = User.FindFirstValue(ClaimTypes.Email),
            roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToArray()
        });
    }

    [Authorize]
    [HttpGet("me/profile")]
    public async Task<ActionResult<UserViewDto>> MyProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) 
            return Unauthorized();

        var me = await _users.FindByIdAsync(userId);
        if (me is null) 
            return Unauthorized();

        var roles = await _users.GetRolesAsync(me);
        return Ok(new UserViewDto(
            me.Id,
            me.Email ?? "",
            me.FirstName,
            me.LastName,
            roles.ToArray()
        ));
    }

    // -------------------------
    // Admin endpoints
    // -------------------------

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserViewDto>>> ListUsers()
    {
        var users = _users.Users.ToList();

        var result = new List<UserViewDto>(users.Count);
        foreach (var u in users)
        {
            var roles = await _users.GetRolesAsync(u);
            result.Add(new UserViewDto(u.Id, u.Email ?? "", u.FirstName, u.LastName, roles.ToArray()));
        }

        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{id}")]
    public async Task<ActionResult<UserViewDto>> GetUser(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null) 
            return NotFound("User not found.");

        var roles = await _users.GetRolesAsync(user);

        return Ok(new UserViewDto(
            user.Id,
            user.Email ?? "",
            user.FirstName,
            user.LastName,
            roles.ToArray()
        ));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateUser(CreateUserDto dto)
    {
        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            EmailConfirmed = true
        };

        var create = await _users.CreateAsync(user, dto.Password);
        if (!create.Succeeded) 
            return BadRequest(create.Errors);

        var addRole = await _users.AddToRoleAsync(user, dto.Role);
        if (!addRole.Succeeded) 
            return BadRequest(addRole.Errors);

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new { user.Id });
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string id, UpdateUserDto dto)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null) 
            return NotFound("User not found.");

        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            user.Email = dto.Email;
            user.UserName = dto.Email;
        }

        if (!string.IsNullOrWhiteSpace(dto.FirstName)) 
            user.FirstName = dto.FirstName;

        if (!string.IsNullOrEmpty(dto.LastName)) 
            user.LastName = dto.LastName;

        var update = await _users.UpdateAsync(user);
        if (!update.Succeeded) 
            return BadRequest(update.Errors);

        if (!string.IsNullOrWhiteSpace(dto.Role))
        {
            var currentRoles = await _users.GetRolesAsync(user);

            if (currentRoles.Count > 0)
            {
                var removeRoles = await _users.RemoveFromRolesAsync(user, currentRoles);
                if (!removeRoles.Succeeded) return BadRequest(removeRoles.Errors);
            }

            var addRole = await _users.AddToRoleAsync(user, dto.Role);
            if (!addRole.Succeeded) 
                return BadRequest(addRole.Errors);
        }

        if (!string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            var hasPassword = await _users.HasPasswordAsync(user);
            IdentityResult pwResult;
            if (hasPassword)
            {
                pwResult = await _users.RemovePasswordAsync(user);
                if (!pwResult.Succeeded) 
                    return BadRequest(pwResult.Errors);
            }

            pwResult = await _users.AddPasswordAsync(user, dto.NewPassword);
            if (!pwResult.Succeeded) 
                return BadRequest(pwResult.Errors);
        }

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null) 
            return NotFound("User not found.");

        var result = await _users.DeleteAsync(user);
        if (!result.Succeeded) 
            return BadRequest(result.Errors);

        return NoContent();
    }
}
