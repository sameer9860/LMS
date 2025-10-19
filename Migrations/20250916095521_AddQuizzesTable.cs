using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizzesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MCQs_Quiz_QuizId",
                table: "MCQs");

            migrationBuilder.DropForeignKey(
                name: "FK_Quiz_Courses_CourseId",
                table: "Quiz");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Quiz",
                table: "Quiz");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MCQs",
                table: "MCQs");

            migrationBuilder.RenameTable(
                name: "Quiz",
                newName: "Quizzes");

            migrationBuilder.RenameTable(
                name: "MCQs",
                newName: "MCQ");

            migrationBuilder.RenameIndex(
                name: "IX_Quiz_CourseId",
                table: "Quizzes",
                newName: "IX_Quizzes_CourseId");

            migrationBuilder.RenameIndex(
                name: "IX_MCQs_QuizId",
                table: "MCQ",
                newName: "IX_MCQ_QuizId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Quizzes",
                table: "Quizzes",
                column: "Id");

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
                name: "FK_Quizzes_Courses_CourseId",
                table: "Quizzes",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MCQ_Quizzes_QuizId",
                table: "MCQ");

            migrationBuilder.DropForeignKey(
                name: "FK_Quizzes_Courses_CourseId",
                table: "Quizzes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Quizzes",
                table: "Quizzes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MCQ",
                table: "MCQ");

            migrationBuilder.RenameTable(
                name: "Quizzes",
                newName: "Quiz");

            migrationBuilder.RenameTable(
                name: "MCQ",
                newName: "MCQs");

            migrationBuilder.RenameIndex(
                name: "IX_Quizzes_CourseId",
                table: "Quiz",
                newName: "IX_Quiz_CourseId");

            migrationBuilder.RenameIndex(
                name: "IX_MCQ_QuizId",
                table: "MCQs",
                newName: "IX_MCQs_QuizId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Quiz",
                table: "Quiz",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MCQs",
                table: "MCQs",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MCQs_Quiz_QuizId",
                table: "MCQs",
                column: "QuizId",
                principalTable: "Quiz",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Quiz_Courses_CourseId",
                table: "Quiz",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
