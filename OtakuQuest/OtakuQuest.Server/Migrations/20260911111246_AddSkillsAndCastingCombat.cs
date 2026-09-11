using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OtakuQuest.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddSkillsAndCastingCombat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Skills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Slot = table.Column<int>(type: "int", nullable: false),
                    CastTurns = table.Column<int>(type: "int", nullable: false),
                    UnlockLevel = table.Column<int>(type: "int", nullable: false),
                    DamageMultiplier = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ComboBonusMultiplier = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    AppliesCombo = table.Column<bool>(type: "bit", nullable: false),
                    ConsumesCombo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Skills", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BossSkills",
                columns: table => new
                {
                    BossId = table.Column<int>(type: "int", nullable: false),
                    SkillId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BossSkills", x => new { x.BossId, x.SkillId });
                    table.ForeignKey(
                        name: "FK_BossSkills_Bosses_BossId",
                        column: x => x.BossId,
                        principalTable: "Bosses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BossSkills_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterSkills",
                columns: table => new
                {
                    CharacterItemId = table.Column<int>(type: "int", nullable: false),
                    SkillId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterSkills", x => new { x.CharacterItemId, x.SkillId });
                    table.ForeignKey(
                        name: "FK_CharacterSkills_Items_CharacterItemId",
                        column: x => x.CharacterItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterSkills_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserCombatStates",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TurnNumber = table.Column<int>(type: "int", nullable: false),
                    PlayerCastingSkillId = table.Column<int>(type: "int", nullable: true),
                    PlayerCastTurnsRemaining = table.Column<int>(type: "int", nullable: false),
                    PlayerComboReady = table.Column<bool>(type: "bit", nullable: false),
                    BossCastingSkillId = table.Column<int>(type: "int", nullable: true),
                    BossCastTurnsRemaining = table.Column<int>(type: "int", nullable: false),
                    BossComboReady = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCombatStates", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserCombatStates_Skills_BossCastingSkillId",
                        column: x => x.BossCastingSkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserCombatStates_Skills_PlayerCastingSkillId",
                        column: x => x.PlayerCastingSkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserCombatStates_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BossSkills_SkillId",
                table: "BossSkills",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterSkills_SkillId",
                table: "CharacterSkills",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_Skills_Name",
                table: "Skills",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserCombatStates_BossCastingSkillId",
                table: "UserCombatStates",
                column: "BossCastingSkillId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCombatStates_PlayerCastingSkillId",
                table: "UserCombatStates",
                column: "PlayerCastingSkillId");

            migrationBuilder.InsertData(
                table: "Skills",
                columns: new[]
                {
                    "Id", "Name", "Description", "Slot", "CastTurns",
                    "UnlockLevel", "DamageMultiplier", "ComboBonusMultiplier",
                    "AppliesCombo", "ConsumesCombo"
                },
                values: new object[,]
                {
                    { 1, "Focused Strike", "Concentrate power and prepare the ultimate.", 0, 1, 1, 1.70m, 0.00m, true, false },
                    { 2, "Heroic Finish", "A decisive attack empowered by Focused Strike.", 1, 2, 5, 2.80m, 2.00m, false, true },
                    { 3, "Petal Brand", "Mark the enemy with gathering petals.", 0, 1, 1, 1.70m, 0.00m, true, false },
                    { 4, "Thousand Petal Bloom", "Detonate the petal mark for massive damage.", 1, 2, 5, 2.80m, 2.00m, false, true },
                    { 5, "Dissonant Note", "Plant a magical resonance in the enemy.", 0, 1, 1, 1.70m, 0.00m, true, false },
                    { 6, "Final Movement", "Complete the movement and amplify its resonance.", 1, 2, 5, 2.80m, 2.00m, false, true },
                    { 7, "Predator's Trace", "Leave a trace that guides the finishing attack.", 0, 1, 1, 1.70m, 0.00m, true, false },
                    { 8, "Crimson Hunt", "Consume the trace for a brutal finishing blow.", 1, 2, 5, 2.80m, 2.00m, false, true },
                    { 9, "Crystal Target", "Fix a crystal target onto the enemy.", 0, 1, 1, 1.70m, 0.00m, true, false },
                    { 10, "Winter's Verdict", "Shatter the target with concentrated force.", 1, 2, 5, 2.80m, 2.00m, false, true },
                    { 11, "Teacher's Mark", "Mark the enemy for the final lesson.", 0, 1, 1, 1.70m, 0.00m, true, false },
                    { 12, "Mach 20 Lesson", "Deliver the final lesson at impossible speed.", 1, 2, 5, 2.80m, 2.00m, false, true },
                    { 13, "Burning Brand", "Prepare the target for Bowser's inferno.", 0, 1, 1, 1.70m, 0.00m, true, false },
                    { 14, "Koopa Inferno", "A huge flame attack empowered by Burning Brand.", 1, 2, 1, 2.80m, 2.00m, false, true },
                    { 15, "Assassination Lesson", "Expose a weakness for the final exam.", 0, 1, 1, 1.70m, 0.00m, true, false },
                    { 16, "Final Exam", "Exploit the exposed weakness with overwhelming speed.", 1, 2, 1, 2.80m, 2.00m, false, true }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BossSkills");

            migrationBuilder.DropTable(
                name: "CharacterSkills");

            migrationBuilder.DropTable(
                name: "UserCombatStates");

            migrationBuilder.DropTable(
                name: "Skills");
        }
    }
}
