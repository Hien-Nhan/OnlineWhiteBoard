using System;
using System.Collections.Generic;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using WhiteboardClient.Models;
using WhiteboardClient.Services;
using Color = Windows.UI.Color;

namespace WhiteboardClient.Views;

/// <summary>
/// Phần quản lý công cụ vẽ: Chọn công cụ, chọn màu từ bảng màu / ColorPicker, đổi cỡ nét.
/// Phụ trách: Thành viên B (Hạng mục T8)
/// </summary>
public sealed partial class BoardPage
{
    private void ToolButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton { Tag: string tag } && Enum.TryParse(tag, out DrawTool tool))
        {
            SelectTool(tool);
        }
    }

    private void SelectTool(DrawTool tool)
    {
        _tool = tool;
        _isDrawing = false;
        _isDrawingShape = false;

        // Chỉ một công cụ được bật; bấm lại công cụ đang chọn thì vẫn giữ trạng thái bật
        foreach (KeyValuePair<DrawTool, ToggleButton> kv in _toolButtons)
        {
            kv.Value.IsChecked = kv.Key == tool;
        }

        Surface.SetCursorShape(tool == DrawTool.Pan ? InputSystemCursorShape.SizeAll : InputSystemCursorShape.Cross);

        UpdateToolInfo();
        BoardCanvas.Invalidate();
    }

    private void PaletteView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_ready) return;
        if (PaletteView.SelectedItem is ColorSwatch swatch)
        {
            SetColor(swatch.Color, fromPalette: true);
        }
    }

    private void CustomColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        if (!_ready) return;
        SetColor(args.NewColor, fromPalette: false);
    }

    private void SetColor(Color c, bool fromPalette)
    {
        _color = c;

        // Màu tuỳ chọn thì bỏ đánh dấu trong bảng màu nhanh
        if (!fromPalette) PaletteView.SelectedIndex = -1;

        ColorPreview.Background = new SolidColorBrush(c);
        ColorPreviewText.Text = HexColor.ToHex(c);
        double luminance = 0.299 * c.R + 0.587 * c.G + 0.114 * c.B;
        ColorPreviewText.Foreground = new SolidColorBrush(luminance > 150 ? Colors.Black : Colors.White);

        if (_tool == DrawTool.Eraser) SelectTool(DrawTool.Pen);
        UpdateToolInfo();
    }

    private void SizeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (!_ready) return;
        _brush = (int)Math.Round(SizeSlider.Value);
        SizeText.Text = _brush + " px";
        UpdateToolInfo();
    }

    private void UpdateToolInfo()
    {
        if (!_ready && ToolInfoText == null) return;

        string name = _tool switch
        {
            DrawTool.Pen => "Bút vẽ",
            DrawTool.Eraser => "Tẩy",
            DrawTool.Line => "Đường thẳng",
            DrawTool.Rectangle => "Hình chữ nhật",
            DrawTool.Pan => "Di chuyển bảng",
            _ => "Hình tròn"
        };
        ToolInfoText.Text = $"Công cụ: {name}   |   Cỡ nét: {_brush} px   |   Màu: {HexColor.ToHex(_color)}   |   Cuộn chuột: zoom   |   Chuột phải/giữa: kéo bảng";
    }
}
