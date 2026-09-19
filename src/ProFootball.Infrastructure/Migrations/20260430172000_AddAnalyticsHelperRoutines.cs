using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ProFootball.Infrastructure.Persistence;

#nullable disable

namespace ProFootball.Infrastructure.Migrations;

[DbContext(typeof(ProFootballDbContext))]
[Migration("20260430172000_AddDashboardProceduresAndView")]
public partial class AddDashboardProceduresAndView : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            create or replace function public.fn_get_player_latest_rating(p_player_id int)
            returns table (
                player_id int,
                attribute_date timestamp with time zone,
                overall_rating int,
                potential int
            )
            language sql
            as $$
                select
                    pa."PlayerId" as player_id,
                    pa."Date" as attribute_date,
                    pa."OverallRating" as overall_rating,
                    pa."Potential" as potential
                from public.player_attributes pa
                where pa."PlayerId" = p_player_id
                order by pa."Date" desc, pa."Id" desc
                limit 1
            $$;
            """);

        migrationBuilder.Sql(
            """
            create or replace procedure public.sp_backfill_team_league_links()
            language plpgsql
            as $$
            begin
                with team_league_matches as (
                    select m."HomeTeamId" as team_id, m."LeagueId" as league_id
                    from public.matches m
                    union all
                    select m."AwayTeamId" as team_id, m."LeagueId" as league_id
                    from public.matches m
                ),
                ranked as (
                    select
                        tlm.team_id,
                        tlm.league_id,
                        count(*) as usage_count,
                        row_number() over (
                            partition by tlm.team_id
                            order by count(*) desc, tlm.league_id asc
                        ) as rn
                    from team_league_matches tlm
                    where tlm.team_id is not null
                      and tlm.league_id is not null
                    group by tlm.team_id, tlm.league_id
                )
                update public.teams t
                set "league_id" = r.league_id
                from ranked r
                where t."Id" = r.team_id
                  and r.rn = 1
                  and (t."league_id" is distinct from r.league_id);
            end;
            $$;
            """);

    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("drop procedure if exists public.sp_backfill_team_league_links();");
        migrationBuilder.Sql("drop function if exists public.fn_get_player_latest_rating(int);");
    }
}
