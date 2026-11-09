using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RadialButton
{
    public sealed class WheelForm : Form
    {
        private const int BaseWindowSize = 380;
        private const float BaseOuterRadius = 172.0f;
        private const float BaseSectorInnerRadius = 52.0f;
        private const float BaseCancelRadius = 43.0f;

        // Constant width of the empty separator between neighbouring sectors.
        // Unlike an angular gap, this remains visually parallel from the
        // inner radius to the outer radius.
        private const float BaseSeparatorWidth = 10.0f;

        private const int FadeInDuration = 90;
        private const int FadeOutDuration = 70;

        private const int WsExLayered = 0x00080000;
        private const int UlwAlpha = 0x00000002;
        private const byte AcSrcOver = 0x00;
        private const byte AcSrcAlpha = 0x01;

        private readonly IReadOnlyList<WheelItem> _items;
        private readonly Point _wheelCenterOnScreen;
        private readonly UserInterfaceSettings _interfaceSettings;
        private readonly WheelColorScheme _colorScheme;
        private readonly float _scale;
        private readonly bool _canGoBack;

        private float OuterRadius { get { return BaseOuterRadius * _scale; } }
        private float SectorInnerRadius { get { return BaseSectorInnerRadius * _scale; } }
        private float CancelRadius { get { return BaseCancelRadius * _scale; } }
        private float SeparatorWidth { get { return BaseSeparatorWidth * _scale; } }

        private int _hoveredIndex = -1;
        private bool _cancelHovered;
        private bool _selectionMade;

        private readonly Timer _fadeTimer = new Timer();
        private Bitmap _renderedBitmap;
        private IntPtr _renderedHBitmap = IntPtr.Zero;
        private int _fadeStartedAt;
        private byte _windowAlpha;
        private byte _fadeStartAlpha;
        private bool _fadingOut;
        private bool _allowClose;
        private DialogResult _pendingDialogResult = DialogResult.Cancel;

        public WheelItem SelectedItem { get; private set; }
        public bool BackRequested { get; private set; }

        public WheelForm(
            IReadOnlyList<WheelItem> items,
            Point wheelCenterOnScreen,
            UserInterfaceSettings interfaceSettings,
            bool canGoBack)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (items.Count < 2 || items.Count > 8)
            {
                throw new ArgumentException(
                    "The wheel must contain between two and eight items.",
                    nameof(items));
            }

            _items = items;
            _wheelCenterOnScreen = wheelCenterOnScreen;
            _interfaceSettings = interfaceSettings ?? UserInterfaceSettingsService.CreateDefault();
            _scale = (float)_interfaceSettings.WheelScale;
            _colorScheme = _interfaceSettings.GetSelectedScheme();
            _canGoBack = canGoBack;

            InitializeForm();
            RegisterEvents();

            _fadeTimer.Interval = 15;
            _fadeTimer.Tick += OnFadeTimerTick;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams createParams = base.CreateParams;
                createParams.ExStyle |= WsExLayered;
                return createParams;
            }
        }

        private void InitializeForm()
        {
            Text = "RadialButton";

            Width = (int)Math.Round(BaseWindowSize * _scale);
            Height = (int)Math.Round(BaseWindowSize * _scale);

            StartPosition = FormStartPosition.Manual;

            Location = new Point(
                _wheelCenterOnScreen.X - Width / 2,
                _wheelCenterOnScreen.Y - Height / 2);

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            KeyPreview = true;

            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.ResizeRedraw,
                true);

            UpdateStyles();
        }

        private void RegisterEvents()
        {
            Shown += OnFormShown;
            Deactivate += OnFormDeactivate;
            KeyDown += OnFormKeyDown;
            MouseMove += OnFormMouseMove;
            MouseDown += OnFormMouseDown;
            FormClosing += OnFormClosing;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // A layered window is presented by UpdateLayeredWindow instead of
            // the regular WinForms paint pipeline.
        }

        private void RenderLayer()
        {
            Bitmap bitmap = new Bitmap(
                ClientSize.Width,
                ClientSize.Height,
                // UpdateLayeredWindow expects premultiplied BGRA when
                // AC_SRC_ALPHA is used.
                PixelFormat.Format32bppPArgb);

            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                // Alpha 1 is visually indistinguishable from transparent, but
                // keeps the form's deliberately empty areas in the hit-test
                // surface. A true alpha-zero pixel lets the click fall through
                // a layered window into Revit.
                graphics.Clear(Color.FromArgb(1, 0, 0, 0));

                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.CompositingMode = CompositingMode.SourceOver;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                // ClearType assumes an opaque background and causes coloured
                // fringes on a per-pixel-alpha surface.
                graphics.TextRenderingHint =
                    System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

                PointF center = new PointF(
                    ClientSize.Width / 2.0f,
                    ClientSize.Height / 2.0f);

                DrawShadow(graphics, center);
                DrawSectors(graphics, center);
                DrawCenter(graphics, center);
            }

            ReplaceRenderedBitmap(bitmap);
        }

        private void DrawShadow(
            Graphics graphics,
            PointF center)
        {
            const int shadowSteps = 9;
            Color configuredShadow = _colorScheme.ReadColor(
                _colorScheme.ShadowColor, Color.FromArgb(80, 0, 0, 0));

            for (int step = shadowSteps;
                 step >= 1;
                 step--)
            {
                int alpha = configuredShadow.A * (shadowSteps - step + 2) / 24;
                float width = (2.0f + step * 1.7f) * _scale;
                using (Pen shadowPen = new Pen(
                    Color.FromArgb(alpha, configuredShadow.R,
                        configuredShadow.G, configuredShadow.B), width))
                {
                    shadowPen.LineJoin = LineJoin.Round;
                    for (int index = 0; index < _items.Count; index++)
                    {
                        using (GraphicsPath sectorPath = CreateSectorPath(center, index))
                            graphics.DrawPath(shadowPen, sectorPath);
                    }

                    RectangleF centerRectangle = new RectangleF(
                        center.X - CancelRadius,
                        center.Y - CancelRadius,
                        CancelRadius * 2.0f,
                        CancelRadius * 2.0f);
                    graphics.DrawEllipse(shadowPen, centerRectangle);
                }
            }
        }

        private void ReplaceRenderedBitmap(Bitmap bitmap)
        {
            if (_renderedHBitmap != IntPtr.Zero)
            {
                NativeMethods.DeleteObject(_renderedHBitmap);
                _renderedHBitmap = IntPtr.Zero;
            }

            if (_renderedBitmap != null)
            {
                _renderedBitmap.Dispose();
            }

            _renderedBitmap = bitmap;
            _renderedHBitmap =
                bitmap.GetHbitmap(Color.FromArgb(0));
        }

        private void PresentLayer(byte alpha)
        {
            if (_renderedHBitmap == IntPtr.Zero ||
                !IsHandleCreated)
            {
                return;
            }

            IntPtr screenDc =
                NativeMethods.GetDC(IntPtr.Zero);

            IntPtr memoryDc = IntPtr.Zero;
            IntPtr oldBitmap = IntPtr.Zero;

            try
            {
                memoryDc =
                    NativeMethods.CreateCompatibleDC(screenDc);

                oldBitmap =
                    NativeMethods.SelectObject(
                        memoryDc,
                        _renderedHBitmap);

                NativePoint sourcePoint =
                    new NativePoint(0, 0);

                NativePoint windowPoint =
                    new NativePoint(Left, Top);

                NativeSize windowSize =
                    new NativeSize(
                        ClientSize.Width,
                        ClientSize.Height);

                BlendFunction blend =
                    new BlendFunction
                    {
                        BlendOp = AcSrcOver,
                        BlendFlags = 0,
                        SourceConstantAlpha = alpha,
                        AlphaFormat = AcSrcAlpha
                    };

                if (!NativeMethods.UpdateLayeredWindow(
                    Handle,
                    screenDc,
                    ref windowPoint,
                    ref windowSize,
                    memoryDc,
                    ref sourcePoint,
                    0,
                    ref blend,
                    UlwAlpha))
                {
                    throw new InvalidOperationException(
                        "Unable to update the RadialButton layered window.");
                }
            }
            finally
            {
                if (oldBitmap != IntPtr.Zero)
                {
                    NativeMethods.SelectObject(
                        memoryDc,
                        oldBitmap);
                }

                if (memoryDc != IntPtr.Zero)
                {
                    NativeMethods.DeleteDC(memoryDc);
                }

                if (screenDc != IntPtr.Zero)
                {
                    NativeMethods.ReleaseDC(
                        IntPtr.Zero,
                        screenDc);
                }
            }

            _windowAlpha = alpha;
        }

        private void DrawSectors(
            Graphics graphics,
            PointF center)
        {
            for (int index = 0;
                 index < _items.Count;
                 index++)
            {
                bool isHovered =
                    index == _hoveredIndex;
                bool isTab = _items[index].IsTab;

                // ==================== COLORS: SECTORS ====================

                Color backgroundColor = isTab
                    ? (isHovered
                        ? _colorScheme.ReadColor(_colorScheme.TabSectorHoverColor,
                            Color.FromArgb(53, 111, 168))
                        : _colorScheme.ReadColor(_colorScheme.TabSectorColor,
                            Color.FromArgb(52, 57, 63)))
                    : (isHovered
                        ? _colorScheme.ReadColor(_colorScheme.SectorHoverColor, Color.FromArgb(63, 128, 201))
                        : _colorScheme.ReadColor(_colorScheme.SectorColor, Color.FromArgb(61, 66, 72)));

                Color borderColor = isTab
                    ? (isHovered
                        ? _colorScheme.ReadColor(_colorScheme.TabSectorHoverBorderColor,
                            Color.FromArgb(107, 152, 194))
                        : _colorScheme.ReadColor(_colorScheme.TabSectorBorderColor,
                            Color.FromArgb(80, 87, 95)))
                    : (isHovered
                        ? _colorScheme.ReadColor(_colorScheme.SectorHoverBorderColor, Color.FromArgb(112, 166, 221))
                        : _colorScheme.ReadColor(_colorScheme.SectorBorderColor, Color.FromArgb(91, 98, 106)));

                // =========================================================

                using (GraphicsPath sectorPath =
                       CreateSectorPath(center, index))
                using (Brush backgroundBrush =
                       new SolidBrush(backgroundColor))
                using (Pen borderPen =
                       new Pen(
                           borderColor,
                           (isHovered ? 1.8f : 1.2f) * _scale))
                {
                    borderPen.LineJoin =
                        LineJoin.Round;

                    graphics.FillPath(
                        backgroundBrush,
                        sectorPath);

                    graphics.DrawPath(
                        borderPen,
                        sectorPath);
                }

                DrawItemTitle(
                    graphics,
                    center,
                    index,
                    _items[index].Title,
                    isHovered);
            }
        }

        private void DrawItemTitle(
            Graphics graphics,
            PointF center,
            int index,
            string title,
            bool isHovered)
        {
            float centerAngle =
                index * GetSectorAngle();

            double radians =
                centerAngle *
                Math.PI /
                180.0;

            float textRadius = 117.0f * _scale;
            float textWidth = 96.0f * _scale;
            float textHeight = 48.0f * _scale;

            float textCenterX =
                center.X +
                (float)Math.Cos(radians) *
                textRadius;

            float textCenterY =
                center.Y +
                (float)Math.Sin(radians) *
                textRadius;

            RectangleF textRectangle =
                new RectangleF(
                    textCenterX - textWidth / 2.0f,
                    textCenterY - textHeight / 2.0f,
                    textWidth,
                    textHeight);

            // ====================== COLORS: TEXT ======================

                Color textColor =
                    isHovered
                    ? _colorScheme.ReadColor(_colorScheme.HoverTextColor, Color.White)
                    : _colorScheme.ReadColor(_colorScheme.TextColor, Color.FromArgb(235, 238, 241));

            // ==========================================================

            using (Font font =
                   new Font(
                       "Segoe UI",
                       (isHovered ? 9.5f : 9.0f) * _scale,
                       isHovered
                           ? FontStyle.Bold
                           : FontStyle.Regular,
                       GraphicsUnit.Point))
            using (Brush textBrush =
                   new SolidBrush(textColor))
            using (StringFormat format =
                   new StringFormat())
            {
                format.Alignment =
                    StringAlignment.Center;

                format.LineAlignment =
                    StringAlignment.Center;

                format.Trimming =
                    StringTrimming.EllipsisWord;

                graphics.DrawString(
                    title,
                    font,
                    textBrush,
                    textRectangle,
                    format);
            }
        }

        private void DrawCenter(
            Graphics graphics,
            PointF center)
        {
            RectangleF centerRectangle =
                new RectangleF(
                    center.X - CancelRadius,
                    center.Y - CancelRadius,
                    CancelRadius * 2.0f,
                    CancelRadius * 2.0f);

            // =================== COLORS: CANCEL BUTTON ===================

                Color backgroundColor =
                _cancelHovered
                    ? _colorScheme.ReadColor(_colorScheme.CancelHoverColor, Color.FromArgb(78, 84, 91))
                    : _colorScheme.ReadColor(_colorScheme.CancelColor, Color.FromArgb(46, 50, 55));

                Color borderColor =
                _cancelHovered
                    ? _colorScheme.ReadColor(_colorScheme.CancelHoverBorderColor, Color.FromArgb(127, 136, 146))
                    : _colorScheme.ReadColor(_colorScheme.CancelBorderColor, Color.FromArgb(91, 98, 106));

            Color textColor =
                _colorScheme.ReadColor(_colorScheme.CancelTextColor, Color.FromArgb(226, 229, 232));

            // =============================================================

            using (Brush backgroundBrush =
                   new SolidBrush(backgroundColor))
            using (Pen borderPen =
                   new Pen(borderColor, 1.2f * _scale))
            using (Font font =
                   new Font(
                       "Segoe UI",
                       8.5f * _scale,
                       _cancelHovered
                           ? FontStyle.Bold
                           : FontStyle.Regular,
                       GraphicsUnit.Point))
            using (Brush textBrush =
                   new SolidBrush(textColor))
            using (StringFormat format =
                   new StringFormat())
            {
                graphics.FillEllipse(
                    backgroundBrush,
                    centerRectangle);

                graphics.DrawEllipse(
                    borderPen,
                    centerRectangle);

                format.Alignment =
                    StringAlignment.Center;

                format.LineAlignment =
                    StringAlignment.Center;

                graphics.DrawString(
                    "Cancel",
                    font,
                    textBrush,
                    centerRectangle,
                    format);
            }
        }

        private GraphicsPath CreateSectorPath(
            PointF center,
            int index)
        {
            float sectorAngle =
                GetSectorAngle();

            float sectorCenterAngle =
                index * sectorAngle;

            float startDividerAngle =
                sectorCenterAngle -
                sectorAngle / 2.0f;

            float endDividerAngle =
                sectorCenterAngle +
                sectorAngle / 2.0f;

            float halfSeparator =
                SeparatorWidth / 2.0f;

            PointF outerStart =
                GetOffsetCircleIntersection(
                    center,
                    OuterRadius,
                    startDividerAngle,
                    halfSeparator);

            PointF innerStart =
                GetOffsetCircleIntersection(
                    center,
                    SectorInnerRadius,
                    startDividerAngle,
                    halfSeparator);

            PointF outerEnd =
                GetOffsetCircleIntersection(
                    center,
                    OuterRadius,
                    endDividerAngle,
                    -halfSeparator);

            PointF innerEnd =
                GetOffsetCircleIntersection(
                    center,
                    SectorInnerRadius,
                    endDividerAngle,
                    -halfSeparator);

            float outerStartAngle =
                GetAngle(
                    center,
                    outerStart);

            float outerEndAngle =
                GetAngle(
                    center,
                    outerEnd);

            float innerStartAngle =
                GetAngle(
                    center,
                    innerStart);

            float innerEndAngle =
                GetAngle(
                    center,
                    innerEnd);

            float outerSweep =
                GetClockwiseSweep(
                    outerStartAngle,
                    outerEndAngle);

            float innerSweep =
                GetClockwiseSweep(
                    innerStartAngle,
                    innerEndAngle);

            RectangleF outerRectangle =
                new RectangleF(
                    center.X - OuterRadius,
                    center.Y - OuterRadius,
                    OuterRadius * 2.0f,
                    OuterRadius * 2.0f);

            RectangleF innerRectangle =
                new RectangleF(
                    center.X - SectorInnerRadius,
                    center.Y - SectorInnerRadius,
                    SectorInnerRadius * 2.0f,
                    SectorInnerRadius * 2.0f);

            GraphicsPath path =
                new GraphicsPath();

            path.AddArc(
                outerRectangle,
                outerStartAngle,
                outerSweep);

            path.AddLine(
                outerEnd,
                innerEnd);

            path.AddArc(
                innerRectangle,
                innerEndAngle,
                -innerSweep);

            path.AddLine(
                innerStart,
                outerStart);

            path.CloseFigure();

            return path;
        }

        private float GetSectorAngle()
        {
            return 360.0f / _items.Count;
        }

        private static PointF GetOffsetCircleIntersection(
            PointF center,
            float radius,
            float dividerAngle,
            float offset)
        {
            double radians =
                dividerAngle *
                Math.PI /
                180.0;

            float directionX =
                (float)Math.Cos(radians);

            float directionY =
                (float)Math.Sin(radians);

            float normalX =
                -directionY;

            float normalY =
                directionX;

            float offsetX =
                normalX * offset;

            float offsetY =
                normalY * offset;

            float distanceAlongDivider =
                (float)Math.Sqrt(
                    radius * radius -
                    offset * offset);

            return new PointF(
                center.X +
                offsetX +
                directionX * distanceAlongDivider,

                center.Y +
                offsetY +
                directionY * distanceAlongDivider);
        }

        private static float GetAngle(
            PointF center,
            PointF point)
        {
            double angle =
                Math.Atan2(
                    point.Y - center.Y,
                    point.X - center.X) *
                180.0 /
                Math.PI;

            if (angle < 0.0)
            {
                angle += 360.0;
            }

            return (float)angle;
        }

        private static float GetClockwiseSweep(
            float startAngle,
            float endAngle)
        {
            float sweep =
                endAngle - startAngle;

            if (sweep < 0.0f)
            {
                sweep += 360.0f;
            }

            return sweep;
        }

        private static double GetDistance(
            Point mousePosition,
            Size clientSize)
        {
            float centerX =
                clientSize.Width / 2.0f;

            float centerY =
                clientSize.Height / 2.0f;

            float deltaX =
                mousePosition.X - centerX;

            float deltaY =
                mousePosition.Y - centerY;

            return Math.Sqrt(
                deltaX * deltaX +
                deltaY * deltaY);
        }

        private int GetSectorIndex(
            Point mousePosition)
        {
            double distance =
                GetDistance(
                    mousePosition,
                    ClientSize);

            if (distance < SectorInnerRadius ||
                distance > OuterRadius)
            {
                return -1;
            }

            PointF center =
                new PointF(
                    ClientSize.Width / 2.0f,
                    ClientSize.Height / 2.0f);

            for (int index = 0;
                 index < _items.Count;
                 index++)
            {
                using (GraphicsPath sectorPath =
                       CreateSectorPath(center, index))
                {
                    if (sectorPath.IsVisible(
                        mousePosition))
                    {
                        return index;
                    }
                }
            }

            return -1;
        }

        private bool IsCancelButton(
            Point mousePosition)
        {
            return GetDistance(
                       mousePosition,
                       ClientSize) <=
                   CancelRadius;
        }

        private void OnFormShown(
            object sender,
            EventArgs e)
        {
            RenderLayer();

            _windowAlpha = 36;
            PresentLayer(_windowAlpha);

            _fadingOut = false;
            _fadeStartedAt =
                Environment.TickCount;

            _fadeTimer.Start();

            Activate();
            Focus();
        }

        private void OnFormDeactivate(
            object sender,
            EventArgs e)
        {
            if (!_selectionMade &&
                !_fadingOut)
            {
                BeginClose(
                    DialogResult.Cancel);
            }
        }

        private void OnFormKeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (!string.Equals(_interfaceSettings.KeyboardBackButton, "None", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(e.KeyCode.ToString(), _interfaceSettings.KeyboardBackButton,
                    StringComparison.OrdinalIgnoreCase))
            {
                e.Handled = true;
                RequestBack();
                return;
            }

            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                CloseWithoutSelection();
            }
        }

        private void OnFormMouseMove(
            object sender,
            MouseEventArgs e)
        {
            int newHoveredIndex =
                GetSectorIndex(e.Location);

            bool newCancelHovered =
                IsCancelButton(e.Location);

            if (newHoveredIndex == _hoveredIndex &&
                newCancelHovered == _cancelHovered)
            {
                return;
            }

            _hoveredIndex =
                newHoveredIndex;

            _cancelHovered =
                newCancelHovered;

            RenderLayer();
            PresentLayer(_windowAlpha);
        }

        private void OnFormMouseDown(
            object sender,
            MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            if (IsCancelButton(e.Location))
            {
                CloseWithoutSelection();
                return;
            }

            int selectedIndex =
                GetSectorIndex(e.Location);

            if (selectedIndex < 0 ||
                selectedIndex >= _items.Count)
            {
                return;
            }

            SelectedItem =
                _items[selectedIndex];

            _selectionMade = true;

            BeginClose(
                DialogResult.OK);
        }

        private void CloseWithoutSelection()
        {
            SelectedItem = null;

            BeginClose(
                DialogResult.Cancel);
        }

        private void RequestBack()
        {
            if (!_canGoBack) return;
            SelectedItem = null;
            BackRequested = true;
            _selectionMade = true;
            BeginClose(DialogResult.Retry);
        }

        private void BeginClose(
            DialogResult dialogResult)
        {
            if (_fadingOut)
            {
                return;
            }

            _pendingDialogResult =
                dialogResult;

            _fadingOut = true;

            _fadeStartAlpha =
                _windowAlpha;

            _fadeStartedAt =
                Environment.TickCount;

            _fadeTimer.Start();
        }

        private void OnFadeTimerTick(
            object sender,
            EventArgs e)
        {
            int elapsed =
                unchecked(
                    Environment.TickCount -
                    _fadeStartedAt);

            if (_fadingOut)
            {
                float progress =
                    Math.Min(
                        1.0f,
                        elapsed /
                        (float)FadeOutDuration);

                byte alpha =
                    (byte)Math.Round(
                        _fadeStartAlpha *
                        (1.0f -
                         EaseOutCubic(progress)));

                PresentLayer(alpha);

                if (progress >= 1.0f)
                {
                    _fadeTimer.Stop();

                    _allowClose = true;

                    DialogResult =
                        _pendingDialogResult;

                    Close();
                }

                return;
            }

            float fadeInProgress =
                Math.Min(
                    1.0f,
                    elapsed /
                    (float)FadeInDuration);

            byte fadeInAlpha =
                (byte)Math.Round(
                    36.0f +
                    (255.0f - 36.0f) *
                    EaseOutCubic(
                        fadeInProgress));

            PresentLayer(fadeInAlpha);

            if (fadeInProgress >= 1.0f)
            {
                _fadeTimer.Stop();
                PresentLayer(255);
            }
        }

        private static float EaseOutCubic(
            float progress)
        {
            float inverse =
                1.0f - progress;

            return 1.0f -
                   inverse *
                   inverse *
                   inverse;
        }

        private void OnFormClosing(
            object sender,
            FormClosingEventArgs e)
        {
            if (_allowClose ||
                e.CloseReason ==
                CloseReason.ApplicationExitCall ||
                e.CloseReason ==
                CloseReason.TaskManagerClosing ||
                e.CloseReason ==
                CloseReason.WindowsShutDown)
            {
                return;
            }

            e.Cancel = true;

            BeginClose(
                DialogResult.Cancel);
        }

        protected override void Dispose(
            bool disposing)
        {
            if (disposing)
            {
                _fadeTimer.Stop();
                _fadeTimer.Dispose();

                if (_renderedBitmap != null)
                {
                    _renderedBitmap.Dispose();
                    _renderedBitmap = null;
                }
            }

            if (_renderedHBitmap != IntPtr.Zero)
            {
                NativeMethods.DeleteObject(
                    _renderedHBitmap);

                _renderedHBitmap =
                    IntPtr.Zero;
            }

            base.Dispose(disposing);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint
        {
            public int X;
            public int Y;

            public NativePoint(
                int x,
                int y)
            {
                X = x;
                Y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeSize
        {
            public int Width;
            public int Height;

            public NativeSize(
                int width,
                int height)
            {
                Width = width;
                Height = height;
            }
        }

        [StructLayout(
            LayoutKind.Sequential,
            Pack = 1)]
        private struct BlendFunction
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        private static class NativeMethods
        {
            [DllImport(
                "user32.dll",
                SetLastError = true)]
            public static extern IntPtr GetDC(
                IntPtr windowHandle);

            [DllImport(
                "user32.dll",
                SetLastError = true)]
            public static extern int ReleaseDC(
                IntPtr windowHandle,
                IntPtr deviceContext);

            [DllImport(
                "gdi32.dll",
                SetLastError = true)]
            public static extern IntPtr CreateCompatibleDC(
                IntPtr deviceContext);

            [DllImport(
                "gdi32.dll",
                SetLastError = true)]
            public static extern bool DeleteDC(
                IntPtr deviceContext);

            [DllImport(
                "gdi32.dll",
                SetLastError = true)]
            public static extern IntPtr SelectObject(
                IntPtr deviceContext,
                IntPtr drawingObject);

            [DllImport(
                "gdi32.dll",
                SetLastError = true)]
            public static extern bool DeleteObject(
                IntPtr drawingObject);

            [DllImport(
                "user32.dll",
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool UpdateLayeredWindow(
                IntPtr windowHandle,
                IntPtr destinationDc,
                ref NativePoint destinationPoint,
                ref NativeSize size,
                IntPtr sourceDc,
                ref NativePoint sourcePoint,
                int colorKey,
                ref BlendFunction blend,
                int flags);
        }
    }
}
