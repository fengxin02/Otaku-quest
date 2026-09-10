using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OtakuQuest.Server.Data;

#nullable disable

namespace OtakuQuest.Server.Migrations
{
    [DbContext(typeof(OtakuQuestDbContext))]
    [Migration("20260910113000_RebalanceExistingContentValues")]
    public partial class RebalanceExistingContentValues : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE [Items]
                SET [HpBonus] = 150, [StrBonus] = 0, [IntBonus] = 0, [DefBonus] = 0,
                    [HpMultiplier] = 1, [StrMultiplier] = 1, [IntMultiplier] = 1, [DefMultiplier] = 1
                WHERE [Name] = N'Sakura';

                UPDATE [Items]
                SET [HpBonus] = 0, [StrBonus] = 0, [IntBonus] = 60, [DefBonus] = 0,
                    [HpMultiplier] = 1, [StrMultiplier] = 1, [IntMultiplier] = 1, [DefMultiplier] = 1
                WHERE [Name] = N'Togawa Sakiko';

                UPDATE [Items]
                SET [HpBonus] = 80, [StrBonus] = 100, [IntBonus] = -5, [DefBonus] = 0,
                    [HpMultiplier] = 1, [StrMultiplier] = 1, [IntMultiplier] = 1, [DefMultiplier] = 1
                WHERE [Name] = N'Luluka';

                UPDATE [Items]
                SET [HpBonus] = 80, [StrBonus] = 30, [IntBonus] = 10, [DefBonus] = 8,
                    [HpMultiplier] = 1, [StrMultiplier] = 1, [IntMultiplier] = 1, [DefMultiplier] = 1
                WHERE [Name] = N'Carlotta';

                UPDATE [Items]
                SET [HpBonus] = 250, [StrBonus] = 40, [IntBonus] = 30, [DefBonus] = 25,
                    [HpMultiplier] = 1, [StrMultiplier] = 1, [IntMultiplier] = 1, [DefMultiplier] = 1
                WHERE [Name] = N'Koro Sensei' AND [Type] = 1;

                UPDATE [Items]
                SET [HpBonus] = 0, [StrBonus] = 120, [IntBonus] = 0, [DefBonus] = 0,
                    [HpMultiplier] = 1, [StrMultiplier] = 1, [IntMultiplier] = 1, [DefMultiplier] = 1
                WHERE [Name] = N'Diamond Sword';

                UPDATE [Items]
                SET [HpBonus] = -40, [StrBonus] = 90, [IntBonus] = 45, [DefBonus] = -8,
                    [HpMultiplier] = 1, [StrMultiplier] = 1, [IntMultiplier] = 1, [DefMultiplier] = 1
                WHERE [Name] = N'Katana';

                UPDATE [Items]
                SET [HpBonus] = 200, [StrBonus] = 60, [IntBonus] = 20, [DefBonus] = 10,
                    [HpMultiplier] = 1, [StrMultiplier] = 1, [IntMultiplier] = 1, [DefMultiplier] = 1
                WHERE [Name] = N'Mario''s Mushrrom';

                UPDATE [Bosses]
                SET [MaxHp] = 400, [STR] = 25, [INT] = 10, [DEF] = 20
                WHERE [Name] = N'Bowser';

                UPDATE [Bosses]
                SET [MaxHp] = 900, [STR] = 50, [INT] = 20, [DEF] = 50
                WHERE [Name] = N'Koro Sensei';
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE [Items]
                SET [HpBonus] = 100, [StrBonus] = 0, [IntBonus] = 0, [DefBonus] = 0,
                    [HpMultiplier] = 1, [StrMultiplier] = 1, [IntMultiplier] = 1, [DefMultiplier] = 1
                WHERE [Name] = N'Sakura';

                UPDATE [Items]
                SET [HpBonus] = 0, [StrBonus] = 0, [IntBonus] = 100, [DefBonus] = 0,
                    [HpMultiplier] = 1, [StrMultiplier] = 1, [IntMultiplier] = 1, [DefMultiplier] = 1
                WHERE [Name] = N'Togawa Sakiko';

                UPDATE [Items]
                SET [HpBonus] = 10, [StrBonus] = 20, [IntBonus] = -15, [DefBonus] = 0,
                    [HpMultiplier] = 1.2, [StrMultiplier] = 2, [IntMultiplier] = 1, [DefMultiplier] = 1
                WHERE [Name] = N'Luluka';

                UPDATE [Items]
                SET [HpBonus] = 100, [StrBonus] = 250, [IntBonus] = 10, [DefBonus] = 20,
                    [HpMultiplier] = 1, [StrMultiplier] = 1, [IntMultiplier] = 1, [DefMultiplier] = 1
                WHERE [Name] = N'Carlotta';

                UPDATE [Items]
                SET [HpBonus] = 500, [StrBonus] = 10, [IntBonus] = 0, [DefBonus] = 201,
                    [HpMultiplier] = 1, [StrMultiplier] = 1, [IntMultiplier] = 2, [DefMultiplier] = 1
                WHERE [Name] = N'Koro Sensei' AND [Type] = 1;

                UPDATE [Items]
                SET [HpBonus] = 0, [StrBonus] = 500, [IntBonus] = 0, [DefBonus] = 0,
                    [HpMultiplier] = 1, [StrMultiplier] = 1, [IntMultiplier] = 1, [DefMultiplier] = 1
                WHERE [Name] = N'Diamond Sword';

                UPDATE [Items]
                SET [HpBonus] = -10, [StrBonus] = 100, [IntBonus] = 50, [DefBonus] = -10,
                    [HpMultiplier] = 1, [StrMultiplier] = 1, [IntMultiplier] = 1, [DefMultiplier] = 1
                WHERE [Name] = N'Katana';

                UPDATE [Items]
                SET [HpBonus] = 500, [StrBonus] = 123, [IntBonus] = 21, [DefBonus] = 19,
                    [HpMultiplier] = 1, [StrMultiplier] = 1, [IntMultiplier] = 1, [DefMultiplier] = 1
                WHERE [Name] = N'Mario''s Mushrrom';

                UPDATE [Bosses]
                SET [MaxHp] = 800, [STR] = 20, [INT] = 10, [DEF] = 15
                WHERE [Name] = N'Bowser';

                UPDATE [Bosses]
                SET [MaxHp] = 1555, [STR] = 24, [INT] = 10, [DEF] = 50
                WHERE [Name] = N'Koro Sensei';
                """);
        }
    }
}
