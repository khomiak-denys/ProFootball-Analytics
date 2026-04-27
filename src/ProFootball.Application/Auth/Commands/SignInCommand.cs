using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Common;

namespace ProFootball.Application.Auth.Commands;

public sealed record SignInCommand(string Login, string Password) : ICommand<Result>;
