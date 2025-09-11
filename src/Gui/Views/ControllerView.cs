// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

using ImGuiNET;
using Silk.NET.Input;

namespace NesNes.Gui.Views;

internal sealed class ControllerView : ClosableWindow
{
    private const string InputLabel = "Press a key...";

    private static readonly string[] s_buttonNames =
    [
        nameof(ControllerState.A),
        nameof(ControllerState.B),
        nameof(ControllerState.Select),
        nameof(ControllerState.Start),
        nameof(ControllerState.Up),
        nameof(ControllerState.Down),
        nameof(ControllerState.Left),
        nameof(ControllerState.Right),
    ];

    private readonly ControllerManager _manager;
    private int _listeningFor = -1;

    public ControllerView(ControllerManager controllerManager)
        : base("Controller", ImGuiWindowFlags.NoResize)
    {
        _manager = controllerManager;
        _manager.Keyboard.KeyDown += SetKey;
    }

    protected override void RenderContent(double _)
    {
        for (var i = 0; i < _manager.Mapping.Length; i++)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text(s_buttonNames[i]);
            ImGui.SameLine();

            if (_listeningFor == i)
            {
                ImGui.Button(InputLabel);
            }
            else if(ImGui.Button(Enum.GetName(_manager.Mapping[i]) ?? "Unknown"))
            {
                _listeningFor = i;
            }
        }
    }

    private void SetKey(IKeyboard _, Key key, int __)
    {
        if (_listeningFor < 0)
        {
            return;
        }

        _manager.Mapping[_listeningFor] = key;
        _listeningFor = -1;
    }
}
