using System;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using WhiteboardClient.Models;
using Windows.UI;
using Color = Windows.UI.Color;

namespace WhiteboardClient.Views;

public sealed partial class BoardPage
{
    private void DrawShapeOnBoard(
        DrawTool tool,
        BoardPoint a,
        BoardPoint b,
        Color color,
        int size)
    {
        if (_board == null) return;

        using CanvasDrawingSession ds = _board.CreateDrawingSession();
        StrokeShape(ds, tool, a, b, color, size, dashed: false);
    }

    private static void StrokeShape(
        CanvasDrawingSession ds,
        DrawTool tool,
        BoardPoint a,
        BoardPoint b,
        Color color,
        int size,
        bool dashed)
    {
        ds.Antialiasing = CanvasAntialiasing.Antialiased;

        using var style = new CanvasStrokeStyle
        {
            DashStyle = dashed
                ? CanvasDashStyle.Dash
                : CanvasDashStyle.Solid
        };

        if (tool == DrawTool.Line)
        {
            style.StartCap = CanvasCapStyle.Round;
            style.EndCap = CanvasCapStyle.Round;

            ds.DrawLine(a.X, a.Y, b.X, b.Y, color, size, style);
            return;
        }

        int x = Math.Min(a.X, b.X);
        int y = Math.Min(a.Y, b.Y);
        int w = Math.Abs(a.X - b.X);
        int h = Math.Abs(a.Y - b.Y);

        if (w == 0 || h == 0) return;

        if (tool == DrawTool.Rectangle)
        {
            ds.DrawRectangle(x, y, w, h, color, size, style);
        }
        else if (tool == DrawTool.Circle)
        {
            ds.DrawEllipse(
                x + w / 2f,
                y + h / 2f,
                w / 2f,
                h / 2f,
                color,
                size,
                style);
        }
    }
}