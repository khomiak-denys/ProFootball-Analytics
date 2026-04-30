using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ProFootball.Infrastructure.Persistence;

#nullable disable

namespace ProFootball.Infrastructure.Migrations;

[DbContext(typeof(ProFootballDbContext))]
[Migration("20260430172100_AddDashboardOverviewView")]
public partial class AddDashboardOverviewView : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            create or replace view public.vw_dashboard_overview as
            with base as (
                select
                    m."Season" as season,
                    m."LeagueId" as league_id,
                    l."Name" as league_name,
                    l."CountryName" as country_name,
                    m."Date" as match_date,
                    m."HomeTeamGoal" as home_goals,
                    m."AwayTeamGoal" as away_goals
                from public.matches m
                join public.leagues l on l."Id" = m."LeagueId"
            )
            select
                b.season,
                b.league_id,
                b.league_name,
                b.country_name,
                count(*)::int as match_count,
                count(distinct t."Id")::int as total_clubs,
                sum(case when b.home_goals > b.away_goals then 1 else 0 end)::int as home_wins,
                sum(case when b.home_goals = b.away_goals then 1 else 0 end)::int as draws,
                sum(case when b.home_goals < b.away_goals then 1 else 0 end)::int as away_wins,
                avg((coalesce(b.home_goals, 0) + coalesce(b.away_goals, 0))::numeric(10,2))::numeric(10,2) as avg_goals_per_match,
                max(b.match_date) as last_match_date
            from base b
            left join public.teams t on t."league_id" = b.league_id
            group by
                b.season,
                b.league_id,
                b.league_name,
                b.country_name;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("drop view if exists public.vw_dashboard_overview;");
    }
}

