using ProFootball.Application.Abstractions.Cqrs;

namespace ProFootball.Application.Auth.Commands;

public sealed record SignInCommand(string Login, string Password) : ICommand;
