using AutoMapper;
using FluentValidation;
using HousingHubBackend.Data;
using HousingHubBackend.Dtos.Maintenance;
using HousingHubBackend.Models;
using HousingHubBackend.Services.Events;
using HousingHubBackend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HousingHubBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MaintenanceController : ControllerBase
    {
        private readonly HousingHubDBContext _context;
        private readonly IMapper _mapper;
        private readonly IValidator<MaintenanceBillDto> _billValidator;
        private readonly IValidator<MaintenanceFeeDto> _feeValidator;
        private readonly IEventBroker _eventBroker;
        private readonly INotificationService _notificationService;

        public MaintenanceController(
            HousingHubDBContext context,
            IMapper mapper,
            IValidator<MaintenanceBillDto> billValidator,
            IValidator<MaintenanceFeeDto> feeValidator,
            IEventBroker eventBroker,
            INotificationService notificationService)
        {
            _context = context;
            _mapper = mapper;
            _billValidator = billValidator;
            _feeValidator = feeValidator;
            _eventBroker = eventBroker;
            _notificationService = notificationService;
        }

        private string CurrentRole => User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        private int? CurrentUserId
        {
            get
            {
                if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) && userId > 0)
                {
                    return userId;
                }

                var email = User.FindFirstValue(ClaimTypes.Email)
                    ?? User.FindFirstValue("email");
                if (string.IsNullOrEmpty(email))
                {
                    return null;
                }

                return _context.UserAccounts
                    .Where(u => u.Email == email)
                    .Select(u => (int?)u.UserId)
                    .FirstOrDefault();
            }
        }

        private int? CurrentSocietyId
        {
            get
            {
                if (int.TryParse(User.FindFirstValue("SocietyId"), out var societyId) && societyId > 0)
                {
                    return societyId;
                }

                var userId = CurrentUserId;
                if (!userId.HasValue)
                {
                    return null;
                }

                return _context.UserAccounts
                    .Where(u => u.UserId == userId.Value)
                    .Select(u => u.SocietyId)
                    .FirstOrDefault();
            }
        }

        private int? CurrentFlatId
        {
            get
            {
                if (int.TryParse(User.FindFirstValue("FlatId"), out var flatId) && flatId > 0)
                {
                    return flatId;
                }

                var userId = CurrentUserId;
                if (!userId.HasValue)
                {
                    return null;
                }

                return _context.UserAccounts
                    .Where(u => u.UserId == userId.Value)
                    .Select(u => u.FlatId)
                    .FirstOrDefault();
            }
        }

        private bool CanManageSociety(int? societyId)
        {
            if (CurrentRole == "super_admin")
            {
                return true;
            }

            if (!societyId.HasValue)
            {
                return false;
            }

            var currentSocietyId = CurrentSocietyId;
            return currentSocietyId.HasValue && currentSocietyId.Value == societyId.Value;
        }

        private int? SocietyIdForFlat(int? flatId)
        {
            if (!flatId.HasValue)
            {
                return null;
            }

            return _context.Flats
                .Where(f => f.FlatId == flatId.Value && f.Wing != null)
                .Select(f => f.Wing.SocietyId)
                .FirstOrDefault();
        }

        private int? SocietyIdForWing(int? wingId)
        {
            if (!wingId.HasValue)
            {
                return null;
            }

            return _context.Wings
                .Where(w => w.WingId == wingId.Value)
                .Select(w => w.SocietyId)
                .FirstOrDefault();
        }

        private bool CanAccessBill(MaintenanceBill bill)
        {
            if (CurrentRole == "super_admin")
            {
                return true;
            }

            if (CurrentRole == "resident")
            {
                var flatId = CurrentFlatId;
                return flatId.HasValue && bill.FlatId == flatId.Value;
            }

            return CanManageSociety(SocietyIdForFlat(bill.FlatId));
        }

        private bool CanAccessFee(MaintenanceFee fee)
        {
            if (CurrentRole == "super_admin")
            {
                return true;
            }

            if (CurrentRole == "resident")
            {
                var societyId = CurrentSocietyId;
                return societyId.HasValue && SocietyIdForWing(fee.WingId) == societyId.Value;
            }

            return CanManageSociety(SocietyIdForWing(fee.WingId));
        }

        private static DateOnly TodayIst()
        {
            var indiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, indiaTimeZone));
        }

        private static void NormalizePaymentFields(MaintenanceBillDto dto)
        {
            dto.TransactionId = (dto.TransactionId ?? string.Empty).Trim();

            if (dto.Paid == true)
            {
                dto.Paid = true;
                dto.PaidDate ??= TodayIst();
            }
            else
            {
                dto.Paid = false;
                dto.PaidDate = null;
                dto.TransactionId = string.Empty;
            }
        }

        private async Task PublishBillCreatedAsync(MaintenanceBill bill, MaintenanceBillDto billDto)
        {
            if (bill.FlatId is not int flatId)
            {
                return;
            }

            var user = _context.UserAccounts
                .Where(u => u.FlatId == flatId)
                .OrderBy(u => u.UserId)
                .FirstOrDefault();

            _eventBroker.Publish(new RealtimeEvent
            {
                Type = EventTypes.BillCreated,
                UserIds = user != null ? new[] { user.UserId } : Array.Empty<int>(),
                FlatIds = new[] { flatId },
                Data = billDto
            });

            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
                await _notificationService.SendNotificationAsync(
                    user.Email,
                    "Maintenance Bill Generated",
                    $"A new maintenance bill has been generated for your flat. BillId: {bill.Mbid}"
                );
            }
        }

        private void PublishBillUpdated(MaintenanceBill bill, MaintenanceBillDto billDto)
        {
            if (bill.FlatId is not int flatId)
            {
                return;
            }

            var userIds = _context.UserAccounts
                .Where(u => u.FlatId == flatId)
                .Select(u => u.UserId)
                .ToArray();

            _eventBroker.Publish(new RealtimeEvent
            {
                Type = EventTypes.BillUpdated,
                UserIds = userIds,
                FlatIds = new[] { flatId },
                Data = billDto
            });
        }

        // MaintenanceBill endpoints
        [HttpGet("bills")]
        [Authorize(Roles = "super_admin,admin,security_staff,resident")]
        public IActionResult GetAllBills()
        {
            IQueryable<MaintenanceBill> query = _context.MaintenanceBills.AsNoTracking();

            if (CurrentRole == "resident")
            {
                var flatId = CurrentFlatId;
                if (!flatId.HasValue)
                {
                    return Forbid();
                }

                query = query.Where(b => b.FlatId == flatId.Value);
            }
            else if (CurrentRole != "super_admin")
            {
                var societyId = CurrentSocietyId;
                if (!societyId.HasValue)
                {
                    return Forbid();
                }

                query = query.Where(b => b.Flat != null
                    && b.Flat.Wing != null
                    && b.Flat.Wing.SocietyId == societyId.Value);
            }

            var bills = query.ToList();
            return Ok(_mapper.Map<IEnumerable<MaintenanceBillDto>>(bills));
        }

        [HttpGet("bills/{id}")]
        [Authorize(Roles = "super_admin,admin,security_staff,resident")]
        public IActionResult GetBill(int id)
        {
            var bill = _context.MaintenanceBills
                .AsNoTracking()
                .FirstOrDefault(b => b.Mbid == id);

            if (bill == null)
            {
                return NotFound();
            }

            if (!CanAccessBill(bill))
            {
                return NotFound();
            }

            return Ok(_mapper.Map<MaintenanceBillDto>(bill));
        }

        [HttpPost("bills")]
        [Authorize(Roles = "super_admin,admin,security_staff")]
        public async Task<IActionResult> CreateBill([FromBody] MaintenanceBillDto dto)
        {
            dto.Mbid = 0;

            var flatSocietyId = SocietyIdForFlat(dto.FlatId);
            if (dto.FlatId.HasValue && flatSocietyId == null)
            {
                return BadRequest(new { message = "The selected flat was not found." });
            }

            if (!CanManageSociety(flatSocietyId))
            {
                return Forbid();
            }

            if (dto.Mfid.HasValue && dto.Mfid.Value <= 0)
            {
                dto.Mfid = null;
            }

            if (dto.Mfid.HasValue)
            {
                var fee = _context.MaintenanceFees
                    .Where(f => f.Mfid == dto.Mfid.Value)
                    .Select(f => new { f.Mfid, f.WingId })
                    .FirstOrDefault();

                if (fee == null)
                {
                    return BadRequest(new { message = "The selected maintenance rate was not found." });
                }

                if (!CanManageSociety(SocietyIdForWing(fee.WingId)))
                {
                    return Forbid();
                }
            }

            NormalizePaymentFields(dto);

            var bill = _mapper.Map<MaintenanceBill>(dto);
            _context.MaintenanceBills.Add(bill);
            await _context.SaveChangesAsync();

            var billDto = _mapper.Map<MaintenanceBillDto>(bill);
            await PublishBillCreatedAsync(bill, billDto);

            return CreatedAtAction(nameof(GetBill), new { id = bill.Mbid }, billDto);
        }

        [HttpPut("bills/{id}")]
        [Authorize(Roles = "super_admin,admin,security_staff")]
        public IActionResult UpdateBill(int id, [FromBody] MaintenanceBillDto dto)
        {
            var existing = _context.MaintenanceBills.FirstOrDefault(b => b.Mbid == id);
            if (existing == null)
            {
                return NotFound();
            }

            if (!CanManageSociety(SocietyIdForFlat(existing.FlatId)))
            {
                return Forbid();
            }

            var newFlatSocietyId = SocietyIdForFlat(dto.FlatId);
            if (dto.FlatId.HasValue && newFlatSocietyId == null)
            {
                return BadRequest(new { message = "The selected flat was not found." });
            }

            if (!CanManageSociety(newFlatSocietyId))
            {
                return Forbid();
            }

            if (dto.Mfid.HasValue && dto.Mfid.Value <= 0)
            {
                dto.Mfid = null;
            }

            if (dto.Mfid.HasValue)
            {
                var fee = _context.MaintenanceFees
                    .Where(f => f.Mfid == dto.Mfid.Value)
                    .Select(f => new { f.Mfid, f.WingId })
                    .FirstOrDefault();

                if (fee == null)
                {
                    return BadRequest(new { message = "The selected maintenance rate was not found." });
                }

                if (!CanManageSociety(SocietyIdForWing(fee.WingId)))
                {
                    return Forbid();
                }
            }
            else
            {
                dto.Mfid = existing.Mfid;
            }

            dto.Mbid = id;
            NormalizePaymentFields(dto);

            _mapper.Map(dto, existing);
            _context.SaveChanges();

            var billDto = _mapper.Map<MaintenanceBillDto>(existing);
            PublishBillUpdated(existing, billDto);

            return NoContent();
        }

        [HttpDelete("bills/{id}")]
        [Authorize(Roles = "super_admin,admin")]
        public IActionResult DeleteBill(int id)
        {
            var bill = _context.MaintenanceBills.Find(id);
            if (bill == null)
            {
                return NotFound();
            }

            if (!CanManageSociety(SocietyIdForFlat(bill.FlatId)))
            {
                return Forbid();
            }

            _context.MaintenanceBills.Remove(bill);
            _context.SaveChanges();
            return NoContent();
        }

        [HttpPost("bills/generate")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GenerateBills([FromBody] GenerateMaintenanceBillsDto dto)
        {
            var societyId = CurrentSocietyId;
            if (!societyId.HasValue)
            {
                return Forbid();
            }

            if (dto.PeriodStart is not DateOnly periodStart
                || dto.PeriodEnd is not DateOnly periodEnd
                || dto.DueDate is not DateOnly dueDate)
            {
                return BadRequest(new { message = "PeriodStart, PeriodEnd, and DueDate are required." });
            }

            var wingsQuery = _context.Wings
                .AsNoTracking()
                .Where(w => w.SocietyId == societyId.Value);

            if (dto.WingId.HasValue)
            {
                wingsQuery = wingsQuery.Where(w => w.WingId == dto.WingId.Value);
            }

            var targetWings = wingsQuery.ToList();
            if (targetWings.Count == 0)
            {
                return BadRequest(new { message = "No matching wings were found for your society." });
            }

            var targetWingIds = targetWings.Select(w => w.WingId).ToList();

            var allTargetFlats = _context.Flats
                .AsNoTracking()
                .Where(f => f.WingId.HasValue && targetWingIds.Contains(f.WingId.Value))
                .Select(f => new
                {
                    f.FlatId,
                    f.WingId,
                    f.Area,
                    HasAccount = f.UserAccounts.Any()
                })
                .ToList();

            var candidateFlats = allTargetFlats
                .Where(f => f.HasAccount && f.Area.HasValue && f.Area.Value > 0)
                .ToList();

            var ignoredCount = allTargetFlats.Count - candidateFlats.Count;

            if (candidateFlats.Count == 0)
            {
                return Ok(new GenerateBillsResultDto
                {
                    GeneratedCount = 0,
                    SkippedCount = 0,
                    IgnoredCount = ignoredCount,
                    TotalAmount = 0
                });
            }

            var candidateWingIds = candidateFlats
                .Select(f => f.WingId.GetValueOrDefault())
                .Distinct()
                .ToList();

            var feeByWing = new Dictionary<int, MaintenanceFee>();
            var missingRateWings = new List<string>();

            foreach (var wingId in candidateWingIds)
            {
                var fee = _context.MaintenanceFees
                    .AsNoTracking()
                    .Where(f => f.WingId == wingId
                        && f.IsActive == true
                        && f.RatePerSqft != null
                        && f.EffectiveFrom != null
                        && f.EffectiveFrom <= periodStart
                        && (f.EffectiveTo == null || f.EffectiveTo >= periodStart))
                    .OrderByDescending(f => f.EffectiveFrom)
                    .ThenByDescending(f => f.Mfid)
                    .FirstOrDefault();

                if (fee == null)
                {
                    var wingName = targetWings.FirstOrDefault(w => w.WingId == wingId)?.Name
                        ?? $"Wing {wingId}";
                    missingRateWings.Add(wingName);
                }
                else
                {
                    feeByWing[wingId] = fee;
                }
            }

            if (missingRateWings.Count > 0)
            {
                return BadRequest(new
                {
                    message = $"No active maintenance rate covers the selected period for: {string.Join(", ", missingRateWings)}."
                });
            }

            var candidateFlatIds = candidateFlats.Select(f => f.FlatId).ToList();

            var existingBills = _context.MaintenanceBills
                .AsNoTracking()
                .Where(b => b.FlatId.HasValue
                    && candidateFlatIds.Contains(b.FlatId.Value)
                    && b.PeriodStart != null
                    && b.PeriodEnd != null
                    && b.PeriodStart <= periodEnd
                    && b.PeriodEnd >= periodStart)
                .Select(b => b.FlatId ?? 0)
                .Distinct()
                .ToList();

            var alreadyBilled = existingBills.ToHashSet();
            var newBills = new List<MaintenanceBill>();
            double totalAmount = 0;

            foreach (var flat in candidateFlats)
            {
                if (alreadyBilled.Contains(flat.FlatId))
                {
                    continue;
                }

                var wingId = flat.WingId.GetValueOrDefault();
                var rate = feeByWing[wingId];
                var amount = Math.Round(
                    flat.Area.GetValueOrDefault() * rate.RatePerSqft.GetValueOrDefault(),
                    2,
                    MidpointRounding.AwayFromZero);

                newBills.Add(new MaintenanceBill
                {
                    FlatId = flat.FlatId,
                    Mfid = rate.Mfid,
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                    DueDate = dueDate,
                    Amount = amount,
                    Paid = false,
                    PaidDate = null,
                    TransactionId = string.Empty
                });

                totalAmount += amount;
            }

            var skippedCount = candidateFlats.Count - newBills.Count;

            if (newBills.Count == 0)
            {
                return Ok(new GenerateBillsResultDto
                {
                    GeneratedCount = 0,
                    SkippedCount = skippedCount,
                    IgnoredCount = ignoredCount,
                    TotalAmount = 0
                });
            }

            _context.MaintenanceBills.AddRange(newBills);
            await _context.SaveChangesAsync();

            var generatedFlatIds = newBills
                .Select(b => b.FlatId.GetValueOrDefault())
                .Distinct()
                .ToArray();

            var userIds = _context.UserAccounts
                .Where(u => u.FlatId.HasValue && generatedFlatIds.Contains(u.FlatId.Value))
                .Select(u => u.UserId)
                .Distinct()
                .ToArray();

            var result = new GenerateBillsResultDto
            {
                GeneratedCount = newBills.Count,
                SkippedCount = skippedCount,
                IgnoredCount = ignoredCount,
                TotalAmount = Math.Round(totalAmount, 2, MidpointRounding.AwayFromZero)
            };

            _eventBroker.Publish(new RealtimeEvent
            {
                Type = EventTypes.BillCreated,
                UserIds = userIds,
                FlatIds = generatedFlatIds,
                Data = result
            });

            return Ok(result);
        }

        // MaintenanceFee endpoints
        [HttpGet("fees")]
        [Authorize(Roles = "super_admin,admin,security_staff")]
        public IActionResult GetAllFees()
        {
            IQueryable<MaintenanceFee> query = _context.MaintenanceFees.AsNoTracking();

            if (CurrentRole != "super_admin")
            {
                var societyId = CurrentSocietyId;
                if (!societyId.HasValue)
                {
                    return Forbid();
                }

                query = query.Where(f => f.Wing != null && f.Wing.SocietyId == societyId.Value);
            }

            return Ok(_mapper.Map<IEnumerable<MaintenanceFeeDto>>(query.ToList()));
        }

        [HttpGet("fees/{id}")]
        [Authorize(Roles = "super_admin,admin,security_staff,resident")]
        public IActionResult GetFee(int id)
        {
            var fee = _context.MaintenanceFees
                .AsNoTracking()
                .FirstOrDefault(f => f.Mfid == id);

            if (fee == null)
            {
                return NotFound();
            }

            if (!CanAccessFee(fee))
            {
                return NotFound();
            }

            return Ok(_mapper.Map<MaintenanceFeeDto>(fee));
        }

        [HttpPost("fees")]
        [Authorize(Roles = "super_admin,admin,security_staff")]
        public IActionResult CreateFee([FromBody] MaintenanceFeeDto dto)
        {
            dto.Mfid = 0;

            var wingSocietyId = SocietyIdForWing(dto.WingId);
            if (dto.WingId.HasValue && wingSocietyId == null)
            {
                return BadRequest(new { message = "The selected wing was not found." });
            }

            if (!CanManageSociety(wingSocietyId))
            {
                return Forbid();
            }

            dto.CreatedBy = CurrentUserId;
            dto.IsActive ??= true;

            var fee = _mapper.Map<MaintenanceFee>(dto);
            _context.MaintenanceFees.Add(fee);
            _context.SaveChanges();

            return CreatedAtAction(
                nameof(GetFee),
                new { id = fee.Mfid },
                _mapper.Map<MaintenanceFeeDto>(fee));
        }

        [HttpPut("fees/{id}")]
        [Authorize(Roles = "super_admin,admin,security_staff")]
        public IActionResult UpdateFee(int id, [FromBody] MaintenanceFeeDto dto)
        {
            var existing = _context.MaintenanceFees.FirstOrDefault(f => f.Mfid == id);
            if (existing == null)
            {
                return NotFound();
            }

            if (!CanManageSociety(SocietyIdForWing(existing.WingId)))
            {
                return Forbid();
            }

            var wingSocietyId = SocietyIdForWing(dto.WingId);
            if (dto.WingId.HasValue && wingSocietyId == null)
            {
                return BadRequest(new { message = "The selected wing was not found." });
            }

            if (!CanManageSociety(wingSocietyId))
            {
                return Forbid();
            }

            dto.Mfid = id;
            dto.CreatedBy = existing.CreatedBy;
            dto.IsActive ??= existing.IsActive;

            _mapper.Map(dto, existing);
            _context.SaveChanges();
            return NoContent();
        }

        [HttpDelete("fees/{id}")]
        [Authorize(Roles = "super_admin,admin")]
        public IActionResult DeleteFee(int id)
        {
            var fee = _context.MaintenanceFees.Find(id);
            if (fee == null)
            {
                return NotFound();
            }

            if (!CanManageSociety(SocietyIdForWing(fee.WingId)))
            {
                return Forbid();
            }

            if (_context.MaintenanceBills.Any(b => b.Mfid == id))
            {
                return BadRequest(new
                {
                    message = "This rate is linked to maintenance bills and cannot be deleted. Deactivate it instead."
                });
            }

            _context.MaintenanceFees.Remove(fee);
            _context.SaveChanges();
            return NoContent();
        }
    }
}
