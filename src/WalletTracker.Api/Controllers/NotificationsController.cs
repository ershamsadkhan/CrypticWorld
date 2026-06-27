using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WalletTracker.Application.Dtos;
using WalletTracker.Application.Interfaces;
using WalletTracker.Domain;

namespace WalletTracker.Api.Controllers;

[ApiController]
[Route("api/notifications/channels")]
public class NotificationsController : ControllerBase
{
    private readonly IWalletTrackerDbContext _db;
    private readonly IEnumerable<INotificationSender> _senders;

    public NotificationsController(IWalletTrackerDbContext db, IEnumerable<INotificationSender> senders)
    {
        _db = db;
        _senders = senders;
    }

    [HttpGet]
    public async Task<ActionResult<List<NotificationChannelDto>>> GetAll(CancellationToken ct)
    {
        var channels = await _db.NotificationChannels
            .Select(c => new NotificationChannelDto(c.Id, c.Type, c.ConfigJson, c.IsEnabled, c.UpdatedAt))
            .ToListAsync(ct);
        return Ok(channels);
    }

    [HttpPost]
    public async Task<ActionResult<NotificationChannelDto>> Create(UpsertNotificationChannelRequest request, CancellationToken ct)
    {
        var channel = new NotificationChannel
        {
            Type = request.Type,
            ConfigJson = request.ConfigJson,
            IsEnabled = request.IsEnabled
        };
        _db.NotificationChannels.Add(channel);
        await _db.SaveChangesAsync(ct);

        return Ok(new NotificationChannelDto(channel.Id, channel.Type, channel.ConfigJson, channel.IsEnabled, channel.UpdatedAt));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<NotificationChannelDto>> Update(int id, UpsertNotificationChannelRequest request, CancellationToken ct)
    {
        var channel = await _db.NotificationChannels.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (channel is null) return NotFound();

        channel.Type = request.Type;
        channel.ConfigJson = request.ConfigJson;
        channel.IsEnabled = request.IsEnabled;
        channel.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(new NotificationChannelDto(channel.Id, channel.Type, channel.ConfigJson, channel.IsEnabled, channel.UpdatedAt));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var channel = await _db.NotificationChannels.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (channel is null) return NotFound();

        _db.NotificationChannels.Remove(channel);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:int}/test")]
    public async Task<IActionResult> SendTest(int id, CancellationToken ct)
    {
        var channel = await _db.NotificationChannels.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (channel is null) return NotFound();

        var sender = _senders.FirstOrDefault(s => s.Type == channel.Type);
        if (sender is null) return BadRequest("No sender registered for this channel type.");

        await sender.SendAsync(channel, "✅ Wallet Tracker test notification", ct);
        return Ok();
    }
}
