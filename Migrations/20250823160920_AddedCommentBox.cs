using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMEHCRM.Migrations
{
    /// <inheritdoc />
    public partial class AddedCommentBox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDateTime",
                table: "TicketManagementRecords",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "TicketManagementRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SrOrTicket",
                table: "TicketManagementRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Channel",
                table: "CRMFormatRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "CRMFormatRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDateTime",
                table: "CRMFormatRecords",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Comment",
                table: "TicketManagementRecords");

            migrationBuilder.DropColumn(
                name: "SrOrTicket",
                table: "TicketManagementRecords");

            migrationBuilder.DropColumn(
                name: "Channel",
                table: "CRMFormatRecords");

            migrationBuilder.DropColumn(
                name: "Comment",
                table: "CRMFormatRecords");

            migrationBuilder.DropColumn(
                name: "CreatedDateTime",
                table: "CRMFormatRecords");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDateTime",
                table: "TicketManagementRecords",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}
