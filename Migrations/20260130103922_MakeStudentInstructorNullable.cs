using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS.Migrations
{
    /// <inheritdoc />
    public partial class MakeStudentInstructorNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_Instructors_Instructorid",
                table: "Students");

            migrationBuilder.AlterColumn<int>(
                name: "Instructorid",
                table: "Students",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Instructors_Instructorid",
                table: "Students",
                column: "Instructorid",
                principalTable: "Instructors",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_Instructors_Instructorid",
                table: "Students");

            migrationBuilder.AlterColumn<int>(
                name: "Instructorid",
                table: "Students",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Instructors_Instructorid",
                table: "Students",
                column: "Instructorid",
                principalTable: "Instructors",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
