using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ProFootball.Infrastructure.Persistence;

#nullable disable

namespace ProFootball.Infrastructure.Migrations;

[DbContext(typeof(ProFootballDbContext))]
[Migration("20260430175500_AddAnalyticsRebuildProcedures")]
public partial class AddAnalyticsRebuildProcedures : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            create or replace procedure public.sp_rebuild_team_season_stats(
                p_season text default null,
                p_league_id int default null
            )
            language plpgsql
            as $$
            begin
                delete from public.team_season_stats tss
                where (p_season is null or tss."Season" = p_season)
                  and (p_league_id is null or tss."LeagueId" = p_league_id);

                insert into public.team_season_stats
                (
                    "Season", "LeagueId", "TeamId", "Matches", "Wins", "Draws", "Losses", "GoalsFor", "GoalsAgainst", "Points"
                )
                select
                    x.season,
                    x.league_id,
                    x.team_id,
                    count(*)::int as matches,
                    sum(case when x.is_win then 1 else 0 end)::int as wins,
                    sum(case when x.is_draw then 1 else 0 end)::int as draws,
                    sum(case when not x.is_win and not x.is_draw then 1 else 0 end)::int as losses,
                    sum(x.goals_for)::int as goals_for,
                    sum(x.goals_against)::int as goals_against,
                    (sum(case when x.is_win then 1 else 0 end) * 3 + sum(case when x.is_draw then 1 else 0 end))::int as points
                from (
                    select
                        m."Season" as season,
                        m."LeagueId" as league_id,
                        m."HomeTeamId" as team_id,
                        coalesce(m."HomeTeamGoal", 0) as goals_for,
                        coalesce(m."AwayTeamGoal", 0) as goals_against,
                        (coalesce(m."HomeTeamGoal", 0) > coalesce(m."AwayTeamGoal", 0)) as is_win,
                        (coalesce(m."HomeTeamGoal", 0) = coalesce(m."AwayTeamGoal", 0)) as is_draw
                    from public.matches m
                    where (p_season is null or m."Season" = p_season)
                      and (p_league_id is null or m."LeagueId" = p_league_id)
                    union all
                    select
                        m."Season" as season,
                        m."LeagueId" as league_id,
                        m."AwayTeamId" as team_id,
                        coalesce(m."AwayTeamGoal", 0) as goals_for,
                        coalesce(m."HomeTeamGoal", 0) as goals_against,
                        (coalesce(m."AwayTeamGoal", 0) > coalesce(m."HomeTeamGoal", 0)) as is_win,
                        (coalesce(m."AwayTeamGoal", 0) = coalesce(m."HomeTeamGoal", 0)) as is_draw
                    from public.matches m
                    where (p_season is null or m."Season" = p_season)
                      and (p_league_id is null or m."LeagueId" = p_league_id)
                ) x
                group by x.season, x.league_id, x.team_id;
            end;
            $$;
            """);

        migrationBuilder.Sql(
            """
            create or replace procedure public.sp_rebuild_analytics_fact_daily(
                p_from date,
                p_to date,
                p_season text default null,
                p_league_id int default null
            )
            language plpgsql
            as $$
            begin
                delete from public.analytics_fact_daily afd
                where afd."DateKey" between p_from and p_to
                  and (p_season is null or afd."Season" = p_season)
                  and (p_league_id is null or afd."LeagueId" = p_league_id);

                with latest_ratings as (
                    select avg(pa."OverallRating")::numeric(18,6) as avg_rating
                    from (
                        select distinct on (x."PlayerId") x."PlayerId", x."OverallRating"
                        from public.player_attributes x
                        where x."OverallRating" is not null
                        order by x."PlayerId", x."Date" desc, x."Id" desc
                    ) pa
                ),
                grouped as (
                    select
                        m."Date"::date as date_key,
                        m."Season" as season,
                        m."LeagueId" as league_id,
                        count(*)::int as matches_count,
                        avg((coalesce(m."HomeTeamGoal",0) + coalesce(m."AwayTeamGoal",0))::numeric(18,6))::numeric(18,6) as avg_goals,
                        (sum(case when coalesce(m."HomeTeamGoal",0) > coalesce(m."AwayTeamGoal",0) then 1 else 0 end)::numeric / nullif(count(*),0))::numeric(18,6) as home_win_rate,
                        (sum(case when coalesce(m."HomeTeamGoal",0) = coalesce(m."AwayTeamGoal",0) then 1 else 0 end)::numeric / nullif(count(*),0))::numeric(18,6) as draw_rate,
                        (sum(case when coalesce(m."HomeTeamGoal",0) < coalesce(m."AwayTeamGoal",0) then 1 else 0 end)::numeric / nullif(count(*),0))::numeric(18,6) as away_win_rate
                    from public.matches m
                    where m."Date"::date between p_from and p_to
                      and (p_season is null or m."Season" = p_season)
                      and (p_league_id is null or m."LeagueId" = p_league_id)
                    group by m."Date"::date, m."Season", m."LeagueId"
                )
                insert into public.analytics_fact_daily
                ("DateKey", "Season", "LeagueId", "MetricKey", "MetricValue", "UpdatedAtUtc")
                select g.date_key, g.season, g.league_id, metric_key, metric_value, now()
                from grouped g
                cross join latest_ratings lr
                cross join lateral (
                    values
                        ('matches_count'::varchar(64), g.matches_count::numeric(18,6)),
                        ('avg_goals'::varchar(64), coalesce(g.avg_goals, 0)),
                        ('home_win_rate'::varchar(64), coalesce(g.home_win_rate, 0)),
                        ('draw_rate'::varchar(64), coalesce(g.draw_rate, 0)),
                        ('away_win_rate'::varchar(64), coalesce(g.away_win_rate, 0)),
                        ('avg_rating'::varchar(64), coalesce(lr.avg_rating, 0))
                ) as metrics(metric_key, metric_value);
            end;
            $$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("drop procedure if exists public.sp_rebuild_analytics_fact_daily(date, date, text, int);");
        migrationBuilder.Sql("drop procedure if exists public.sp_rebuild_team_season_stats(text, int);");
    }
}
