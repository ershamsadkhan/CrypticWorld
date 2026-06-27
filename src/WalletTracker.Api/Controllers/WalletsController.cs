using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WalletTracker.Application.Dtos;
using WalletTracker.Application.Interfaces;
using WalletTracker.Domain;

namespace WalletTracker.Api.Controllers;

[ApiController]
[Route("api/wallets")]
public class WalletsController : ControllerBase
{
    private readonly IWalletTrackerDbContext _db;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WalletsController> _logger;

    public WalletsController(IWalletTrackerDbContext db, IServiceScopeFactory scopeFactory, ILogger<WalletsController> logger)
    {
        _db = db;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<WalletDto>>> GetAll(CancellationToken ct)
    {
        var wallets = await _db.Wallets
            .Select(w => new WalletDto(w.Id, w.Address, w.Chain, w.Label, w.IsActive, w.CreatedAt, w.BackfillStatus))
            .ToListAsync(ct);
        return Ok(wallets);
    }

    [HttpPost]
    public async Task<ActionResult<WalletDto>> Create(CreateWalletRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Address))
        {
            return BadRequest("Address is required.");
        }

        var exists = await _db.Wallets.AnyAsync(w => w.Address == request.Address && w.Chain == request.Chain, ct);
        if (exists)
        {
            return Conflict("This wallet is already tracked on this chain.");
        }

        var wallet = new TrackedWallet
        {
            Address = request.Address,
            Chain = request.Chain,
            Label = request.Label
        };
        _db.Wallets.Add(wallet);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetAll), new WalletDto(wallet.Id, wallet.Address, wallet.Chain, wallet.Label, wallet.IsActive, wallet.CreatedAt, wallet.BackfillStatus));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.Id == id, ct);
        if (wallet is null) return NotFound();

        _db.Wallets.Remove(wallet);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("{id:int}/trades")]
    public async Task<ActionResult<List<TradeDto>>> GetTrades(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var trades = await _db.Trades
            .Where(t => t.WalletId == id)
            .OrderByDescending(t => t.BlockTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TradeDto(t.Id, t.WalletId, t.Chain, t.TxHash, t.BlockTime, t.Direction,
                t.TokenAddress, t.TokenSymbol, t.AmountToken, t.AmountQuote, t.PricePerTokenInQuote, t.QuoteSymbol, t.DexName))
            .ToListAsync(ct);

        return Ok(trades);
    }

    [HttpGet("{id:int}/stats")]
    public async Task<ActionResult<WalletStatsDto>> GetStats(int id, CancellationToken ct)
    {
        var stats = await _db.WalletStats.FirstOrDefaultAsync(s => s.WalletId == id, ct);
        if (stats is null) return NotFound();

        var pnlByQuote = await _db.QuotePnLs
            .Where(q => q.WalletId == id)
            .Select(q => new QuotePnLDto(q.QuoteSymbol, q.RealizedPnL, q.UnrealizedPnL))
            .ToListAsync(ct);

        return Ok(new WalletStatsDto(stats.WalletId, stats.TotalTrades, stats.TotalSells, stats.WinningSells,
            stats.WinRate, pnlByQuote, stats.UpdatedAt));
    }

    [HttpPost("{id:int}/backfill")]
    public async Task<IActionResult> Backfill(int id, CancellationToken ct)
    {
        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.Id == id, ct);
        if (wallet is null) return NotFound();

        if (wallet.BackfillStatus == BackfillStatus.InProgress)
        {
            return Conflict("Backfill is already in progress for this wallet.");
        }

        // Runs detached from the request's DbContext/scope since full history can take a long time.
        var walletId = wallet.Id;
        var chain = wallet.Chain;
        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var scopedDb = scope.ServiceProvider.GetRequiredService<IWalletTrackerDbContext>();
            var indexers = scope.ServiceProvider.GetServices<IChainIndexer>();
            var indexer = indexers.FirstOrDefault(i => i.Chain == chain);
            if (indexer is null)
            {
                _logger.LogWarning("No indexer registered for chain {Chain}, cannot backfill wallet {WalletId}", chain, walletId);
                return;
            }

            var scopedWallet = await scopedDb.Wallets.FirstOrDefaultAsync(w => w.Id == walletId);
            if (scopedWallet is null) return;

            try
            {
                await indexer.BackfillAsync(scopedWallet, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Backfill task failed for wallet {WalletId}", walletId);
            }
        }, CancellationToken.None);

        return Accepted();
    }

    [HttpGet("{id:int}/alerts")]
    public async Task<ActionResult<List<NewTokenAlertDto>>> GetAlerts(int id, CancellationToken ct)
    {
        var alerts = await _db.NewTokenAlerts
            .Where(a => a.WalletId == id)
            .OrderByDescending(a => a.FirstSeenAt)
            .Select(a => new NewTokenAlertDto(a.Id, a.WalletId, a.TokenAddress, a.TokenSymbol, a.FirstSeenAt, a.Notified))
            .ToListAsync(ct);

        return Ok(alerts);
    }
}
