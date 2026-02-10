using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OelTicketsBackend.Data;
using System.Security.Claims;

namespace OelTicketsBackend.Controllers;

[ApiController]
[Route("api/project")]
public class ProjectsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProjectsController(AppDbContext db)
    {
        _db = db;
    }

    public sealed record CreateProjectDto(string Name, string? Description);
    public sealed record UpdateProjectDto(string? Name, string? Description, bool? Archived);
    public sealed record ProjectViewDto(int Id, string Name, string? Description, bool Archived);

    public sealed record ProjectMemberDto(string UserId);
    public sealed record MembershipViewDto(int ProjectId, string UserId);

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectViewDto>>> ListProjects()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var isAdmin = User.IsInRole("Admin");

        var q = _db.Projects.AsQueryable();

        if (!isAdmin)
        {
            q = q.Where(p =>
                _db.ProjectMemberships.Any(m => m.ProjectId == p.Id && m.UserId == userId)
            );
        }

        var list = await q
            .OrderBy(p => p.Name)
            .Select(p => new ProjectViewDto(p.Id, p.Name, p.Description, p.Archived))
            .ToListAsync();

        return Ok(list);
    }

    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProjectViewDto>> GetProject(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var p = await _db.Projects.FirstOrDefaultAsync(x => x.Id == id);
        if (p is null)
            return NotFound("Project not found.");

        if (!User.IsInRole("Admin"))
        {
            var isMember = await _db.ProjectMemberships.AnyAsync(m => m.ProjectId == id && m.UserId == userId);
            if (!isMember)
                return Forbid();
        }

        return Ok(new ProjectViewDto(p.Id, p.Name, p.Description, p.Archived));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateProject(CreateProjectDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Name is required.");

        var p = new Project
        {
            Name = dto.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            Archived = false
        };

        _db.Projects.Add(p);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProject), new { id = p.Id }, new { p.Id });
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateProject(int id, UpdateProjectDto dto)
    {
        var p = await _db.Projects.FirstOrDefaultAsync(x => x.Id == id);
        if (p is null)
            return NotFound("Project not found.");

        if (!string.IsNullOrWhiteSpace(dto.Name))
            p.Name = dto.Name.Trim();

        if (dto.Description is not null)
            p.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

        if (dto.Archived.HasValue)
            p.Archived = dto.Archived.Value;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProject(int id)
    {
        var p = await _db.Projects.FirstOrDefaultAsync(x => x.Id == id);
        if (p is null)
            return NotFound("Project not found.");

        var memberships = _db.ProjectMemberships.Where(m => m.ProjectId == id);
        _db.ProjectMemberships.RemoveRange(memberships);

        _db.Projects.Remove(p);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [Authorize]
    [HttpGet("{id}/members")]
    public async Task<ActionResult<IEnumerable<MembershipViewDto>>> ListMembers(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var projectExists = await _db.Projects.AnyAsync(p => p.Id == id);
        if (!projectExists)
            return NotFound("Project not found.");

        if (!User.IsInRole("Admin"))
        {
            var isMember = await _db.ProjectMemberships.AnyAsync(m => m.ProjectId == id && m.UserId == userId);
            if (!isMember)
                return Forbid();
        }

        var list = await _db.ProjectMemberships
            .Where(m => m.ProjectId == id)
            .OrderBy(m => m.UserId)
            .Select(m => new MembershipViewDto(m.ProjectId, m.UserId))
            .ToListAsync();

        return Ok(list);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id}/members")]
    public async Task<IActionResult> AddMember(int id, ProjectMemberDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.UserId))
            return BadRequest("UserId is required.");

        var projectExists = await _db.Projects.AnyAsync(p => p.Id == id);
        if (!projectExists)
            return NotFound("Project not found.");

        var userExists = await _db.Users.AnyAsync(u => u.Id == dto.UserId);
        if (!userExists)
            return NotFound("User not found.");

        var exists = await _db.ProjectMemberships.AnyAsync(m => m.ProjectId == id && m.UserId == dto.UserId);
        if (exists)
            return Conflict("User is already a member.");

        _db.ProjectMemberships.Add(new ProjectMembership { ProjectId = id, UserId = dto.UserId });
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(int id, string userId)
    {
        var m = await _db.ProjectMemberships.FirstOrDefaultAsync(x => x.ProjectId == id && x.UserId == userId);
        if (m is null)
            return NotFound("Membership not found.");

        _db.ProjectMemberships.Remove(m);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}