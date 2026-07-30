using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using MeiHuaPuChess.Core.Enums;
using MeiHuaPuChess.Core.Engine;
using MeiHuaPuChess.Core.Models;

namespace MeiHuaPuChess.App.Views;

/// <summary>
/// 仿天天象棋精美棋盘控件
/// </summary>
public partial class ChessBoardView : UserControl
{
    private const int Rows = 10;
    private const int Cols = 9;
    private const double PieceRatio = 0.88;

    // ============ 棋盘配色 ============
    // 棋盘面 — 暖木色
    private static readonly Color BoardFaceColor = Color.FromRgb(0xEE, 0xD8, 0xB0);
    // 棋盘面暗纹
    private static readonly Color BoardStripeColor = Color.FromRgb(0xE8, 0xD0, 0xA5);
    // 外框 — 深胡桃木
    private static readonly Color FrameDarkColor = Color.FromRgb(0x4A, 0x2F, 0x1A);
    private static readonly Color FrameLightColor = Color.FromRgb(0x6B, 0x4E, 0x3D);
    // 网格线
    private static readonly Color GridLineColor = Color.FromRgb(0x5C, 0x3D, 0x2E);
    private static readonly Color GridLineThinColor = Color.FromRgb(0x7A, 0x5C, 0x4A);
    // 河界
    private static readonly Color RiverBgColor = Color.FromRgb(0xE2, 0xCA, 0xA0);
    // 棋子
    private static readonly Color PieceTopColor = Color.FromRgb(0xFF, 0xFC, 0xF5);
    private static readonly Color PieceMidColor = Color.FromRgb(0xF0, 0xE6, 0xD0);
    private static readonly Color PieceBottomColor = Color.FromRgb(0xD4, 0xC4, 0xA8);
    private static readonly Color PieceRimTopColor = Color.FromRgb(0xB8, 0x9A, 0x70);
    private static readonly Color PieceRimBottomColor = Color.FromRgb(0x7A, 0x5C, 0x40);
    // 红方文字
    private static readonly Color RedTextColor = Color.FromRgb(0xC4, 0x1E, 0x3A);
    // 黑方文字
    private static readonly Color BlackTextColor = Color.FromRgb(0x1A, 0x1A, 0x2E);
    // 选中
    private static readonly Color SelectedColor = Color.FromRgb(0xFF, 0xB3, 0x00);
    // 合法走法
    private static readonly Color LegalMoveColor = Color.FromRgb(0x66, 0xBB, 0x6A);
    private static readonly Color CaptureHintColor = Color.FromRgb(0xEF, 0x53, 0x50);
    // 最后一步
    private static readonly Color LastMoveFromColor = Color.FromRgb(0xFF, 0xE0, 0x82);
    private static readonly Color LastMoveToColor = Color.FromRgb(0xFF, 0xCC, 0x02);

    private readonly Canvas _boardCanvas = new() { Background = Brushes.Transparent };
    private readonly Dictionary<(int, int), Border> _pieceElements = new();
    private readonly List<UIElement> _overlayElements = new();

    public GameEngine? Engine { get; set; }

    public static readonly DependencyProperty IsReadOnlyProperty =
        DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(ChessBoardView),
            new PropertyMetadata(false));

    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    /// <summary>局面编辑模式</summary>
    public bool IsEditMode { get; set; }

    public Side EditingSide { get; set; } = Side.Red;

    private (int Row, int Col)? _selectedPiece;
    private List<(int Row, int Col)> _legalMoves = new();

    public event Action<int, int, int, int>? OnPlayerMove;
    /// <summary>编辑模式下棋子移动事件</summary>
    public event Action<int, int, int, int>? OnEditMove;

    private double _cellSize;
    private double _offsetX;
    private double _offsetY;

    public ChessBoardView()
    {
        InitializeComponent();

        // 深色木框包裹
        Content = new Border
        {
            Background = new SolidColorBrush(FrameDarkColor),
            BorderThickness = new Thickness(10),
            BorderBrush = new SolidColorBrush(FrameLightColor),
            CornerRadius = new CornerRadius(6),
            Child = new Border
            {
                // 内框细线装饰
                BorderThickness = new Thickness(2),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x3D, 0x2B, 0x1F)),
                CornerRadius = new CornerRadius(3),
                Child = new Viewbox
                {
                    Child = _boardCanvas,
                    Width = 540,
                    Height = 600,
                    Stretch = Stretch.Uniform
                }
            }
        };

        _boardCanvas.Width = 540;
        _boardCanvas.Height = 600;
        _boardCanvas.MouseDown += OnBoardClick;

        Loaded += (s, e) => DrawBoard();
        SizeChanged += (s, e) => DrawBoard();
    }

    // ================================================================
    //  主绘制入口
    // ================================================================

    public void DrawBoard()
    {
        _boardCanvas.Children.Clear();
        _pieceElements.Clear();
        _overlayElements.Clear();

        double w = _boardCanvas.Width;
        double h = _boardCanvas.Height;

        _cellSize = Math.Min(w / (Cols + 1), h / (Rows + 1));
        _offsetX = (w - _cellSize * (Cols - 1)) / 2.0;
        _offsetY = (h - _cellSize * (Rows - 1)) / 2.0;

        DrawBackground();
        DrawWoodGrain();
        DrawGridLines();
        DrawStarPoints();
        DrawPalaceDiagonals();
        DrawRiverArea();
        DrawCoordinateLabels();
        DrawPieces();
    }

    // ================================================================
    //  棋盘背景
    // ================================================================

    private void DrawBackground()
    {
        double margin = _cellSize * 0.55;
        var bg = new Rectangle
        {
            Width = _cellSize * (Cols - 1) + margin * 2,
            Height = _cellSize * (Rows - 1) + margin * 2,
            Fill = new SolidColorBrush(BoardFaceColor),
            RadiusX = 3,
            RadiusY = 3
        };
        Canvas.SetLeft(bg, _offsetX - margin);
        Canvas.SetTop(bg, _offsetY - margin);
        _boardCanvas.Children.Add(bg);
    }

    /// <summary>模拟木纹暗纹</summary>
    private void DrawWoodGrain()
    {
        double startX = _offsetX;
        double startY = _offsetY;
        double totalW = _cellSize * (Cols - 1);
        double totalH = _cellSize * (Rows - 1);

        // 横向木纹
        for (int i = 0; i < 20; i++)
        {
            double y = startY + i * totalH / 20;
            var stripe = new Rectangle
            {
                Width = totalW,
                Height = totalH / 20 * 0.3,
                Fill = new SolidColorBrush(BoardStripeColor),
                Opacity = 0.25,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(stripe, startX);
            Canvas.SetTop(stripe, y);
            _boardCanvas.Children.Add(stripe);
        }
    }

    // ================================================================
    //  网格线
    // ================================================================

    private void DrawGridLines()
    {
        double startX = _offsetX;
        double startY = _offsetY;

        // 横线 (10条)
        for (int row = 0; row < Rows; row++)
        {
            double y = startY + row * _cellSize;
            double x1 = startX;
            double x2 = startX + (Cols - 1) * _cellSize;

            bool isEdge = row == 0 || row == Rows - 1;
            var line = new Line
            {
                X1 = x1, Y1 = y,
                X2 = x2, Y2 = y,
                Stroke = new SolidColorBrush(GridLineColor),
                StrokeThickness = isEdge ? 1.6 : 1.0,
                SnapsToDevicePixels = true
            };
            _boardCanvas.Children.Add(line);
        }

        // 竖线
        for (int col = 0; col < Cols; col++)
        {
            double x = startX + col * _cellSize;
            bool isEdge = col == 0 || col == Cols - 1;

            if (isEdge)
            {
                var line = new Line
                {
                    X1 = x, Y1 = startY,
                    X2 = x, Y2 = startY + (Rows - 1) * _cellSize,
                    Stroke = new SolidColorBrush(GridLineColor),
                    StrokeThickness = 1.8,
                    SnapsToDevicePixels = true
                };
                _boardCanvas.Children.Add(line);
            }
            else
            {
                AddLine(x, startY, x, startY + 4 * _cellSize, 1.0);
                AddLine(x, startY + 5 * _cellSize, x, startY + 9 * _cellSize, 1.0);
            }
        }
    }

    private void AddLine(double x1, double y1, double x2, double y2, double thickness)
    {
        _boardCanvas.Children.Add(new Line
        {
            X1 = x1, Y1 = y1,
            X2 = x2, Y2 = y2,
            Stroke = new SolidColorBrush(GridLineThinColor),
            StrokeThickness = thickness,
            SnapsToDevicePixels = true
        });
    }

    // ================================================================
    //  星位标记（四角交叉标记）
    // ================================================================

    private void DrawStarPoints()
    {
        // 传统星位：双方各4个，位于兵/卒和炮/砲的起始列
        // 红方星位: (3,0),(3,2),(3,4),(3,6),(3,8) 兵位 + (2,1),(2,7) 炮位
        // 黑方星位: (6,0),(6,2),(6,4),(6,6),(6,8) 卒位 + (7,1),(7,7) 炮位

        int[] redRows = { 3, 2, 2 };
        int[] redCols = { 0, 1, 7 };
        int[] blackRows = { 6, 7, 7 };
        int[] blackCols = { 0, 1, 7 };

        // 兵/卒位
        for (int c = 0; c <= 8; c += 2)
        {
            DrawStarMark(3, c, false);
            DrawStarMark(6, c, false);
        }

        // 炮位（仅左右两角）
        DrawStarMark(2, 1, true);
        DrawStarMark(2, 7, true);
        DrawStarMark(7, 1, true);
        DrawStarMark(7, 7, true);
    }

    private void DrawStarMark(int row, int col, bool isCorner)
    {
        double cx = _offsetX + col * _cellSize;
        double cy = _offsetY + row * _cellSize;
        double gap = _cellSize * 0.12;
        double len = _cellSize * 0.08;
        double thick = 0.8;

        Brush brush = new SolidColorBrush(GridLineColor) { Opacity = 0.7 };

        // 四个小线段围成十字缺口
        if (col > 0)
        {
            // 左上
            AddMarkLine(cx - gap, cy - gap, cx - gap - len, cy - gap, thick, brush);
            AddMarkLine(cx - gap, cy - gap, cx - gap, cy - gap - len, thick, brush);
            // 左下
            AddMarkLine(cx - gap, cy + gap, cx - gap - len, cy + gap, thick, brush);
            AddMarkLine(cx - gap, cy + gap, cx - gap, cy + gap + len, thick, brush);
        }
        if (col < 8)
        {
            // 右上
            AddMarkLine(cx + gap, cy - gap, cx + gap + len, cy - gap, thick, brush);
            AddMarkLine(cx + gap, cy - gap, cx + gap, cy - gap - len, thick, brush);
            // 右下
            AddMarkLine(cx + gap, cy + gap, cx + gap + len, cy + gap, thick, brush);
            AddMarkLine(cx + gap, cy + gap, cx + gap, cy + gap + len, thick, brush);
        }
    }

    private void AddMarkLine(double x1, double y1, double x2, double y2, double t, Brush b)
    {
        _boardCanvas.Children.Add(new Line
        {
            X1 = x1, Y1 = y1,
            X2 = x2, Y2 = y2,
            Stroke = b,
            StrokeThickness = t,
            SnapsToDevicePixels = true
        });
    }

    // ================================================================
    //  九宫对角线
    // ================================================================

    private void DrawPalaceDiagonals()
    {
        double sx = _offsetX;
        double sy = _offsetY;

        var dashBrush = new SolidColorBrush(GridLineThinColor) { Opacity = 0.55 };

        // 红方九宫
        DrawDiagonalLine(sx + 3 * _cellSize, sy, sx + 5 * _cellSize, sy + 2 * _cellSize, dashBrush);
        DrawDiagonalLine(sx + 5 * _cellSize, sy, sx + 3 * _cellSize, sy + 2 * _cellSize, dashBrush);

        // 黑方九宫
        DrawDiagonalLine(sx + 3 * _cellSize, sy + 7 * _cellSize, sx + 5 * _cellSize, sy + 9 * _cellSize, dashBrush);
        DrawDiagonalLine(sx + 5 * _cellSize, sy + 7 * _cellSize, sx + 3 * _cellSize, sy + 9 * _cellSize, dashBrush);
    }

    private void DrawDiagonalLine(double x1, double y1, double x2, double y2, Brush brush)
    {
        _boardCanvas.Children.Add(new Line
        {
            X1 = x1, Y1 = y1,
            X2 = x2, Y2 = y2,
            Stroke = brush,
            StrokeThickness = 0.8,
            StrokeDashArray = new DoubleCollection { 4, 4 },
            SnapsToDevicePixels = true
        });
    }

    // ================================================================
    //  楚河汉界
    // ================================================================

    private void DrawRiverArea()
    {
        double sx = _offsetX;
        double sy = _offsetY;
        double riverY = sy + 4 * _cellSize;
        double totalW = _cellSize * (Cols - 1);

        // 河界底色
        var riverBg = new Rectangle
        {
            Width = totalW,
            Height = _cellSize,
            Fill = new SolidColorBrush(RiverBgColor),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(riverBg, sx);
        Canvas.SetTop(riverBg, riverY);
        Panel.SetZIndex(riverBg, -1);
        _boardCanvas.Children.Add(riverBg);

        // 河界上下边框线（略微加粗）
        var topLine = new Rectangle
        {
            Width = totalW,
            Height = 1.5,
            Fill = new SolidColorBrush(GridLineColor),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(topLine, sx);
        Canvas.SetTop(topLine, riverY);
        _boardCanvas.Children.Add(topLine);

        var bottomLine = new Rectangle
        {
            Width = totalW,
            Height = 1.5,
            Fill = new SolidColorBrush(GridLineColor),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(bottomLine, sx);
        Canvas.SetTop(bottomLine, riverY + _cellSize);
        _boardCanvas.Children.Add(bottomLine);

        // 文字
        var textBrush = new SolidColorBrush(GridLineColor);
        double fontSize = _cellSize * 0.48;

        // "楚 河" — 左半
        var chuHe = new TextBlock
        {
            Text = "楚   河",
            FontSize = fontSize,
            FontFamily = new FontFamily("KaiTi, DFKai-SB, SimSun, serif"),
            FontWeight = FontWeights.Bold,
            Foreground = textBrush,
            TextAlignment = TextAlignment.Center
        };
        Canvas.SetLeft(chuHe, sx + _cellSize * 0.3);
        Canvas.SetTop(chuHe, riverY + _cellSize * 0.22);
        _boardCanvas.Children.Add(chuHe);

        // "漢 界" — 右半
        var hanJie = new TextBlock
        {
            Text = "漢   界",
            FontSize = fontSize,
            FontFamily = new FontFamily("KaiTi, DFKai-SB, SimSun, serif"),
            FontWeight = FontWeights.Bold,
            Foreground = textBrush,
            TextAlignment = TextAlignment.Center
        };
        Canvas.SetLeft(hanJie, sx + _cellSize * 4.5);
        Canvas.SetTop(hanJie, riverY + _cellSize * 0.22);
        _boardCanvas.Children.Add(hanJie);
    }

    // ================================================================
    //  坐标标注（列号）
    // ================================================================

    private void DrawCoordinateLabels()
    {
        // 红方列号（底部）：一二三四五六七八九
        string[] redCols = { "九", "八", "七", "六", "五", "四", "三", "二", "一" };
        string[] blackCols = { "1", "2", "3", "4", "5", "6", "7", "8", "9" };

        double labelSize = _cellSize * 0.32;
        var labelBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x6A, 0x5A));
        var font = new FontFamily("KaiTi, SimSun");

        for (int col = 0; col < Cols; col++)
        {
            double x = _offsetX + col * _cellSize;

            // 红方侧（顶部）
            var redLabel = new TextBlock
            {
                Text = redCols[col],
                FontSize = labelSize,
                FontFamily = font,
                Foreground = labelBrush,
                TextAlignment = TextAlignment.Center,
                Opacity = 0.6
            };
            Canvas.SetLeft(redLabel, x - labelSize);
            Canvas.SetTop(redLabel, _offsetY - _cellSize * 0.55);
            redLabel.Width = labelSize * 2;
            _boardCanvas.Children.Add(redLabel);

            // 黑方侧（底部）
            var blackLabel = new TextBlock
            {
                Text = blackCols[col],
                FontSize = labelSize,
                FontFamily = font,
                Foreground = labelBrush,
                TextAlignment = TextAlignment.Center,
                Opacity = 0.6
            };
            Canvas.SetLeft(blackLabel, x - labelSize);
            Canvas.SetTop(blackLabel, _offsetY + 9 * _cellSize + _cellSize * 0.2);
            blackLabel.Width = labelSize * 2;
            _boardCanvas.Children.Add(blackLabel);
        }
    }

    // ================================================================
    //  绘制棋子
    // ================================================================

    public void DrawPieces()
    {
        foreach (var (_, border) in _pieceElements)
            _boardCanvas.Children.Remove(border);
        _pieceElements.Clear();

        if (Engine?.Board == null) return;

        foreach (var piece in Engine.Board.GetAllAlivePieces())
            DrawPiece(piece);

        DrawOverlays();
    }

    private void DrawPiece(ChessPiece piece)
    {
        double x = _offsetX + piece.Col * _cellSize;
        double y = _offsetY + piece.Row * _cellSize;
        double pieceSize = _cellSize * PieceRatio;

        // 外容器
        var border = new Border
        {
            Width = pieceSize,
            Height = pieceSize,
            CornerRadius = new CornerRadius(pieceSize / 2),
            Tag = $"{piece.Row},{piece.Col}"
        };

        var grid = new Grid();
        double inset = pieceSize * 0.04;

        // 第1层：棋子主体
        var body = new Ellipse
        {
            Width = pieceSize - inset * 2,
            Height = pieceSize - inset * 2
        };

        // 主体渐变 — 左上高光 → 右下暗面
        var bodyGradient = new RadialGradientBrush
        {
            GradientOrigin = new Point(0.33, 0.28),
            Center = new Point(0.33, 0.28),
            RadiusX = 0.75,
            RadiusY = 0.75
        };
        bodyGradient.GradientStops.Add(new GradientStop(PieceTopColor, 0.0));
        bodyGradient.GradientStops.Add(new GradientStop(Color.FromRgb(0xF8, 0xF2, 0xE4), 0.35));
        bodyGradient.GradientStops.Add(new GradientStop(PieceMidColor, 0.7));
        bodyGradient.GradientStops.Add(new GradientStop(PieceBottomColor, 1.0));
        body.Fill = bodyGradient;

        // 主体边框 — 上下渐变模拟金属包边
        var rimGradient = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1)
        };
        rimGradient.GradientStops.Add(new GradientStop(PieceRimTopColor, 0));
        rimGradient.GradientStops.Add(new GradientStop(Color.FromRgb(0xC8, 0xB0, 0x90), 0.3));
        rimGradient.GradientStops.Add(new GradientStop(PieceRimBottomColor, 1));
        body.Stroke = rimGradient;
        body.StrokeThickness = 1.8;

        // 第4层：内圈装饰线（细金线）
        var innerRing = new Ellipse
        {
            Width = (pieceSize - inset * 2) * 0.82,
            Height = (pieceSize - inset * 2) * 0.82,
            Stroke = new SolidColorBrush(Color.FromArgb(0x50, 0xC0, 0xA0, 0x70)),
            StrokeThickness = 0.6
        };

        // 第5层：文字
        var textColor = piece.Side == Side.Red
            ? new SolidColorBrush(RedTextColor)
            : new SolidColorBrush(BlackTextColor);

        var text = new TextBlock
        {
            Text = piece.DisplayChar,
            FontSize = pieceSize * 0.52,
            FontFamily = new FontFamily("KaiTi, DFKai-SB, SimSun, Microsoft YaHei"),
            FontWeight = FontWeights.Bold,
            Foreground = textColor,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            RenderTransform = new TranslateTransform(0, -1)
        };

        grid.Children.Add(body);
        grid.Children.Add(innerRing);
        grid.Children.Add(text);
        border.Child = grid;

        Canvas.SetLeft(border, x - pieceSize / 2);
        Canvas.SetTop(border, y - pieceSize / 2);
        Panel.SetZIndex(border, 10);

        _boardCanvas.Children.Add(border);
        _pieceElements[(piece.Row, piece.Col)] = border;
    }

    // ================================================================
    //  点击处理
    // ================================================================

    private void OnBoardClick(object sender, MouseButtonEventArgs e)
    {
        if (IsReadOnly && !IsEditMode) return;

        var pos = e.GetPosition(_boardCanvas);
        var (row, col) = PixelToBoard(pos.X, pos.Y);

        if (row < 0 || row >= Rows || col < 0 || col >= Cols) return;

        // 编辑模式：点击选中 → 点击目标 → 触发移动事件
        if (IsEditMode)
        {
            HandleEditClick(row, col);
            return;
        }

        if (_selectedPiece == null)
        {
            SelectPiece(row, col);
        }
        else
        {
            var from = _selectedPiece.Value;
            if (_legalMoves.Contains((row, col)))
            {
                OnPlayerMove?.Invoke(from.Row, from.Col, row, col);
                ClearSelection();
            }
            else if (from.Row == row && from.Col == col)
            {
                ClearSelection();
            }
            else
            {
                ClearSelection();
                SelectPiece(row, col);
            }
        }
    }

    private void HandleEditClick(int row, int col)
    {
        var piece = Engine?.Board?[row, col];

        if (_selectedPiece == null)
        {
            // 点击棋子 → 选中
            if (piece != null)
            {
                _selectedPiece = (row, col);
                DrawOverlays();
            }
        }
        else
        {
            var from = _selectedPiece.Value;
            _selectedPiece = null;
            DrawOverlays();

            // 点击同一位置 → 取消选中
            if (from.Row == row && from.Col == col) return;

            // 移动棋子（不论目标是空还是有子）
            OnEditMove?.Invoke(from.Row, from.Col, row, col);
        }
    }

    private void SelectPiece(int row, int col)
    {
        if (IsReadOnly && !IsEditMode) return;

        var piece = Engine?.Board?[row, col];
        if (piece == null) return;

        if (!IsEditMode && Engine?.Phase == GamePhase.BlackTurn && piece.Side != Side.Black) return;

        _selectedPiece = (row, col);

        if (Engine != null)
        {
            var generator = new MoveGenerator(Engine.Board);
            _legalMoves = generator.GetLegalMovesForPiece(row, col);
        }

        DrawOverlays();
    }

    private void ClearSelection()
    {
        _selectedPiece = null;
        _legalMoves.Clear();
        DrawOverlays();
    }

    public void ClearSelectionPublic() => ClearSelection();

    // ================================================================
    //  覆盖层（选中、合法走法、最近一步）
    // ================================================================

    private void DrawOverlays()
    {
        foreach (var elem in _overlayElements)
            _boardCanvas.Children.Remove(elem);
        _overlayElements.Clear();

        // 最后一步标记
        if (Engine?.MoveHistory.Count > 0)
        {
            var lastMove = Engine.MoveHistory[^1];
            DrawLastMoveHighlight(lastMove.FromRow, lastMove.FromCol, false);
            DrawLastMoveHighlight(lastMove.ToRow, lastMove.ToCol, true);
        }

        // 合法走法
        foreach (var (row, col) in _legalMoves)
        {
            bool isCapture = Engine?.Board?[row, col] != null;
            DrawLegalMoveIndicator(row, col, isCapture);
        }

        // 选中高亮
        if (_selectedPiece != null)
        {
            var (row, col) = _selectedPiece.Value;
            DrawSelectionGlow(row, col);
        }
    }

    /// <summary>选中棋子的金色光环</summary>
    private void DrawSelectionGlow(int row, int col)
    {
        double x = _offsetX + col * _cellSize;
        double y = _offsetY + row * _cellSize;
        double size = _cellSize * PieceRatio + 8;

        var glow = new Ellipse
        {
            Width = size,
            Height = size,
            Stroke = new SolidColorBrush(SelectedColor),
            StrokeThickness = 3.5,
            Opacity = 0.75
        };

        Canvas.SetLeft(glow, x - size / 2);
        Canvas.SetTop(glow, y - size / 2);
        Panel.SetZIndex(glow, 15);
        _boardCanvas.Children.Add(glow);
        _overlayElements.Add(glow);
    }

    /// <summary>合法走法指示</summary>
    private void DrawLegalMoveIndicator(int row, int col, bool isCapture)
    {
        double x = _offsetX + col * _cellSize;
        double y = _offsetY + row * _cellSize;

        if (isCapture)
        {
            // 吃子目标：红色宽环
            double size = _cellSize * PieceRatio + 4;
            var ring = new Ellipse
            {
                Width = size,
                Height = size,
                Stroke = new SolidColorBrush(CaptureHintColor),
                StrokeThickness = 3,
                Opacity = 0.65,
                StrokeDashArray = new DoubleCollection { 4, 2 }
            };
            Canvas.SetLeft(ring, x - size / 2);
            Canvas.SetTop(ring, y - size / 2);
            Panel.SetZIndex(ring, 14);
            _boardCanvas.Children.Add(ring);
            _overlayElements.Add(ring);
        }
        else
        {
            // 移动目标：绿色小圆点
            double dotSize = _cellSize * 0.24;
            var dot = new Ellipse
            {
                Width = dotSize,
                Height = dotSize,
                Fill = new SolidColorBrush(LegalMoveColor),
                Opacity = 0.6
            };
            Canvas.SetLeft(dot, x - dotSize / 2);
            Canvas.SetTop(dot, y - dotSize / 2);
            Panel.SetZIndex(dot, 14);
            _boardCanvas.Children.Add(dot);
            _overlayElements.Add(dot);
        }
    }

    /// <summary>最后一步高亮方块</summary>
    private void DrawLastMoveHighlight(int row, int col, bool isDestination)
    {
        double x = _offsetX + col * _cellSize;
        double y = _offsetY + row * _cellSize;
        double size = _cellSize * 0.55;

        var color = isDestination ? LastMoveToColor : LastMoveFromColor;
        double opacity = isDestination ? 0.4 : 0.3;

        var marker = new Rectangle
        {
            Width = size,
            Height = size,
            Fill = new SolidColorBrush(color),
            Opacity = opacity,
            RadiusX = 4,
            RadiusY = 4
        };

        Canvas.SetLeft(marker, x - size / 2);
        Canvas.SetTop(marker, y - size / 2);
        Panel.SetZIndex(marker, 1);
        _boardCanvas.Children.Add(marker);
        _overlayElements.Add(marker);
    }

    // ================================================================
    //  坐标转换
    // ================================================================

    private (int row, int col) PixelToBoard(double px, double py)
    {
        int col = (int)Math.Round((px - _offsetX) / _cellSize);
        int row = (int)Math.Round((py - _offsetY) / _cellSize);

        double xDist = Math.Abs(px - (_offsetX + col * _cellSize));
        double yDist = Math.Abs(py - (_offsetY + row * _cellSize));

        if (xDist > _cellSize * 0.5 || yDist > _cellSize * 0.5)
        {
            col = Math.Clamp(col, 0, Cols - 1);
            row = Math.Clamp(row, 0, Rows - 1);

            xDist = Math.Abs(px - (_offsetX + col * _cellSize));
            yDist = Math.Abs(py - (_offsetY + row * _cellSize));
            if (xDist > _cellSize * 0.6 || yDist > _cellSize * 0.6)
                return (-1, -1);
        }

        col = Math.Clamp(col, 0, Cols - 1);
        row = Math.Clamp(row, 0, Rows - 1);

        return (row, col);
    }
}
