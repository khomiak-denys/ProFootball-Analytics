using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Common;

namespace ProFootball.Application.Auth.Commands;

public sealed record RestoreSessionCommand : ICommand<Result>;
