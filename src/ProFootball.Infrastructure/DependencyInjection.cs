using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Auth.Abstractions;
using ProFootball.Application.Auth.Commands;
using ProFootball.Application.Auth.Dtos;
using ProFootball.Application.Auth.Handlers;
using ProFootball.Application.Auth.Queries;
using ProFootball.Application.Country.Dtos;
using ProFootball.Application.Country.Queries;
using ProFootball.Application.Common;
using ProFootball.Application.Dispatching;
using ProFootball.Application.Importing.Commands;
using ProFootball.Application.Importing.Dtos;
using ProFootball.Application.League.Dtos;
using ProFootball.Application.League.Queries;
using ProFootball.Application.Match.Dtos;
using ProFootball.Application.Match.Queries;
using ProFootball.Application.Player.Dtos;
using ProFootball.Application.Player.Queries;
using ProFootball.Application.Team.Dtos;
using ProFootball.Application.Team.Queries;
using ProFootball.Domain.Entities;
using ProFootball.Infrastructure.Auth;
using ProFootball.Infrastructure.Importing;
using ProFootball.Infrastructure.Persistence;
using ProFootball.Infrastructure.Persistence.Repositories;
using ProFootball.Infrastructure.Querying;

namespace ProFootball.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "ProFootballDb";
    public const string ConnectionStringEnvironmentVariable = "PROFOOTBALL_DB_CONNECTION_STRING";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
                               ?? configuration[ConnectionStringEnvironmentVariable];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is missing. " +
                $"Set appsettings.json or env var '{ConnectionStringEnvironmentVariable}'.");
        }

        services.AddDbContext<ProFootballDbContext>(options => options.UseNpgsql(connectionString));
        services.AddDbContextFactory<ProFootballDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ILeagueRepository, EfLeagueRepository>();
        services.AddScoped<ITeamRepository, EfTeamRepository>();
        services.AddScoped<ITeamAttributeRepository, EfTeamAttributeRepository>();
        services.AddScoped<IPlayerRepository, EfPlayerRepository>();
        services.AddScoped<IPlayerAttributeRepository, EfPlayerAttributeRepository>();
        services.AddScoped<IFootballMatchRepository, EfFootballMatchRepository>();
        services.AddScoped<IAppUserAuthRepository, EfAppUserAuthRepository>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IQueryDispatcher, QueryDispatcher>();
        services.AddScoped<ICommandDispatcher, CommandDispatcher>();

        services.AddSingleton<IUserSessionStore, InMemorySessionStore>();
        services.AddScoped<ICommandHandler<SignInCommand, Result>, SignInCommandHandler>();
        services.AddScoped<ICommandHandler<RegisterUserCommand, Result>, RegisterUserCommandHandler>();
        services.AddScoped<ICommandHandler<SignOutCommand>, SignOutCommandHandler>();
        services.AddScoped<IQueryHandler<GetSessionStateQuery, SessionStateDto>, GetSessionStateQueryHandler>();

        services.AddScoped<ICommandHandler<ImportDataCommand, DataImportResult>, DataImportService>();

        services.AddScoped<IQueryHandler<GetCountriesQuery, IReadOnlyList<CountryDto>>, CountriesLeaguesQueryService>();
        services.AddScoped<IQueryHandler<GetCountriesWithLeagueCountQuery, IReadOnlyList<CountryLeagueListItemDto>>, CountriesLeaguesQueryService>();
        services.AddScoped<IQueryHandler<GetCountrySnapshotQuery, CountryLeagueSnapshotDto>, CountriesLeaguesQueryService>();
        services.AddScoped<IQueryHandler<GetLeaguesQuery, IReadOnlyList<LeagueDto>>, CountriesLeaguesQueryService>();

        services.AddScoped<IQueryHandler<TeamSearchQuery, PagedResult<TeamListItemDto>>, TeamsQueryService>();
        services.AddScoped<IQueryHandler<GetTeamDetailsQuery, TeamDetailsDto?>, TeamsQueryService>();
        services.AddScoped<IQueryHandler<PlayerSearchQuery, PagedResult<PlayerListItemDto>>, PlayersQueryService>();
        services.AddScoped<IQueryHandler<GetPlayerDetailsQuery, PlayerDetailsDto?>, PlayersQueryService>();
        services.AddScoped<IQueryHandler<GetPlayerTrendQuery, IReadOnlyList<PlayerTrendPointDto>>, AnalyticsQueryService>();
        services.AddScoped<IQueryHandler<TopPlayersQuery, IReadOnlyList<TopPlayerDto>>, AnalyticsQueryService>();

        services.AddScoped<IQueryHandler<MatchSearchQuery, PagedResult<MatchListItemDto>>, MatchesQueryService>();
        services.AddScoped<IQueryHandler<GetMatchTeamsQuery, IReadOnlyList<TeamListItemDto>>, MatchesQueryService>();
        services.AddScoped<IQueryHandler<GetMatchDetailsQuery, MatchDetailsDto?>, MatchesQueryService>();
        services.AddScoped<IQueryHandler<GetMatchesBySeasonQuery, IReadOnlyList<MatchesBySeasonDto>>, MatchesQueryService>();
        services.AddScoped<IQueryHandler<GetDashboardKpiQuery, DashboardKpiDto>, MatchesQueryService>();

        return services;
    }
}
