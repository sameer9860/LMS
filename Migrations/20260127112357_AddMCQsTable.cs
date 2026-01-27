using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS.Migrations
{
    /// <inheritdoc />
    public partial class AddMCQsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MCQ_Quizzes_QuizId",
                table: "MCQ");

            migrationBuilder.DropForeignKey(
                name: "FK_QuizAnswers_MCQ_MCQId",
                table: "QuizAnswers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MCQ",
                table: "MCQ");

            migrationBuilder.RenameTable(
                name: "MCQ",
                newName: "MCQs");

            migrationBuilder.RenameIndex(
                name: "IX_MCQ_QuizId",
                table: "MCQs",
                newName: "IX_MCQs_QuizId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MCQs",
                table: "MCQs",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MCQs_Quizzes_QuizId",
                table: "MCQs",
                column: "QuizId",
                principalTable: "Quizzes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QuizAnswers_MCQs_MCQId",
                table: "QuizAnswers",
                column: "MCQId",
                principalTable: "MCQs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MCQs_Quizzes_QuizId",
                table: "MCQs");

            migrationBuilder.DropForeignKey(
                name: "FK_QuizAnswers_MCQs_MCQId",
                table: "QuizAnswers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MCQs",
                table: "MCQs");

            migrationBuilder.RenameTable(
                name: "MCQs",
                newName: "MCQ");

            migrationBuilder.RenameIndex(
                name: "IX_MCQs_QuizId",
                table: "MCQ",
                newName: "IX_MCQ_QuizId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MCQ",
                table: "MCQ",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MCQ_Quizzes_QuizId",
                table: "MCQ",
                column: "QuizId",
                principalTable: "Quizzes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QuizAnswers_MCQ_MCQId",
                table: "QuizAnswers",
                column: "MCQId",
                principalTable: "MCQ",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
