using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Country.Dtos;

namespace ProFootball.Application.Country.Queries;

public sealed record GetCountriesQuery : IQuery<IReadOnlyList<CountryDto>>;
