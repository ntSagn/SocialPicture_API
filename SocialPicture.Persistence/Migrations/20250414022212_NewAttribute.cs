using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialPicture.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NewAttribute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FollowersCount",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FollowingCount",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PostsCount",
                table: "Users",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FollowersCount",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FollowingCount",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PostsCount",
                table: "Users");
        }
    }
}
