using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Common;

namespace ProFootball.Application.Auth.Commands;

public sealed record RegisterUserCommand(
    string FirstName,
    string LastName,
    string Login,
    string Password,
    string ConfirmPassword) : ICommand<Result>;
