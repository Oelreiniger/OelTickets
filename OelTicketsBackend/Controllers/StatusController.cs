using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OelTicketsBackend.Data;

namespace OelTicketsBackend.Controllers;

[ApiController]
[Route("api/status")]
public class StatusesController : ControllerBase
{
    private readonly AppDbContext _db;

    public StatusesController(AppDbContext db)
    {
        _db = db;
    }

    public sealed record StatusViewDto(int Id, string Name);
    public sealed record CreateStatusDto(string Name);
    public sealed record UpdateStatusDto(string? Name);

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<StatusViewDto>>> ListStatuses()
    {
        var list = await _db.Statuses
            .OrderBy(s => s.Id)
            .Select(s => new StatusViewDto(s.Id, s.Name))
            .ToListAsync();

        return Ok(list);
    }

    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<StatusViewDto>> GetStatus(int id)
    {
        var s = await _db.Statuses.FirstOrDefaultAsync(x => x.Id == id);
        if (s is null)
            return NotFound("Status not found.");

        return Ok(new StatusViewDto(s.Id, s.Name));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateStatus(CreateStatusDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Name is required.");

        var name = dto.Name.Trim();

        var exists = await _db.Statuses.AnyAsync(x => x.Name == name);
        if (exists)
            return Conflict("Status already exists.");

        var s = new Status { Name = name };

        _db.Statuses.Add(s);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetStatus), new { id = s.Id }, new { s.Id });
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateStatusDto dto)
    {
        var s = await _db.Statuses.FirstOrDefaultAsync(x => x.Id == id);
        if (s is null)
            return NotFound("Status not found.");

        if (!string.IsNullOrWhiteSpace(dto.Name))
        {
            var name = dto.Name.Trim();
            var exists = await _db.Statuses.AnyAsync(x => x.Id != id && x.Name == name);
            if (exists)
                return Conflict("Status already exists.");

            s.Name = name;
        }

        await _db.SaveChangesAsync();

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteStatus(int id)
    {
        var s = await _db.Statuses.FirstOrDefaultAsync(x => x.Id == id);
        if (s is null)
            return NotFound("Status not found.");

        var inUse = await _db.Tickets.AnyAsync(t => t.StatusId == id && t.DeletedAtUtc == null);
        if (inUse)
            return Conflict("Status is in use.");

        _db.Statuses.Remove(s);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}