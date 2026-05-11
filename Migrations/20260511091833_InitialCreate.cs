using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetSentinel.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Alerts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SourceIp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DestinationIp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsAcknowledged = table.Column<bool>(type: "bit", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MacAddress = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Unknown"),
                    Hostname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "Unknown"),
                    IsOnline = table.Column<bool>(type: "bit", nullable: false),
                    IsKnown = table.Column<bool>(type: "bit", nullable: false),
                    DeviceLabel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FirstSeen = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LastSeen = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LastOffline = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LogEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Level = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "INFO"),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScanResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScanTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    DevicesFound = table.Column<int>(type: "int", nullable: false),
                    OnlineCount = table.Column<int>(type: "int", nullable: false),
                    NewDevicesFound = table.Column<int>(type: "int", nullable: false),
                    DurationMs = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScanResults", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrafficRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceIp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DestinationIp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourcePort = table.Column<int>(type: "int", nullable: false),
                    DestinationPort = table.Column<int>(type: "int", nullable: false),
                    Protocol = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    BytesSent = table.Column<long>(type: "bigint", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Details = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrafficRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_IsAcknowledged",
                table: "Alerts",
                column: "IsAcknowledged");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_SourceIp_Type_IsAcknowledged",
                table: "Alerts",
                columns: new[] { "SourceIp", "Type", "IsAcknowledged" });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_Timestamp",
                table: "Alerts",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_IpAddress",
                table: "Devices",
                column: "IpAddress",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devices_IsOnline",
                table: "Devices",
                column: "IsOnline");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_MacAddress",
                table: "Devices",
                column: "MacAddress");

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_Level",
                table: "LogEntries",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_Timestamp",
                table: "LogEntries",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ScanResults_ScanTime",
                table: "ScanResults",
                column: "ScanTime");

            migrationBuilder.CreateIndex(
                name: "IX_TrafficRecords_Protocol",
                table: "TrafficRecords",
                column: "Protocol");

            migrationBuilder.CreateIndex(
                name: "IX_TrafficRecords_SourceIp",
                table: "TrafficRecords",
                column: "SourceIp");

            migrationBuilder.CreateIndex(
                name: "IX_TrafficRecords_Timestamp",
                table: "TrafficRecords",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alerts");

            migrationBuilder.DropTable(
                name: "Devices");

            migrationBuilder.DropTable(
                name: "LogEntries");

            migrationBuilder.DropTable(
                name: "ScanResults");

            migrationBuilder.DropTable(
                name: "TrafficRecords");
        }
    }
}
