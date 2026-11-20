using AutoMapper;
using FluentValidation;
using HousingHubBackend.Data;
using HousingHubBackend.Dtos;
using HousingHubBackend.Models;
using HousingHubBackend.Services.Events;
using HousingHubBackend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace HousingHubBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BookingsController : ControllerBase
    {
        private readonly HousingHubDBContext _context;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateBookingDto> _createValidator;
        private readonly IValidator<UpdateBookingDto> _updateValidator;
        private readonly IEventBroker _eventBroker;
        private readonly INotificationService _notificationService;

        public BookingsController(
            HousingHubDBContext context,
            IMapper mapper,
            IValidator<CreateBookingDto> createValidator,
            IValidator<UpdateBookingDto> updateValidator,
            IEventBroker eventBroker,
            INotificationService notificationService)
        {
            _context = context;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _eventBroker = eventBroker;
            _notificationService = notificationService;
        }

        [HttpGet]
        [Authorize(Roles = "super_admin,admin,security_staff,resident")]
        public IActionResult GetAll()
        {
            var bookings = _context.Bookings
                .Select(b => new {
                    b.BookingId,
                    b.UserId,
                    UserName = b.User != null ? b.User.Name : null,
                    b.FlatId,
                    b.AmenityId,
                    b.CreatedAt,
                    b.StartDate,
                    b.EndDate,
                    b.StartTime,
                    b.EndTime,
                    b.Amount,
                    b.Paid,
                    b.TransactionId,
                    b.Status
                })
                .ToList();
            return Ok(bookings);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "super_admin,admin,security_staff,resident")]
        public IActionResult Get(int id)
        {
            var booking = _context.Bookings.Find(id);
            if (booking == null) return NotFound();
            return Ok(_mapper.Map<BookingDto>(booking));
        }

        [HttpPost]
        [Authorize(Roles = "super_admin,admin,security_staff,resident")]
        public async Task<IActionResult> Create([FromBody] CreateBookingDto dto)
        {
            // Set CreatedAt to current date
            dto.CreatedAt = DateOnly.FromDateTime(DateTime.Now);

            // Calculate amount based on amenity hourly rate and booking duration
            var amenity = _context.Amenities.FirstOrDefault(a => a.AmenityId == dto.AmenityId);
            if (amenity == null || !amenity.HourlyRate.HasValue || !dto.StartTime.HasValue || !dto.EndTime.HasValue)
            {
                return BadRequest("Invalid amenity or time for booking.");
            }
            var duration = (dto.EndTime.Value.ToTimeSpan() - dto.StartTime.Value.ToTimeSpan()).TotalHours;
            if (duration <= 0) return BadRequest("End time must be after start time.");
            dto.Amount = Math.Round(duration * amenity.HourlyRate.Value, 2);

            var booking = _mapper.Map<Booking>(dto);
            _context.Bookings.Add(booking);
            _context.SaveChanges();

            var bookingDto = _mapper.Map<BookingDto>(booking);
            _eventBroker.Publish(new RealtimeEvent
            {
                Type = EventTypes.BookingCreated,
                UserIds = booking.UserId is int userId ? new[] { userId } : Array.Empty<int>(),
                FlatIds = booking.FlatId is int flatId ? new[] { flatId } : Array.Empty<int>(),
                SocietyId = ResolveSocietyId(booking.FlatId, booking.UserId),
                Roles = new[] { "admin" },
                Data = bookingDto
            });

            // Keep the existing email notification for the resident.
            if (booking.UserId != null)
            {
                var user = _context.UserAccounts.FirstOrDefault(u => u.UserId == booking.UserId);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    await _notificationService.SendNotificationAsync(
                        user.Email,
                        "Booking Created",
                        $"A new booking has been created for you. BookingId: {booking.BookingId}"
                    );
                }
            }
            return CreatedAtAction(nameof(Get), new { id = booking.BookingId }, bookingDto);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "super_admin,admin,security_staff,resident")]
        public IActionResult Update(int id, [FromBody] UpdateBookingDto dto)
        {
            var existing = _context.Bookings.Find(id);
            if (existing == null) return NotFound();

            // Always update status if present in DTO (even if empty string)
            if (dto.Status != null)
                existing.Status = dto.Status;
            if (dto.Paid.HasValue)
                existing.Paid = dto.Paid;
            if (dto.TransactionId != null)
                existing.TransactionId = dto.TransactionId;
            if (dto.UserId.HasValue)
                existing.UserId = dto.UserId;
            if (dto.FlatId.HasValue)
                existing.FlatId = dto.FlatId;
            if (dto.AmenityId.HasValue)
                existing.AmenityId = dto.AmenityId;
            if (dto.CreatedAt.HasValue)
                existing.CreatedAt = dto.CreatedAt;
            if (dto.StartDate.HasValue)
                existing.StartDate = dto.StartDate;
            if (dto.EndDate.HasValue)
                existing.EndDate = dto.EndDate;
            if (dto.StartTime.HasValue)
                existing.StartTime = dto.StartTime;
            if (dto.EndTime.HasValue)
                existing.EndTime = dto.EndTime;
            if (dto.Amount.HasValue)
                existing.Amount = dto.Amount;

            _context.SaveChanges();
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "super_admin,admin,resident")]
        public IActionResult Delete(int id)
        {
            var booking = _context.Bookings.Find(id);
            if (booking == null) return NotFound();
            _context.Bookings.Remove(booking);
            _context.SaveChanges();
            return NoContent();
        }

        private int? ResolveSocietyId(int? flatId, int? userId)
        {
            if (flatId is int id)
            {
                var societyId = _context.Flats
                    .Where(f => f.FlatId == id)
                    .Select(f => f.Wing != null ? f.Wing.SocietyId : (int?)null)
                    .FirstOrDefault();

                if (societyId.HasValue)
                {
                    return societyId;
                }
            }

            if (userId is int accountId)
            {
                return _context.UserAccounts
                    .Where(u => u.UserId == accountId)
                    .Select(u => u.SocietyId)
                    .FirstOrDefault();
            }

            return null;
        }
    }
}