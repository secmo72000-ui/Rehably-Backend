using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rehably.Infrastructure.Migrations.ApplicationDb
{
    /// <inheritdoc />
    public partial class AddClinicalAssessmentSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClinicAssessmentFieldConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepNumber = table.Column<int>(type: "integer", nullable: false),
                    FieldKey = table.Column<string>(type: "text", nullable: false),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicAssessmentFieldConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Specialities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NameEn = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NameAr = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IcdChapters = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IconUrl = table.Column<string>(type: "text", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Specialities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Diagnoses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uuid", nullable: true),
                    SpecialityId = table.Column<Guid>(type: "uuid", nullable: false),
                    BodyRegionCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    IcdCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NameEn = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    NameAr = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultProtocolName = table.Column<string>(type: "text", nullable: true),
                    DefaultExerciseIds = table.Column<string>(type: "text", nullable: true),
                    SuggestedSessions = table.Column<int>(type: "integer", nullable: true),
                    SuggestedDurationWeeks = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Diagnoses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Diagnoses_BodyRegionCategories_BodyRegionCategoryId",
                        column: x => x.BodyRegionCategoryId,
                        principalTable: "BodyRegionCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Diagnoses_Specialities_SpecialityId",
                        column: x => x.SpecialityId,
                        principalTable: "Specialities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PatientAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    TherapistId = table.Column<string>(type: "text", nullable: true),
                    TherapistName = table.Column<string>(type: "text", nullable: true),
                    SpecialityId = table.Column<Guid>(type: "uuid", nullable: false),
                    BodyRegionCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    DiagnosisId = table.Column<Guid>(type: "uuid", nullable: true),
                    DiagnosisFreeText = table.Column<string>(type: "text", nullable: true),
                    PatientAge = table.Column<int>(type: "integer", nullable: true),
                    Gender = table.Column<string>(type: "text", nullable: true),
                    HasPostOp = table.Column<bool>(type: "boolean", nullable: false),
                    AttachmentUrls = table.Column<string>(type: "text", nullable: true),
                    AdditionalNotes = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientAssessments_Diagnoses_DiagnosisId",
                        column: x => x.DiagnosisId,
                        principalTable: "Diagnoses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PatientAssessments_Specialities_SpecialityId",
                        column: x => x.SpecialityId,
                        principalTable: "Specialities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentClinicalReasonings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProblemList = table.Column<string>(type: "text", nullable: true),
                    WorkingHypothesis = table.Column<string>(type: "text", nullable: true),
                    SeverityIrritability = table.Column<string>(type: "text", nullable: true),
                    DifferentialConsiderations = table.Column<string>(type: "text", nullable: true),
                    DecisionPoints = table.Column<string>(type: "text", nullable: true),
                    ImagingRequested = table.Column<bool>(type: "boolean", nullable: true),
                    ImagingReason = table.Column<string>(type: "text", nullable: true),
                    ReferralRequired = table.Column<bool>(type: "boolean", nullable: true),
                    ReferralTo = table.Column<string>(type: "text", nullable: true),
                    Urgency = table.Column<string>(type: "text", nullable: true),
                    BreakGlassUsed = table.Column<bool>(type: "boolean", nullable: true),
                    BreakGlassReason = table.Column<string>(type: "text", nullable: true),
                    AdditionalNotes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentClinicalReasonings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentClinicalReasonings_PatientAssessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "PatientAssessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentNeuros",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sensation = table.Column<string>(type: "text", nullable: true),
                    Numbness = table.Column<string>(type: "text", nullable: true),
                    Tingling = table.Column<string>(type: "text", nullable: true),
                    Myotomes = table.Column<string>(type: "text", nullable: true),
                    KeyMuscleWeakness = table.Column<string>(type: "text", nullable: true),
                    Reflexes = table.Column<string>(type: "text", nullable: true),
                    NeurovascularChecks = table.Column<string>(type: "text", nullable: true),
                    SpecialTests = table.Column<string>(type: "text", nullable: true),
                    AdditionalNotes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentNeuros", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentNeuros_PatientAssessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "PatientAssessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentObjectives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Posture = table.Column<string>(type: "text", nullable: true),
                    Swelling = table.Column<string>(type: "text", nullable: true),
                    Redness = table.Column<string>(type: "text", nullable: true),
                    Deformity = table.Column<string>(type: "text", nullable: true),
                    Gait = table.Column<string>(type: "text", nullable: true),
                    Transfers = table.Column<string>(type: "text", nullable: true),
                    AssistiveDevices = table.Column<string>(type: "text", nullable: true),
                    FunctionalTests = table.Column<string>(type: "text", nullable: true),
                    StrengthData = table.Column<string>(type: "text", nullable: true),
                    RomData = table.Column<string>(type: "text", nullable: true),
                    AdditionalNotes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentObjectives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentObjectives_PatientAssessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "PatientAssessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentPostOps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcedureName = table.Column<string>(type: "text", nullable: true),
                    ProcedureSide = table.Column<string>(type: "text", nullable: true),
                    SurgeryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DaysPostOp = table.Column<int>(type: "integer", nullable: true),
                    SurgeonFacility = table.Column<string>(type: "text", nullable: true),
                    WeightBearingStatus = table.Column<string>(type: "text", nullable: true),
                    RomRestriction = table.Column<string>(type: "text", nullable: true),
                    PostOpPrecautions = table.Column<string>(type: "text", nullable: true),
                    WoundStatus = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentPostOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentPostOps_PatientAssessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "PatientAssessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentRedFlags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Flags = table.Column<string>(type: "text", nullable: true),
                    Decision = table.Column<string>(type: "text", nullable: true),
                    DecisionNotes = table.Column<string>(type: "text", nullable: true),
                    ActionsTaken = table.Column<string>(type: "text", nullable: true),
                    ActionNotes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentRedFlags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentRedFlags_PatientAssessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "PatientAssessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentSubjectives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChiefComplaint = table.Column<string>(type: "text", nullable: true),
                    OnsetMechanism = table.Column<string>(type: "text", nullable: true),
                    PainNow = table.Column<int>(type: "integer", nullable: true),
                    PainBest = table.Column<int>(type: "integer", nullable: true),
                    PainWorst = table.Column<int>(type: "integer", nullable: true),
                    NightPain = table.Column<bool>(type: "boolean", nullable: true),
                    MorningStiffness = table.Column<bool>(type: "boolean", nullable: true),
                    PainPattern24h = table.Column<string>(type: "text", nullable: true),
                    AggravatIngFactors = table.Column<string>(type: "text", nullable: true),
                    EasingFactors = table.Column<string>(type: "text", nullable: true),
                    FunctionalLimits = table.Column<string>(type: "text", nullable: true),
                    PreviousInjuries = table.Column<string>(type: "text", nullable: true),
                    MedicalHistory = table.Column<string>(type: "text", nullable: true),
                    Medications = table.Column<string>(type: "text", nullable: true),
                    ScreeningFlags = table.Column<string>(type: "text", nullable: true),
                    PatientGoals = table.Column<string>(type: "text", nullable: true),
                    AdditionalNotes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentSubjectives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentSubjectives_PatientAssessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "PatientAssessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 25, 10, 57, 4, 331, DateTimeKind.Utc).AddTicks(4981));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 25, 10, 57, 4, 331, DateTimeKind.Utc).AddTicks(6077));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000003"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 25, 10, 57, 4, 331, DateTimeKind.Utc).AddTicks(6081));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000004"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 25, 10, 57, 4, 331, DateTimeKind.Utc).AddTicks(6092));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000005"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 25, 10, 57, 4, 331, DateTimeKind.Utc).AddTicks(6094));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000006"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 25, 10, 57, 4, 331, DateTimeKind.Utc).AddTicks(6096));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000007"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 25, 10, 57, 4, 331, DateTimeKind.Utc).AddTicks(6098));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000008"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 25, 10, 57, 4, 331, DateTimeKind.Utc).AddTicks(6100));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000009"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 25, 10, 57, 4, 331, DateTimeKind.Utc).AddTicks(6102));

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentClinicalReasonings_AssessmentId",
                table: "AssessmentClinicalReasonings",
                column: "AssessmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentNeuros_AssessmentId",
                table: "AssessmentNeuros",
                column: "AssessmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentObjectives_AssessmentId",
                table: "AssessmentObjectives",
                column: "AssessmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentPostOps_AssessmentId",
                table: "AssessmentPostOps",
                column: "AssessmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentRedFlags_AssessmentId",
                table: "AssessmentRedFlags",
                column: "AssessmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentSubjectives_AssessmentId",
                table: "AssessmentSubjectives",
                column: "AssessmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicAssessmentFieldConfigs_ClinicId_StepNumber_FieldKey",
                table: "ClinicAssessmentFieldConfigs",
                columns: new[] { "ClinicId", "StepNumber", "FieldKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Diagnoses_BodyRegionCategoryId",
                table: "Diagnoses",
                column: "BodyRegionCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Diagnoses_IcdCode_ClinicId",
                table: "Diagnoses",
                columns: new[] { "IcdCode", "ClinicId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Diagnoses_SpecialityId",
                table: "Diagnoses",
                column: "SpecialityId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAssessments_AppointmentId",
                table: "PatientAssessments",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAssessments_ClinicId",
                table: "PatientAssessments",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAssessments_DiagnosisId",
                table: "PatientAssessments",
                column: "DiagnosisId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAssessments_PatientId",
                table: "PatientAssessments",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAssessments_SpecialityId",
                table: "PatientAssessments",
                column: "SpecialityId");

            migrationBuilder.CreateIndex(
                name: "IX_Specialities_Code",
                table: "Specialities",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssessmentClinicalReasonings");

            migrationBuilder.DropTable(
                name: "AssessmentNeuros");

            migrationBuilder.DropTable(
                name: "AssessmentObjectives");

            migrationBuilder.DropTable(
                name: "AssessmentPostOps");

            migrationBuilder.DropTable(
                name: "AssessmentRedFlags");

            migrationBuilder.DropTable(
                name: "AssessmentSubjectives");

            migrationBuilder.DropTable(
                name: "ClinicAssessmentFieldConfigs");

            migrationBuilder.DropTable(
                name: "PatientAssessments");

            migrationBuilder.DropTable(
                name: "Diagnoses");

            migrationBuilder.DropTable(
                name: "Specialities");

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 24, 10, 4, 55, 83, DateTimeKind.Utc).AddTicks(5667));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 24, 10, 4, 55, 83, DateTimeKind.Utc).AddTicks(6654));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000003"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 24, 10, 4, 55, 83, DateTimeKind.Utc).AddTicks(6657));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000004"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 24, 10, 4, 55, 83, DateTimeKind.Utc).AddTicks(6659));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000005"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 24, 10, 4, 55, 83, DateTimeKind.Utc).AddTicks(6661));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000006"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 24, 10, 4, 55, 83, DateTimeKind.Utc).AddTicks(6663));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000007"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 24, 10, 4, 55, 83, DateTimeKind.Utc).AddTicks(6665));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000008"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 24, 10, 4, 55, 83, DateTimeKind.Utc).AddTicks(6672));

            migrationBuilder.UpdateData(
                table: "BodyRegionCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000009"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 24, 10, 4, 55, 83, DateTimeKind.Utc).AddTicks(6674));
        }
    }
}
