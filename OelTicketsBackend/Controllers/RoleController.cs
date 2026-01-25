using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OelTicketsBackend.Auth;

namespace OelTicketsBackend.Controllers;

[ApiController]
[Route("api/role")]
public class RoleController : ControllerBase
{
    private readonly RoleManager<IdentityRole> _roles;
    private readonly UserManager<ApplicationUser> _users;

    public RoleController(RoleManager<IdentityRole> roles, UserManager<ApplicationUser> users)
    {
        _roles = roles;
        _users = users;
    }

    public sealed record RoleViewDto(string Id, string Name);
    public sealed record CreateRoleDto(string Name);
    public sealed record RenameRoleDto(string NewName);
    public sealed record RoleUserDto(string UserId, string Email, string FirstName, string LastName);

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public ActionResult<IEnumerable<RoleViewDto>> ListRoles()
    {
        var list = _roles.Roles
            .Select(r => new RoleViewDto(r.Id, r.Name ?? ""))
            .ToList();

        return Ok(list);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{name}")]
    public async Task<ActionResult<RoleViewDto>> GetRole(string name)
    {
        var role = await _roles.FindByNameAsync(name);
        if (role is null)
            return NotFound("Role not found.");

        return Ok(new RoleViewDto(role.Id, role.Name ?? ""));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateRole(CreateRoleDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Role name is required.");

        if (await _roles.RoleExistsAsync(dto.Name))
            return Conflict("Role already exists.");

        var result = await _roles.CreateAsync(new IdentityRole(dto.Name));
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return CreatedAtAction(nameof(GetRole), new { name = dto.Name }, new { dto.Name });
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{name}")]
    public async Task<IActionResult> RenameRole(string name, RenameRoleDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NewName))
            return BadRequest("NewName is required.");

        var role = await _roles.FindByNameAsync(name);
        if (role is null)
            return NotFound("Role not found.");

        if (!string.Equals(name, dto.NewName, StringComparison.OrdinalIgnoreCase) && await _roles.RoleExistsAsync(dto.NewName))
            return Conflict("Role name already exists.");

        role.Name = dto.NewName;

        var result = await _roles.UpdateAsync(role);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{name}")]
    public async Task<IActionResult> DeleteRole(string name)
    {
        var role = await _roles.FindByNameAsync(name);
        if (role is null)
            return NotFound("Role not found.");

        var result = await _roles.DeleteAsync(role);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{name}/users")]
    public async Task<ActionResult<IEnumerable<RoleUserDto>>> ListUsersInRole(string name)
    {
        if (!await _roles.RoleExistsAsync(name))
            return NotFound("Role not found.");

        var users = await _users.GetUsersInRoleAsync(name);

        var list = users.Select(u => new RoleUserDto(
            u.Id,
            u.Email ?? "",
            u.FirstName,
            u.LastName
        )).ToList();

        return Ok(list);
    }

    public sealed record AddUserToRoleDto(string UserId);

    [Authorize(Roles = "Admin")]
    [HttpPost("{name}/users")]
    public async Task<IActionResult> AddUserToRole(string name, AddUserToRoleDto dto)
    {
        if (!await _roles.RoleExistsAsync(name))
            return NotFound("Role not found.");

        var user = await _users.FindByIdAsync(dto.UserId);
        if (user is null)
            return NotFound("User not found.");

        var result = await _users.AddToRoleAsync(user, name);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{name}/users/{userId}")]
    public async Task<IActionResult> RemoveUserFromRole(string name, string userId)
    {
        if (!await _roles.RoleExistsAsync(name))
            return NotFound("Role not found.");

        var user = await _users.FindByIdAsync(userId);
        if (user is null)
            return NotFound("User not found.");

        var result = await _users.RemoveFromRoleAsync(user, name);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return NoContent();
    }
}
