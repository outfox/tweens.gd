// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using Godot;

namespace playground;

internal static class PlaygroundTheme
{
    public static readonly Color Background = new("#0b0f14"), Panel = new("#121b24"), Stage = new("#0e151d"),
        Border = new("#293946"), Text = new("#e2edf3"), Muted = new("#8d9daa"),
        Mint = new("#73e2bd"), Amber = new("#ffc078"), Blue = new("#88bfff");

    public static StyleBoxFlat Box(Color color, int padding = 16, bool border = true)
    {
        return new StyleBoxFlat
        {
            BgColor = color, BorderColor = Border,
            BorderWidthLeft = border ? 1 : 0, BorderWidthRight = border ? 1 : 0,
            BorderWidthTop = border ? 1 : 0, BorderWidthBottom = border ? 1 : 0,
            CornerRadiusTopLeft = 12, CornerRadiusTopRight = 12,
            CornerRadiusBottomLeft = 12, CornerRadiusBottomRight = 12,
            ContentMarginLeft = padding, ContentMarginRight = padding,
            ContentMarginTop = padding, ContentMarginBottom = padding,
        };
    }

    public static Theme Create()
    {
        var theme = new Theme { DefaultFontSize = 15 };
        foreach (var type in new[] { "Label", "Button", "OptionButton", "TextEdit" })
            theme.SetColor("font_color", type, Text);
        foreach (var type in new[] { "Button", "OptionButton" })
        {
            theme.SetStylebox("normal", type, Box(Stage, 12));
            theme.SetStylebox("hover", type, Box(new Color("#223441"), 12));
            theme.SetStylebox("pressed", type, Box(new Color("#2d4851"), 12));
            theme.SetStylebox("disabled", type, Box(Stage, 12));
            var focus = Box(Colors.Transparent, 0);
            focus.BorderColor = Mint;
            theme.SetStylebox("focus", type, focus);
            theme.SetColor("font_hover_color", type, Mint);
            theme.SetColor("font_pressed_color", type, Mint);
            theme.SetColor("font_disabled_color", type, Muted with { A = 0.45f });
        }
        theme.SetStylebox("panel", "PanelContainer", Box(Panel));
        theme.SetStylebox("normal", "TextEdit", Box(Stage));
        theme.SetStylebox("read_only", "TextEdit", Box(Stage));
        theme.SetColor("font_readonly_color", "TextEdit", Text);
        theme.SetStylebox("slider", "HSlider", new StyleBoxFlat { BgColor = Border, ContentMarginTop = 3, ContentMarginBottom = 3 });
        theme.SetStylebox("grabber_area", "HSlider", new StyleBoxFlat { BgColor = Mint, ContentMarginTop = 3, ContentMarginBottom = 3 });
        theme.SetStylebox("grabber_area_highlight", "HSlider", theme.GetStylebox("grabber_area", "HSlider"));
        theme.SetColor("font_color", "PopupMenu", Text);
        theme.SetStylebox("panel", "PopupMenu", Box(Panel, 8));
        theme.SetConstant("v_separation", "PopupMenu", 6);
        return theme;
    }

    public static Label Label(string text, int size = 15, Color? color = null)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color ?? Text);
        return label;
    }

    public static T Add<T>(this Node parent, T child) where T : Node { parent.AddChild(child); return child; }
}
