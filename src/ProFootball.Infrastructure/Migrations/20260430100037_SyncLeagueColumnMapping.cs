using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProFootball.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncLeagueColumnMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                do $$
                begin
                  if exists (
                    select 1 from information_schema.columns
                    where table_schema='public' and table_name='leagues' and column_name='Description'
                  ) then
                    execute 'alter table public.leagues rename column "Description" to description';
                  end if;
                end $$;
                """);

            migrationBuilder.Sql(
                """
                do $$
                begin
                  if exists (
                    select 1 from information_schema.columns
                    where table_schema='public' and table_name='leagues' and column_name='MaxTeams'
                  ) then
                    execute 'alter table public.leagues rename column "MaxTeams" to max_teams';
                  end if;
                end $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                do $$
                begin
                  if exists (
                    select 1 from information_schema.columns
                    where table_schema='public' and table_name='leagues' and column_name='description'
                  ) then
                    execute 'alter table public.leagues rename column description to "Description"';
                  end if;
                end $$;
                """);

            migrationBuilder.Sql(
                """
                do $$
                begin
                  if exists (
                    select 1 from information_schema.columns
                    where table_schema='public' and table_name='leagues' and column_name='max_teams'
                  ) then
                    execute 'alter table public.leagues rename column max_teams to "MaxTeams"';
                  end if;
                end $$;
                """);
        }
    }
}
