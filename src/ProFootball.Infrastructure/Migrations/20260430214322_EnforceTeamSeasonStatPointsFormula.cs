using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProFootball.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceTeamSeasonStatPointsFormula : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                create or replace function public.trg_team_season_stats_apply_formulas_fn()
                returns trigger
                language plpgsql
                as $$
                begin
                    new."Matches" := coalesce(new."Wins", 0) + coalesce(new."Draws", 0) + coalesce(new."Losses", 0);
                    new."Points" := coalesce(new."Wins", 0) * 3 + coalesce(new."Draws", 0);
                    return new;
                end;
                $$;
                """);

            migrationBuilder.Sql(
                """
                drop trigger if exists trg_team_season_stats_apply_formulas on public.team_season_stats;

                create trigger trg_team_season_stats_apply_formulas
                before insert or update of "Wins", "Draws", "Losses", "Matches", "Points"
                on public.team_season_stats
                for each row
                execute function public.trg_team_season_stats_apply_formulas_fn();
                """);

            migrationBuilder.Sql(
                """
                do $$
                begin
                    if not exists (
                        select 1
                        from pg_constraint
                        where conname = 'CK_team_season_stats_matches_formula'
                    ) then
                        alter table public.team_season_stats
                        add constraint "CK_team_season_stats_matches_formula"
                        check ("Matches" = "Wins" + "Draws" + "Losses");
                    end if;
                end;
                $$;
                """);

            migrationBuilder.Sql(
                """
                do $$
                begin
                    if not exists (
                        select 1
                        from pg_constraint
                        where conname = 'CK_team_season_stats_points_formula'
                    ) then
                        alter table public.team_season_stats
                        add constraint "CK_team_season_stats_points_formula"
                        check ("Points" = "Wins" * 3 + "Draws");
                    end if;
                end;
                $$;
                """);

            migrationBuilder.Sql(
                """
                update public.team_season_stats
                set
                    "Matches" = coalesce("Wins", 0) + coalesce("Draws", 0) + coalesce("Losses", 0),
                    "Points" = coalesce("Wins", 0) * 3 + coalesce("Draws", 0);
                """);

            migrationBuilder.Sql("call public.sp_rebuild_team_season_stats(null, null);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                alter table public.team_season_stats
                drop constraint if exists "CK_team_season_stats_points_formula";
                """);

            migrationBuilder.Sql(
                """
                alter table public.team_season_stats
                drop constraint if exists "CK_team_season_stats_matches_formula";
                """);

            migrationBuilder.Sql(
                """
                drop trigger if exists trg_team_season_stats_apply_formulas on public.team_season_stats;
                drop function if exists public.trg_team_season_stats_apply_formulas_fn();
                """);
        }
    }
}
