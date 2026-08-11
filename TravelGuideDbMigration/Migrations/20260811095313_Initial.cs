using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelGuideDbMigration.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryId);
                });

            migrationBuilder.CreateTable(
                name: "FromPoints",
                columns: table => new
                {
                    FromPointId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FromPoints", x => x.FromPointId);
                });

            migrationBuilder.CreateTable(
                name: "Months",
                columns: table => new
                {
                    MonthId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Months", x => x.MonthId);
                });

            migrationBuilder.CreateTable(
                name: "Motorcycles",
                columns: table => new
                {
                    MotorcycleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MotorcycleKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Manufacturer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReleaseYear = table.Column<int>(type: "int", nullable: false),
                    StateNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Motorcycles", x => x.MotorcycleId);
                });

            migrationBuilder.CreateTable(
                name: "Municipalities",
                columns: table => new
                {
                    MunicipalityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Municipalities", x => x.MunicipalityId);
                });

            migrationBuilder.CreateTable(
                name: "Regions",
                columns: table => new
                {
                    RegionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Regions", x => x.RegionId);
                });

            migrationBuilder.CreateTable(
                name: "RouteDistances",
                columns: table => new
                {
                    RouteDistanceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StartLatitude = table.Column<double>(type: "float", nullable: false),
                    StartLongitude = table.Column<double>(type: "float", nullable: false),
                    EndLatitude = table.Column<double>(type: "float", nullable: false),
                    EndLongitude = table.Column<double>(type: "float", nullable: false),
                    AirDistance = table.Column<double>(type: "float", nullable: false),
                    RoadDistance = table.Column<double>(type: "float", nullable: false),
                    RoadTime = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteDistances", x => x.RouteDistanceId);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    TagId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.TagId);
                });

            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    TaskId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.TaskId);
                });

            migrationBuilder.CreateTable(
                name: "Places",
                columns: table => new
                {
                    PlaceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    RegionId = table.Column<int>(type: "int", nullable: true),
                    MunicipalityId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    State = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Places", x => x.PlaceId);
                    table.ForeignKey(
                        name: "FK_Places_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "MunicipalityId");
                    table.ForeignKey(
                        name: "FK_Places_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "RegionId");
                });

            migrationBuilder.CreateTable(
                name: "TaskStartPoints",
                columns: table => new
                {
                    TspId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<int>(type: "int", nullable: false),
                    StartPoint = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskStartPoints", x => x.TspId);
                    table.ForeignKey(
                        name: "FK_TaskStartPoints_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "TaskId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DistanceByPlaces",
                columns: table => new
                {
                    DistanceByPlaceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlaceId = table.Column<int>(type: "int", nullable: false),
                    FromPointId = table.Column<int>(type: "int", nullable: false),
                    Distance = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DistanceByPlaces", x => x.DistanceByPlaceId);
                    table.ForeignKey(
                        name: "FK_DistanceByPlaces_FromPoints_FromPointId",
                        column: x => x.FromPointId,
                        principalTable: "FromPoints",
                        principalColumn: "FromPointId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DistanceByPlaces_Places_PlaceId",
                        column: x => x.PlaceId,
                        principalTable: "Places",
                        principalColumn: "PlaceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlacesByBestSeasons",
                columns: table => new
                {
                    PlaceId = table.Column<int>(type: "int", nullable: false),
                    MonthId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlacesByBestSeasons", x => new { x.PlaceId, x.MonthId });
                    table.ForeignKey(
                        name: "FK_PlacesByBestSeasons_Months_MonthId",
                        column: x => x.MonthId,
                        principalTable: "Months",
                        principalColumn: "MonthId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlacesByBestSeasons_Places_PlaceId",
                        column: x => x.PlaceId,
                        principalTable: "Places",
                        principalColumn: "PlaceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlacesByCategories",
                columns: table => new
                {
                    PlaceId = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlacesByCategories", x => new { x.PlaceId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_PlacesByCategories_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlacesByCategories_Places_PlaceId",
                        column: x => x.PlaceId,
                        principalTable: "Places",
                        principalColumn: "PlaceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlacesByTags",
                columns: table => new
                {
                    PlaceId = table.Column<int>(type: "int", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlacesByTags", x => new { x.PlaceId, x.TagId });
                    table.ForeignKey(
                        name: "FK_PlacesByTags_Places_PlaceId",
                        column: x => x.PlaceId,
                        principalTable: "Places",
                        principalColumn: "PlaceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlacesByTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UrlGraphNodes",
                columns: table => new
                {
                    UgnId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FromUrlId = table.Column<int>(type: "int", nullable: false),
                    GotUrlId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UrlGraphNodes", x => x.UgnId);
                    table.ForeignKey(
                        name: "FK_UrlGraphNodes_Places_FromUrlId",
                        column: x => x.FromUrlId,
                        principalTable: "Places",
                        principalColumn: "PlaceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UrlGraphNodes_Places_GotUrlId",
                        column: x => x.GotUrlId,
                        principalTable: "Places",
                        principalColumn: "PlaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Visits",
                columns: table => new
                {
                    VisitId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlaceId = table.Column<int>(type: "int", nullable: false),
                    MotorcycleId = table.Column<int>(type: "int", nullable: false),
                    VisitDate = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visits", x => x.VisitId);
                    table.ForeignKey(
                        name: "FK_Visits_Motorcycles_MotorcycleId",
                        column: x => x.MotorcycleId,
                        principalTable: "Motorcycles",
                        principalColumn: "MotorcycleId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Visits_Places_PlaceId",
                        column: x => x.PlaceId,
                        principalTable: "Places",
                        principalColumn: "PlaceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DistanceByPlaces_FromPointId",
                table: "DistanceByPlaces",
                column: "FromPointId");

            migrationBuilder.CreateIndex(
                name: "IX_DistanceByPlaces_PlaceId_FromPointId",
                table: "DistanceByPlaces",
                columns: new[] { "PlaceId", "FromPointId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FromPoints_Name",
                table: "FromPoints",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Months_Name",
                table: "Months",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Motorcycles_MotorcycleKey",
                table: "Motorcycles",
                column: "MotorcycleKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Municipalities_Name",
                table: "Municipalities",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Places_MunicipalityId",
                table: "Places",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_Places_RegionId",
                table: "Places",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_Places_Url",
                table: "Places",
                column: "Url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlacesByBestSeasons_MonthId",
                table: "PlacesByBestSeasons",
                column: "MonthId");

            migrationBuilder.CreateIndex(
                name: "IX_PlacesByCategories_CategoryId",
                table: "PlacesByCategories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PlacesByTags_TagId",
                table: "PlacesByTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_Regions_Name",
                table: "Regions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RouteDistances_StartLatitude_StartLongitude_EndLatitude_EndLongitude",
                table: "RouteDistances",
                columns: new[] { "StartLatitude", "StartLongitude", "EndLatitude", "EndLongitude" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name",
                table: "Tags",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_TaskName",
                table: "Tasks",
                column: "TaskName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskStartPoints_TaskId",
                table: "TaskStartPoints",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_UrlGraphNodes_FromUrlId_GotUrlId",
                table: "UrlGraphNodes",
                columns: new[] { "FromUrlId", "GotUrlId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UrlGraphNodes_GotUrlId",
                table: "UrlGraphNodes",
                column: "GotUrlId");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_MotorcycleId",
                table: "Visits",
                column: "MotorcycleId");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_PlaceId",
                table: "Visits",
                column: "PlaceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DistanceByPlaces");

            migrationBuilder.DropTable(
                name: "PlacesByBestSeasons");

            migrationBuilder.DropTable(
                name: "PlacesByCategories");

            migrationBuilder.DropTable(
                name: "PlacesByTags");

            migrationBuilder.DropTable(
                name: "RouteDistances");

            migrationBuilder.DropTable(
                name: "TaskStartPoints");

            migrationBuilder.DropTable(
                name: "UrlGraphNodes");

            migrationBuilder.DropTable(
                name: "Visits");

            migrationBuilder.DropTable(
                name: "FromPoints");

            migrationBuilder.DropTable(
                name: "Months");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "Tasks");

            migrationBuilder.DropTable(
                name: "Motorcycles");

            migrationBuilder.DropTable(
                name: "Places");

            migrationBuilder.DropTable(
                name: "Municipalities");

            migrationBuilder.DropTable(
                name: "Regions");
        }
    }
}
