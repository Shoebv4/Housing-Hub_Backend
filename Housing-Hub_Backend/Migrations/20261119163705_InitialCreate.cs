using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HousingHubBackend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Society",
                columns: table => new
                {
                    SocietyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    State = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    City = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Pincode = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Society", x => x.SocietyId);
                });

            migrationBuilder.CreateTable(
                name: "Amenity",
                columns: table => new
                {
                    AmenityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SocietyId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    HourlyRate = table.Column<double>(type: "float", nullable: true),
                    MaxCapacity = table.Column<int>(type: "int", nullable: true),
                    OpeningTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    ClosingTime = table.Column<TimeOnly>(type: "time", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Amenity", x => x.AmenityId);
                    table.ForeignKey(
                        name: "FK_Amenity_Society_SocietyId",
                        column: x => x.SocietyId,
                        principalTable: "Society",
                        principalColumn: "SocietyId");
                });

            migrationBuilder.CreateTable(
                name: "Wing",
                columns: table => new
                {
                    WingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TotalFloors = table.Column<int>(type: "int", nullable: true),
                    FlatsPerFloor = table.Column<int>(type: "int", nullable: true),
                    SocietyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wing", x => x.WingId);
                    table.ForeignKey(
                        name: "FK_Wing_Society_SocietyId",
                        column: x => x.SocietyId,
                        principalTable: "Society",
                        principalColumn: "SocietyId");
                });

            migrationBuilder.CreateTable(
                name: "Flat",
                columns: table => new
                {
                    FlatId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WingId = table.Column<int>(type: "int", nullable: true),
                    FlatNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FloorNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Area = table.Column<double>(type: "float", nullable: true),
                    Status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Flat", x => x.FlatId);
                    table.ForeignKey(
                        name: "FK_Flat_Wing_WingId",
                        column: x => x.WingId,
                        principalTable: "Wing",
                        principalColumn: "WingId");
                });

            migrationBuilder.CreateTable(
                name: "UserAccount",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Password = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Role = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    SocietyId = table.Column<int>(type: "int", nullable: true),
                    FlatId = table.Column<int>(type: "int", nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccount", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserAccount_Flat_FlatId",
                        column: x => x.FlatId,
                        principalTable: "Flat",
                        principalColumn: "FlatId");
                    table.ForeignKey(
                        name: "FK_UserAccount_Society_SocietyId",
                        column: x => x.SocietyId,
                        principalTable: "Society",
                        principalColumn: "SocietyId");
                });

            migrationBuilder.CreateTable(
                name: "Announcement",
                columns: table => new
                {
                    AId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    SocietyId = table.Column<int>(type: "int", nullable: true),
                    isGlobal = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Announcement", x => x.AId);
                    table.ForeignKey(
                        name: "FK_Announcement_Society_SocietyId",
                        column: x => x.SocietyId,
                        principalTable: "Society",
                        principalColumn: "SocietyId");
                    table.ForeignKey(
                        name: "FK_Announcement_UserAccount_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserAccount",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Booking",
                columns: table => new
                {
                    BookingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    FlatId = table.Column<int>(type: "int", nullable: true),
                    AmenityId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateOnly>(type: "date", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    Amount = table.Column<double>(type: "float", nullable: true),
                    Paid = table.Column<bool>(type: "bit", nullable: true),
                    TransactionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Booking", x => x.BookingId);
                    table.ForeignKey(
                        name: "FK_Booking_Amenity_AmenityId",
                        column: x => x.AmenityId,
                        principalTable: "Amenity",
                        principalColumn: "AmenityId");
                    table.ForeignKey(
                        name: "FK_Booking_Flat_FlatId",
                        column: x => x.FlatId,
                        principalTable: "Flat",
                        principalColumn: "FlatId");
                    table.ForeignKey(
                        name: "FK_Booking_UserAccount_UserId",
                        column: x => x.UserId,
                        principalTable: "UserAccount",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Complaint",
                columns: table => new
                {
                    ComplaintId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FlatId = table.Column<int>(type: "int", nullable: true),
                    RaisedBy = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Complaint", x => x.ComplaintId);
                    table.ForeignKey(
                        name: "FK_Complaint_Flat_FlatId",
                        column: x => x.FlatId,
                        principalTable: "Flat",
                        principalColumn: "FlatId");
                    table.ForeignKey(
                        name: "FK_Complaint_UserAccount_RaisedBy",
                        column: x => x.RaisedBy,
                        principalTable: "UserAccount",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceFee",
                columns: table => new
                {
                    MFId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WingId = table.Column<int>(type: "int", nullable: true),
                    RatePerSqft = table.Column<double>(type: "float", nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceFee", x => x.MFId);
                    table.ForeignKey(
                        name: "FK_MaintenanceFee_UserAccount_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserAccount",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_MaintenanceFee_Wing_WingId",
                        column: x => x.WingId,
                        principalTable: "Wing",
                        principalColumn: "WingId");
                });

            migrationBuilder.CreateTable(
                name: "Visitor",
                columns: table => new
                {
                    VisitorId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FlatId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VisitorType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    VehicleNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Purpose = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntryTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    ExitTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    RecordedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visitor", x => x.VisitorId);
                    table.ForeignKey(
                        name: "FK_Visitor_Flat_FlatId",
                        column: x => x.FlatId,
                        principalTable: "Flat",
                        principalColumn: "FlatId");
                    table.ForeignKey(
                        name: "FK_Visitor_UserAccount_RecordedBy",
                        column: x => x.RecordedBy,
                        principalTable: "UserAccount",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "AnnouncementTargetFlat",
                columns: table => new
                {
                    ATFId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnnouncementId = table.Column<int>(type: "int", nullable: true),
                    FlatId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnnouncementTargetFlat", x => x.ATFId);
                    table.ForeignKey(
                        name: "FK_AnnouncementTargetFlat_Announcement_AnnouncementId",
                        column: x => x.AnnouncementId,
                        principalTable: "Announcement",
                        principalColumn: "AId");
                    table.ForeignKey(
                        name: "FK_AnnouncementTargetFlat_Flat_FlatId",
                        column: x => x.FlatId,
                        principalTable: "Flat",
                        principalColumn: "FlatId");
                });

            migrationBuilder.CreateTable(
                name: "AnnouncementTargetWing",
                columns: table => new
                {
                    ATWId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnnouncementId = table.Column<int>(type: "int", nullable: true),
                    WingId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnnouncementTargetWing", x => x.ATWId);
                    table.ForeignKey(
                        name: "FK_AnnouncementTargetWing_Announcement_AnnouncementId",
                        column: x => x.AnnouncementId,
                        principalTable: "Announcement",
                        principalColumn: "AId");
                    table.ForeignKey(
                        name: "FK_AnnouncementTargetWing_Wing_WingId",
                        column: x => x.WingId,
                        principalTable: "Wing",
                        principalColumn: "WingId");
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceBill",
                columns: table => new
                {
                    MBId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FlatId = table.Column<int>(type: "int", nullable: true),
                    Mfid = table.Column<int>(type: "int", nullable: true),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: true),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: true),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Amount = table.Column<double>(type: "float", nullable: true),
                    PaidDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Paid = table.Column<bool>(type: "bit", nullable: true),
                    TransactionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceBill", x => x.MBId);
                    table.ForeignKey(
                        name: "FK_MaintenanceBill_Flat_FlatId",
                        column: x => x.FlatId,
                        principalTable: "Flat",
                        principalColumn: "FlatId");
                    table.ForeignKey(
                        name: "FK_MaintenanceBill_MaintenanceFee_Mfid",
                        column: x => x.Mfid,
                        principalTable: "MaintenanceFee",
                        principalColumn: "MFId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Amenity_SocietyId",
                table: "Amenity",
                column: "SocietyId");

            migrationBuilder.CreateIndex(
                name: "IX_Announcement_CreatedBy",
                table: "Announcement",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Announcement_SocietyId",
                table: "Announcement",
                column: "SocietyId");

            migrationBuilder.CreateIndex(
                name: "IX_AnnouncementTargetFlat_AnnouncementId",
                table: "AnnouncementTargetFlat",
                column: "AnnouncementId");

            migrationBuilder.CreateIndex(
                name: "IX_AnnouncementTargetFlat_FlatId",
                table: "AnnouncementTargetFlat",
                column: "FlatId");

            migrationBuilder.CreateIndex(
                name: "IX_AnnouncementTargetWing_AnnouncementId",
                table: "AnnouncementTargetWing",
                column: "AnnouncementId");

            migrationBuilder.CreateIndex(
                name: "IX_AnnouncementTargetWing_WingId",
                table: "AnnouncementTargetWing",
                column: "WingId");

            migrationBuilder.CreateIndex(
                name: "IX_Booking_AmenityId",
                table: "Booking",
                column: "AmenityId");

            migrationBuilder.CreateIndex(
                name: "IX_Booking_FlatId",
                table: "Booking",
                column: "FlatId");

            migrationBuilder.CreateIndex(
                name: "IX_Booking_Status",
                table: "Booking",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Booking_UserId",
                table: "Booking",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Complaint_FlatId",
                table: "Complaint",
                column: "FlatId");

            migrationBuilder.CreateIndex(
                name: "IX_Complaint_RaisedBy",
                table: "Complaint",
                column: "RaisedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Complaint_Status",
                table: "Complaint",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Flat_WingId",
                table: "Flat",
                column: "WingId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceBill_FlatId",
                table: "MaintenanceBill",
                column: "FlatId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceBill_Mfid",
                table: "MaintenanceBill",
                column: "Mfid");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceFee_CreatedBy",
                table: "MaintenanceFee",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceFee_WingId",
                table: "MaintenanceFee",
                column: "WingId");

            migrationBuilder.CreateIndex(
                name: "IX_User_Role",
                table: "UserAccount",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_User_SocietyId",
                table: "UserAccount",
                column: "SocietyId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccount_FlatId",
                table: "UserAccount",
                column: "FlatId");

            migrationBuilder.CreateIndex(
                name: "UQ__UserAcco__A9D10534881C3379",
                table: "UserAccount",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Visitor_FlatId",
                table: "Visitor",
                column: "FlatId");

            migrationBuilder.CreateIndex(
                name: "IX_Visitor_RecordedBy",
                table: "Visitor",
                column: "RecordedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Wing_SocietyId",
                table: "Wing",
                column: "SocietyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnnouncementTargetFlat");

            migrationBuilder.DropTable(
                name: "AnnouncementTargetWing");

            migrationBuilder.DropTable(
                name: "Booking");

            migrationBuilder.DropTable(
                name: "Complaint");

            migrationBuilder.DropTable(
                name: "MaintenanceBill");

            migrationBuilder.DropTable(
                name: "Visitor");

            migrationBuilder.DropTable(
                name: "Announcement");

            migrationBuilder.DropTable(
                name: "Amenity");

            migrationBuilder.DropTable(
                name: "MaintenanceFee");

            migrationBuilder.DropTable(
                name: "UserAccount");

            migrationBuilder.DropTable(
                name: "Flat");

            migrationBuilder.DropTable(
                name: "Wing");

            migrationBuilder.DropTable(
                name: "Society");
        }
    }
}
