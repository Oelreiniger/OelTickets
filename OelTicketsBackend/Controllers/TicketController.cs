using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OelTicketsBackend.Data;
using System.Security.Claims;

namespace OelTicketsBackend.Controllers;

[ApiController]
[Route("api/ticket")]
public class TicketsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TicketsController(AppDbContext db)
    {
        _db = db;
    }

    public sealed record CreateTicketDto(int ProjectId, int StatusId, string Title, string? Description);
    public sealed record UpdateTicketDto(int? StatusId, string? Title, string? Description);
    public sealed record TicketViewDto(int Id, int ProjectId, int StatusId, string Title, string? Description, string CreatedByUserId, DateTimeOffset CreatedAt, DateTimeOffset? DeletedAt);

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TicketViewDto>>> ListTickets([FromQuery] int? projectId = null, [FromQuery] bool includeDeleted = false)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var isAdmin = User.IsInRole("Admin");

        var q = _db.Tickets.AsQueryable();

        if (!includeDeleted)
            q = q.Where(t => t.DeletedAtUtc == null);

        if (projectId.HasValue)
            q = q.Where(t => t.ProjectId == projectId.Value);

        if (!isAdmin)
        {
            q = q.Where(t =>
                _db.ProjectMemberships.Any(m => m.ProjectId == t.ProjectId && m.UserId == userId)
            );
        }

        var list = await q
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new TicketViewDto(t.Id, t.ProjectId, t.StatusId, t.Title, t.Description, t.CreatedByUserId, t.CreatedAtUtc, t.DeletedAtUtc))
            .ToListAsync();

        return Ok(list);
    }

    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TicketViewDto>> GetTicket(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var t = await _db.Tickets.FirstOrDefaultAsync(x => x.Id == id);
        if (t is null)
            return NotFound("Ticket not found.");

        if (!User.IsInRole("Admin"))
        {
            var isMember = await _db.ProjectMemberships.AnyAsync(m => m.ProjectId == t.ProjectId && m.UserId == userId);
            if (!isMember)
                return Forbid();
        }

        return Ok(new TicketViewDto(t.Id, t.ProjectId, t.StatusId, t.Title, t.Description, t.CreatedByUserId, t.CreatedAtUtc, t.DeletedAtUtc));
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateTicket(CreateTicketDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        if (dto.ProjectId <= 0)
            return BadRequest("ProjectId is required.");

        if (dto.StatusId <= 0)
            return BadRequest("StatusId is required.");

        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest("Title is required.");

        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == dto.ProjectId);
        if (project is null)
            return NotFound("Project not found.");

        if (project.Archived)
            return Conflict("Project is archived.");

        var statusExists = await _db.Statuses.AnyAsync(s => s.Id == dto.StatusId);
        if (!statusExists)
            return NotFound("Status not found.");

        if (!User.IsInRole("Admin"))
        {
            var isMember = await _db.ProjectMemberships.AnyAsync(m => m.ProjectId == dto.ProjectId && m.UserId == userId);
            if (!isMember)
                return Forbid();
        }

        var t = new Ticket
        {
            ProjectId = dto.ProjectId,
            StatusId = dto.StatusId,
            Title = dto.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            CreatedByUserId = userId,
            CreatedAtUtc = DateTime.UtcNow,
            DeletedAtUtc = null
        };

        _db.Tickets.Add(t);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTicket), new { id = t.Id }, new { t.Id });
    }

    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateTicket(int id, UpdateTicketDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var t = await _db.Tickets.FirstOrDefaultAsync(x => x.Id == id);
        if (t is null)
            return NotFound("Ticket not found.");

        if (t.DeletedAtUtc is not null)
            return Conflict("Ticket is deleted.");

        var isAdmin = User.IsInRole("Admin");
        var canEdit = isAdmin || t.CreatedByUserId == userId;

        if (!canEdit)
            return Forbid();

        if (dto.StatusId.HasValue)
        {
            var statusExists = await _db.Statuses.AnyAsync(s => s.Id == dto.StatusId.Value);
            if (!statusExists)
                return NotFound("Status not found.");

            t.StatusId = dto.StatusId.Value;
        }

        if (!string.IsNullOrWhiteSpace(dto.Title))
            t.Title = dto.Title.Trim();

        if (dto.Description is not null)
            t.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

        await _db.SaveChangesAsync();

        return NoContent();
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTicket(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var t = await _db.Tickets.FirstOrDefaultAsync(x => x.Id == id);
        if (t is null)
            return NotFound("Ticket not found.");

        var isAdmin = User.IsInRole("Admin");
        var canDelete = isAdmin || t.CreatedByUserId == userId;

        if (!canDelete)
            return Forbid();

        if (t.DeletedAtUtc is null)
        {
            t.DeletedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return NoContent();
    }
}