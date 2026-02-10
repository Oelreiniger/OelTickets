using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OelTicketsBackend.Data;
using System.Security.Claims;

namespace OelTicketsBackend.Controllers;

[ApiController]
[Route("api/comment")]
public class CommentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public CommentsController(AppDbContext db)
    {
        _db = db;
    }

    public sealed record CreateCommentDto(int TicketId, string Content);
    public sealed record UpdateCommentDto(string? Content);
    public sealed record CommentViewDto(int Id, int TicketId, string Content, string CreatedByUserId, DateTimeOffset CreatedAt, DateTimeOffset? DeletedAt);

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CommentViewDto>>> ListComments([FromQuery] int? ticketId = null, [FromQuery] bool includeDeleted = false)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var isAdmin = User.IsInRole("Admin");

        var q = _db.Comments.AsQueryable();

        if (!includeDeleted)
            q = q.Where(c => c.DeletedAtUtc == null);

        if (ticketId.HasValue)
            q = q.Where(c => c.TicketId == ticketId.Value);

        if (!isAdmin)
        {
            q = q.Where(c =>
                _db.Tickets.Any(t => t.Id == c.TicketId &&
                    _db.ProjectMemberships.Any(m => m.ProjectId == t.ProjectId && m.UserId == userId)
                )
            );
        }

        var list = await q
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new CommentViewDto(c.Id, c.TicketId, c.Content, c.CreatedByUserId, c.CreatedAtUtc, c.DeletedAtUtc))
            .ToListAsync();

        return Ok(list);
    }

    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CommentViewDto>> GetComment(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var c = await _db.Comments.FirstOrDefaultAsync(x => x.Id == id);
        if (c is null)
            return NotFound("Comment not found.");

        if (!User.IsInRole("Admin"))
        {
            var isMember = await _db.Tickets
                .Where(t => t.Id == c.TicketId)
                .AnyAsync(t => _db.ProjectMemberships.Any(m => m.ProjectId == t.ProjectId && m.UserId == userId));

            if (!isMember)
                return Forbid();
        }

        return Ok(new CommentViewDto(c.Id, c.TicketId, c.Content, c.CreatedByUserId, c.CreatedAtUtc, c.DeletedAtUtc));
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateComment(CreateCommentDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        if (dto.TicketId <= 0)
            return BadRequest("TicketId is required.");

        if (string.IsNullOrWhiteSpace(dto.Content))
            return BadRequest("Content is required.");

        var t = await _db.Tickets.FirstOrDefaultAsync(x => x.Id == dto.TicketId);
        if (t is null)
            return NotFound("Ticket not found.");

        if (t.DeletedAtUtc is not null)
            return Conflict("Ticket is deleted.");

        if (!User.IsInRole("Admin"))
        {
            var isMember = await _db.ProjectMemberships.AnyAsync(m => m.ProjectId == t.ProjectId && m.UserId == userId);
            if (!isMember)
                return Forbid();
        }

        var c = new Comment
        {
            TicketId = dto.TicketId,
            Content = dto.Content.Trim(),
            CreatedByUserId = userId,
            CreatedAtUtc = DateTime.UtcNow,
            DeletedAtUtc = null
        };

        _db.Comments.Add(c);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetComment), new { id = c.Id }, new { c.Id });
    }

    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateComment(int id, UpdateCommentDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var c = await _db.Comments.FirstOrDefaultAsync(x => x.Id == id);
        if (c is null)
            return NotFound("Comment not found.");

        if (c.DeletedAtUtc is not null)
            return Conflict("Comment is deleted.");

        var isAdmin = User.IsInRole("Admin");
        var canEdit = isAdmin || c.CreatedByUserId == userId;

        if (!canEdit)
            return Forbid();

        if (dto.Content is not null)
            c.Content = string.IsNullOrWhiteSpace(dto.Content) ? "" : dto.Content.Trim();

        await _db.SaveChangesAsync();

        return NoContent();
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteComment(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var c = await _db.Comments.FirstOrDefaultAsync(x => x.Id == id);
        if (c is null)
            return NotFound("Comment not found.");

        var isAdmin = User.IsInRole("Admin");
        var canDelete = isAdmin || c.CreatedByUserId == userId;

        if (!canDelete)
            return Forbid();

        if (c.DeletedAtUtc is null)
        {
            c.DeletedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return NoContent();
    }
}