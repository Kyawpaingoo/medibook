using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbDoctors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Full_Name = table.Column<string>(type: "text", nullable: false),
                    Specialization = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Phone_Number = table.Column<string>(type: "text", nullable: true),
                    Is_Active = table.Column<bool>(type: "boolean", nullable: false),
                    Created_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbDoctors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbPatients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Full_Name = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Phone_Number = table.Column<string>(type: "text", nullable: true),
                    Date_Of_Birth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Created_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbPatients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbSlots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Doctor_Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Start_Time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    End_Time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Created_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbSlots", x => x.Id);
                    table.CheckConstraint("CK_Slots_Status", "\"Status\" IN ('Available','Reserved','Confirmed','Cancelled')");
                    table.ForeignKey(
                        name: "FK_tbSlots_tbDoctors_Doctor_Id",
                        column: x => x.Doctor_Id,
                        principalTable: "tbDoctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tbUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Password = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Doctor_Id = table.Column<Guid>(type: "uuid", nullable: true),
                    Refresh_Token = table.Column<string>(type: "text", nullable: true),
                    Refresh_Token_Expiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Created_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbUsers", x => x.Id);
                    table.CheckConstraint("CK_Users_Role", "\"Role\" IN ('Admin','Doctor','Staff')");
                    table.ForeignKey(
                        name: "FK_tbUsers_tbDoctors_Doctor_Id",
                        column: x => x.Doctor_Id,
                        principalTable: "tbDoctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tbAppointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slot_Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Doctor_Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Patient_Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Created_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbAppointments", x => x.Id);
                    table.CheckConstraint("CK_Appointments_Status", "\"Status\" IN ('Reserved','Confirmed','Cancelled')");
                    table.ForeignKey(
                        name: "FK_tbAppointments_tbDoctors_Doctor_Id",
                        column: x => x.Doctor_Id,
                        principalTable: "tbDoctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbAppointments_tbPatients_Patient_Id",
                        column: x => x.Patient_Id,
                        principalTable: "tbPatients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbAppointments_tbSlots_Slot_Id",
                        column: x => x.Slot_Id,
                        principalTable: "tbSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tbAppointment_Status_History",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Appointment_Id = table.Column<Guid>(type: "uuid", nullable: false),
                    From_Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    To_Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Changed_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Changed_By_User_Id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbAppointment_Status_History", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbAppointment_Status_History_tbAppointments_Appointment_Id",
                        column: x => x.Appointment_Id,
                        principalTable: "tbAppointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbAppointment_Status_History_tbUsers_Changed_By_User_Id",
                        column: x => x.Changed_By_User_Id,
                        principalTable: "tbUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbAppointment_Status_History_Appointment_Id",
                table: "tbAppointment_Status_History",
                column: "Appointment_Id");

            migrationBuilder.CreateIndex(
                name: "IX_tbAppointment_Status_History_Changed_By_User_Id",
                table: "tbAppointment_Status_History",
                column: "Changed_By_User_Id");

            migrationBuilder.CreateIndex(
                name: "IX_tbAppointments_Doctor_Id",
                table: "tbAppointments",
                column: "Doctor_Id");

            migrationBuilder.CreateIndex(
                name: "IX_tbAppointments_Patient_Id",
                table: "tbAppointments",
                column: "Patient_Id");

            migrationBuilder.CreateIndex(
                name: "uq_active_appointment_per_slot",
                table: "tbAppointments",
                column: "Slot_Id",
                unique: true,
                filter: "\"Status\" IN ('Reserved','Confirmed')");

            migrationBuilder.CreateIndex(
                name: "IX_tbDoctors_Email",
                table: "tbDoctors",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbPatients_Email",
                table: "tbPatients",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbSlots_Doctor_Id_Start_Time",
                table: "tbSlots",
                columns: new[] { "Doctor_Id", "Start_Time" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbSlots_Doctor_Id_Status_Start_Time",
                table: "tbSlots",
                columns: new[] { "Doctor_Id", "Status", "Start_Time" });

            migrationBuilder.CreateIndex(
                name: "IX_tbUsers_Doctor_Id",
                table: "tbUsers",
                column: "Doctor_Id");

            migrationBuilder.CreateIndex(
                name: "IX_tbUsers_Email",
                table: "tbUsers",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbAppointment_Status_History");

            migrationBuilder.DropTable(
                name: "tbAppointments");

            migrationBuilder.DropTable(
                name: "tbUsers");

            migrationBuilder.DropTable(
                name: "tbPatients");

            migrationBuilder.DropTable(
                name: "tbSlots");

            migrationBuilder.DropTable(
                name: "tbDoctors");
        }
    }
}
