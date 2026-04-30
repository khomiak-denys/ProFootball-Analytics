using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Common;

namespace ProFootball.Application.Player.Commands;

public sealed record CreatePlayerCommand(
    string FirstName,
    string LastName,
    DateTime? Birthday,
    int? Height,
    int? Weight) : ICommand<Result>;

public sealed record UpdatePlayerCommand(
    int PlayerId,
    string FirstName,
    string LastName,
    DateTime? Birthday,
    int? Height,
    int? Weight) : ICommand<Result>;

public sealed record DeletePlayerCommand(int PlayerId) : ICommand<Result>;
