// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using NesNes.Core;

namespace NesNes.Host.Web;

internal sealed class DebuggerControls
{
    public required Action Pause { get; init; }
    public required Func<Registers> GetRegisters { get; init; }
}

internal sealed class DebuggerWebApp
{
    private readonly WebApplication _app;
    private readonly DebuggerControls _controls;

    public DebuggerWebApp(DebuggerControls controls)
    {
        _controls = controls;

        var builder = WebApplication.CreateBuilder();
        _app = builder.Build();
    }

    public void Start()
    {
        _app.UseDefaultFiles();
        _app.UseStaticFiles();

        _app.MapPost(
            "/pause",
            (HttpContext context) =>
            {
                _controls.Pause();
                return Results.Ok();
            }
        );

        _app.MapGet(
            "/registers",
            (HttpContext context) =>
            {
                var registers = _controls.GetRegisters();
                var html = $@"<p>{registers}</p>";
                return Results.Content(html, "text/html");
            }
        );

        _app.RunAsync();
    }
}
