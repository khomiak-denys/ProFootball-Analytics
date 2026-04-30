using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProFootball.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDataIntegrityTriggers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                drop trigger if exists trg_matches_validate_teams on public.matches;
                drop trigger if exists trg_matches_validate_season_by_date on public.matches;
                drop trigger if exists trg_matches_validate_league_country_consistency on public.matches;
                drop trigger if exists trg_teams_shortname_normalize on public.teams;
                drop trigger if exists trg_player_attributes_validate_range on public.player_attributes;
                drop trigger if exists trg_player_attributes_chronology_guard on public.player_attributes;
                drop trigger if exists trg_prevent_delete_referenced_team on public.teams;
                drop trigger if exists trg_league_max_teams_guard on public.teams;
                """);

            migrationBuilder.Sql(
                """
                drop function if exists public.fn_trg_matches_validate_teams();
                drop function if exists public.fn_trg_matches_validate_season_by_date();
                drop function if exists public.fn_trg_matches_validate_league_country_consistency();
                drop function if exists public.fn_trg_teams_shortname_normalize();
                drop function if exists public.fn_trg_player_attributes_validate_range();
                drop function if exists public.fn_trg_player_attributes_chronology_guard();
                drop function if exists public.fn_trg_prevent_delete_referenced_team();
                drop function if exists public.fn_trg_league_max_teams_guard();
                """);

            migrationBuilder.Sql(
                """
                create function public.fn_trg_matches_validate_teams()
                returns trigger
                language plpgsql
                as $$
                begin
                  if new."HomeTeamId" = new."AwayTeamId" then
                    raise exception 'Home and away team cannot be the same';
                  end if;
                  return new;
                end;
                $$;
                create trigger trg_matches_validate_teams
                before insert or update on public.matches
                for each row execute function public.fn_trg_matches_validate_teams();
                """);

            migrationBuilder.Sql(
                """
                create function public.fn_trg_matches_validate_season_by_date()
                returns trigger
                language plpgsql
                as $$
                declare
                  season_start integer;
                  season_end integer;
                  season_from date;
                  season_to date;
                begin
                  if new."Season" is null or new."Date" is null then
                    return new;
                  end if;

                  if new."Season" !~ '^[0-9]{4}/[0-9]{4}$' then
                    raise exception 'Season format must be YYYY/YYYY';
                  end if;

                  season_start := split_part(new."Season", '/', 1)::integer;
                  season_end := split_part(new."Season", '/', 2)::integer;
                  if season_end <> season_start + 1 then
                    raise exception 'Season year range is invalid';
                  end if;

                  season_from := make_date(season_start, 7, 1);
                  season_to := make_date(season_end, 6, 30);
                  if new."Date"::date < season_from or new."Date"::date > season_to then
                    raise exception 'Match date is outside season range';
                  end if;

                  return new;
                end;
                $$;
                create trigger trg_matches_validate_season_by_date
                before insert or update on public.matches
                for each row execute function public.fn_trg_matches_validate_season_by_date();
                """);

            migrationBuilder.Sql(
                """
                create function public.fn_trg_matches_validate_league_country_consistency()
                returns trigger
                language plpgsql
                as $$
                declare
                  league_country text;
                begin
                  select "CountryName" into league_country
                  from public.leagues
                  where "Id" = new."LeagueId";

                  if league_country is null then
                    raise exception 'League % does not exist', new."LeagueId";
                  end if;

                  if new."CountryName" is null or btrim(new."CountryName") = '' then
                    new."CountryName" := league_country;
                    return new;
                  end if;

                  if new."CountryName" <> league_country then
                    raise exception 'CountryName % does not match league country %', new."CountryName", league_country;
                  end if;

                  return new;
                end;
                $$;
                create trigger trg_matches_validate_league_country_consistency
                before insert or update on public.matches
                for each row execute function public.fn_trg_matches_validate_league_country_consistency();
                """);

            migrationBuilder.Sql(
                """
                create function public.fn_trg_teams_shortname_normalize()
                returns trigger
                language plpgsql
                as $$
                begin
                  if new."ShortName" is null then
                    return new;
                  end if;

                  new."ShortName" := upper(btrim(new."ShortName"));
                  if new."ShortName" = '' then
                    new."ShortName" := null;
                  end if;

                  return new;
                end;
                $$;
                create trigger trg_teams_shortname_normalize
                before insert or update on public.teams
                for each row execute function public.fn_trg_teams_shortname_normalize();
                """);

            migrationBuilder.Sql(
                """
                create function public.fn_trg_player_attributes_validate_range()
                returns trigger
                language plpgsql
                as $$
                begin
                  if new."OverallRating" is not null and (new."OverallRating" < 0 or new."OverallRating" > 100) then
                    raise exception 'OverallRating must be in range [0..100]';
                  end if;
                  if new."Potential" is not null and (new."Potential" < 0 or new."Potential" > 100) then
                    raise exception 'Potential must be in range [0..100]';
                  end if;
                  return new;
                end;
                $$;
                create trigger trg_player_attributes_validate_range
                before insert or update on public.player_attributes
                for each row execute function public.fn_trg_player_attributes_validate_range();
                """);

            migrationBuilder.Sql(
                """
                create function public.fn_trg_player_attributes_chronology_guard()
                returns trigger
                language plpgsql
                as $$
                begin
                  if exists (
                    select 1
                    from public.player_attributes pa
                    where pa."PlayerId" = new."PlayerId"
                      and pa."Date" > new."Date"
                      and pa."Id" <> coalesce(new."Id", -1)
                  ) then
                    raise exception 'Player attribute date cannot be earlier than existing newer snapshot';
                  end if;
                  return new;
                end;
                $$;
                create trigger trg_player_attributes_chronology_guard
                before insert or update on public.player_attributes
                for each row execute function public.fn_trg_player_attributes_chronology_guard();
                """);

            migrationBuilder.Sql(
                """
                create function public.fn_trg_prevent_delete_referenced_team()
                returns trigger
                language plpgsql
                as $$
                begin
                  if exists (
                    select 1 from public.matches m
                    where m."HomeTeamId" = old."Id" or m."AwayTeamId" = old."Id"
                  ) then
                    raise exception 'Cannot delete team % because it is referenced by matches', old."Id";
                  end if;
                  return old;
                end;
                $$;
                create trigger trg_prevent_delete_referenced_team
                before delete on public.teams
                for each row execute function public.fn_trg_prevent_delete_referenced_team();
                """);

            migrationBuilder.Sql(
                """
                create function public.fn_trg_league_max_teams_guard()
                returns trigger
                language plpgsql
                as $$
                declare
                  max_teams_value integer;
                  team_count integer;
                begin
                  if new.league_id is null then
                    return new;
                  end if;

                  select l.max_teams into max_teams_value
                  from public.leagues l
                  where l."Id" = new.league_id;

                  if max_teams_value is null then
                    return new;
                  end if;

                  select count(*) into team_count
                  from public.teams t
                  where t.league_id = new.league_id
                    and t."Id" <> coalesce(new."Id", -1);

                  if team_count >= max_teams_value then
                    raise exception 'League % reached max_teams limit (%)', new.league_id, max_teams_value;
                  end if;

                  return new;
                end;
                $$;
                create trigger trg_league_max_teams_guard
                before insert or update of league_id on public.teams
                for each row execute function public.fn_trg_league_max_teams_guard();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                drop trigger if exists trg_matches_validate_teams on public.matches;
                drop trigger if exists trg_matches_validate_season_by_date on public.matches;
                drop trigger if exists trg_matches_validate_league_country_consistency on public.matches;
                drop trigger if exists trg_teams_shortname_normalize on public.teams;
                drop trigger if exists trg_player_attributes_validate_range on public.player_attributes;
                drop trigger if exists trg_player_attributes_chronology_guard on public.player_attributes;
                drop trigger if exists trg_prevent_delete_referenced_team on public.teams;
                drop trigger if exists trg_league_max_teams_guard on public.teams;
                """);

            migrationBuilder.Sql(
                """
                drop function if exists public.fn_trg_matches_validate_teams();
                drop function if exists public.fn_trg_matches_validate_season_by_date();
                drop function if exists public.fn_trg_matches_validate_league_country_consistency();
                drop function if exists public.fn_trg_teams_shortname_normalize();
                drop function if exists public.fn_trg_player_attributes_validate_range();
                drop function if exists public.fn_trg_player_attributes_chronology_guard();
                drop function if exists public.fn_trg_prevent_delete_referenced_team();
                drop function if exists public.fn_trg_league_max_teams_guard();
                """);
        }
    }
}
