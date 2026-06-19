using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProFootball.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ApplyAnalyticsDbObjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("alter table public.leagues add column if not exists max_teams integer;");
            migrationBuilder.Sql("alter table public.leagues add column if not exists description character varying(2000);");

            migrationBuilder.Sql(
                """
                create or replace view vw_match_outcome_stats as
                select m."Season" as season, m."LeagueId" as league_id, l."Name" as league_name, l."CountryName" as country_name,
                       count(*)::int as match_count,
                       sum(case when m."HomeTeamGoal" > m."AwayTeamGoal" then 1 else 0 end)::int as home_wins,
                       sum(case when m."HomeTeamGoal" = m."AwayTeamGoal" then 1 else 0 end)::int as draws,
                       sum(case when m."HomeTeamGoal" < m."AwayTeamGoal" then 1 else 0 end)::int as away_wins,
                       avg((m."HomeTeamGoal" + m."AwayTeamGoal")::numeric(10,2))::numeric(10,2) as avg_goals_per_match
                from public.matches m
                join public.leagues l on l."Id" = m."LeagueId"
                where m."HomeTeamGoal" is not null and m."AwayTeamGoal" is not null
                group by m."Season", m."LeagueId", l."Name", l."CountryName";
                """);

            migrationBuilder.Sql(
                """
                create or replace view vw_player_latest_attributes as
                select distinct on (pa."PlayerId") pa."PlayerId" as player_id, p."Name" as player_name,
                       pa."Date" as attribute_date, pa."OverallRating" as overall_rating, pa."Potential" as potential,
                       (coalesce(pa."Potential",0) - coalesce(pa."OverallRating",0)) as rating_delta
                from public.player_attributes pa
                join public.players p on p."Id" = pa."PlayerId"
                order by pa."PlayerId", pa."Date" desc, pa."Id" desc;
                """);

            migrationBuilder.Sql(
                """
                create or replace view vw_player_rating_trend_monthly as
                with month_series as (
                    select m."Season" as season, m."LeagueId" as league_id, date_trunc('month', m."Date")::date as month_start
                    from public.matches m
                    where m."Date" is not null
                    group by m."Season", m."LeagueId", date_trunc('month', m."Date")
                )
                select ms.season, ms.league_id, ms.month_start,
                       avg(pa."OverallRating"::numeric(10,2))::numeric(10,2) as avg_overall_rating
                from month_series ms
                join public.player_attributes pa on date_trunc('month', pa."Date")::date = ms.month_start
                where pa."OverallRating" is not null
                group by ms.season, ms.league_id, ms.month_start;
                """);

            migrationBuilder.Sql(
                """
                create or replace view vw_league_competitiveness as
                select m."Season" as season, m."LeagueId" as league_id, l."Name" as league_name, l."CountryName" as country_name,
                       count(*)::int as match_count,
                       avg(abs((m."HomeTeamGoal" - m."AwayTeamGoal")::numeric(10,2)))::numeric(10,2) as avg_goal_diff,
                       (sum(case when m."HomeTeamGoal" = m."AwayTeamGoal" then 1 else 0 end)::numeric / nullif(count(*),0))::numeric(10,4) as draw_rate
                from public.matches m
                join public.leagues l on l."Id" = m."LeagueId"
                where m."HomeTeamGoal" is not null and m."AwayTeamGoal" is not null
                group by m."Season", m."LeagueId", l."Name", l."CountryName";
                """);

            migrationBuilder.Sql("alter table public.matches add constraint ck_matches_teams_not_equal check (\"HomeTeamId\" <> \"AwayTeamId\");");
            migrationBuilder.Sql("alter table public.matches add constraint ck_matches_home_goal_nonnegative check (\"HomeTeamGoal\" is null or \"HomeTeamGoal\" >= 0);");
            migrationBuilder.Sql("alter table public.matches add constraint ck_matches_away_goal_nonnegative check (\"AwayTeamGoal\" is null or \"AwayTeamGoal\" >= 0);");
            migrationBuilder.Sql("alter table public.matches add constraint ck_matches_season_format check (\"Season\" ~ '^[0-9]{4}/[0-9]{4}$');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("drop view if exists vw_league_competitiveness;");
            migrationBuilder.Sql("drop view if exists vw_player_rating_trend_monthly;");
            migrationBuilder.Sql("drop view if exists vw_player_latest_attributes;");
            migrationBuilder.Sql("drop view if exists vw_match_outcome_stats;");

            migrationBuilder.Sql("alter table public.matches drop constraint if exists ck_matches_season_format;");
            migrationBuilder.Sql("alter table public.matches drop constraint if exists ck_matches_away_goal_nonnegative;");
            migrationBuilder.Sql("alter table public.matches drop constraint if exists ck_matches_home_goal_nonnegative;");
            migrationBuilder.Sql("alter table public.matches drop constraint if exists ck_matches_teams_not_equal;");
        }
    }
}
