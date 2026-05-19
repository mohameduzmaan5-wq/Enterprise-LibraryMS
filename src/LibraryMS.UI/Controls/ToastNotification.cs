using Guna.UI2.WinForms;
using LibraryMS.UI.Theme;
using Timer = System.Windows.Forms.Timer;

namespace LibraryMS.UI.Controls;

public enum ToastType
{
    Success,
    Error,
    Info,
    Warning
}

public class ToastNotification : Form
{
    private Label _messageLabel = null!;
    private PictureBox _iconBox = null!;
    private Timer _animationTimer = null!;
    private Timer _displayTimer = null!;
    private int _targetY;
    private double _opacity = 0;
    private const int AnimationSpeed = 10;

    public ToastNotification(string message, ToastType type)
    {
        InitializeComponent(message, type);
    }

    private void InitializeComponent(string message, ToastType type)
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        ShowInTaskbar = false;
        Size = new Size(400, 64);
        BackColor = GetBackColor(type);
        Opacity = 0;

        // Apply rounded corners using Guna2Elipse
        var elipse = new Guna2Elipse
        {
            TargetControl = this,
            BorderRadius = 8
        };

        _iconBox = new PictureBox
        {
            Size = new Size(24, 24),
            Location = new Point(15, 18),
            SizeMode = PictureBoxSizeMode.Zoom,
            Image = GetIcon(type)
        };

        _messageLabel = new Label
        {
            Text = message,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Regular),
            AutoSize = false,
            Size = new Size(330, 44),
            Location = new Point(50, 10),
            TextAlign = ContentAlignment.MiddleLeft
        };

        Controls.Add(_iconBox);
        Controls.Add(_messageLabel);

        _animationTimer = new Timer { Interval = 10 };
        _animationTimer.Tick += AnimationTimer_Tick;

        _displayTimer = new Timer { Interval = 3000 };
        _displayTimer.Tick += DisplayTimer_Tick;
    }

    private Color GetBackColor(ToastType type) => type switch
    {
        ToastType.Success => Color.FromArgb(38, 166, 91),
        ToastType.Error   => Color.FromArgb(214, 48, 49),
        ToastType.Warning => Color.FromArgb(225, 177, 44),
        _                 => Color.FromArgb(45, 134, 202)
    };

    private Image? GetIcon(ToastType type)
    {
        // For simplicity, we just draw a simple shape or return null.
        // In a real app, you would load resources. Here we draw it dynamically.
        var bmp = new Bitmap(24, 24);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(Color.White);
        if (type == ToastType.Success)
        {
            // Simple checkmark
            var pen = new Pen(Color.White, 3);
            g.DrawLine(pen, 5, 12, 10, 17);
            g.DrawLine(pen, 10, 17, 19, 7);
        }
        else if (type == ToastType.Error)
        {
            // Simple X
            var pen = new Pen(Color.White, 3);
            g.DrawLine(pen, 6, 6, 18, 18);
            g.DrawLine(pen, 18, 6, 6, 18);
        }
        else
        {
            g.FillEllipse(brush, 4, 4, 16, 16);
        }
        return bmp;
    }

    public static void Show(Form parent, string message, ToastType type = ToastType.Info)
    {
        var toast = new ToastNotification(message, type);
        
        // Position at bottom right of the parent form
        int startX = parent.Location.X + parent.Width - toast.Width - 20;
        int startY = parent.Location.Y + parent.Height;
        toast._targetY = parent.Location.Y + parent.Height - toast.Height - 20;
        
        toast.Location = new Point(startX, startY);
        toast.Show(parent);
        toast._animationTimer.Start();
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        if (_opacity < 1.0)
        {
            _opacity += 0.1;
            Opacity = _opacity;
        }

        if (Location.Y > _targetY)
        {
            Location = new Point(Location.X, Location.Y - AnimationSpeed);
        }
        else if (_opacity >= 1.0)
        {
            _animationTimer.Stop();
            _displayTimer.Start();
        }
    }

    private void DisplayTimer_Tick(object? sender, EventArgs e)
    {
        _displayTimer.Stop();
        _animationTimer.Tick -= AnimationTimer_Tick;
        _animationTimer.Tick += FadeOutTimer_Tick;
        _animationTimer.Start();
    }

    private void FadeOutTimer_Tick(object? sender, EventArgs e)
    {
        if (Opacity > 0)
        {
            Opacity -= 0.1;
        }
        else
        {
            _animationTimer.Stop();
            Close();
            Dispose();
        }
    }
}
