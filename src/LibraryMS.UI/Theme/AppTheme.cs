namespace LibraryMS.UI.Theme;

/// <summary>
/// Centralized dark theme color palette with glassmorphism and neumorphism tokens.
/// Premium enterprise design system.
/// </summary>
public static class AppTheme
{
    // ─── Core Background Colors ────────────────────────────────
    public static readonly Color Background = Color.FromArgb(15, 15, 26);           // #0F0F1A
    public static readonly Color Surface = Color.FromArgb(26, 26, 46);              // #1A1A2E
    public static readonly Color SurfaceLight = Color.FromArgb(37, 37, 61);         // #25253D
    public static readonly Color SurfaceHover = Color.FromArgb(45, 45, 75);         // #2D2D4B
    public static readonly Color SurfaceElevated = Color.FromArgb(30, 30, 55);      // #1E1E37

    // ─── Accent Colors ─────────────────────────────────────────
    public static readonly Color Primary = Color.FromArgb(108, 99, 255);            // #6C63FF
    public static readonly Color PrimaryLight = Color.FromArgb(139, 131, 255);      // #8B83FF
    public static readonly Color PrimaryDark = Color.FromArgb(85, 77, 204);         // #554DCC
    public static readonly Color Secondary = Color.FromArgb(0, 217, 255);           // #00D9FF
    public static readonly Color SecondaryLight = Color.FromArgb(51, 228, 255);     // #33E4FF
    public static readonly Color Accent = Color.FromArgb(255, 107, 157);            // #FF6B9D

    // ─── Status Colors ──────────────────────────────────────────
    public static readonly Color Success = Color.FromArgb(0, 230, 118);             // #00E676
    public static readonly Color SuccessLight = Color.FromArgb(40, 255, 148);       // #28FF94
    public static readonly Color Warning = Color.FromArgb(255, 179, 0);             // #FFB300
    public static readonly Color WarningLight = Color.FromArgb(255, 204, 51);       // #FFCC33
    public static readonly Color Danger = Color.FromArgb(255, 82, 82);              // #FF5252
    public static readonly Color DangerLight = Color.FromArgb(255, 120, 120);       // #FF7878

    // ─── Text Colors ────────────────────────────────────────────
    public static readonly Color TextPrimary = Color.FromArgb(234, 234, 234);       // #EAEAEA
    public static readonly Color TextSecondary = Color.FromArgb(142, 142, 160);     // #8E8EA0
    public static readonly Color TextMuted = Color.FromArgb(100, 100, 120);         // #646478
    public static readonly Color TextOnPrimary = Color.FromArgb(255, 255, 255);     // #FFFFFF

    // ─── Border & Separator Colors ──────────────────────────────
    public static readonly Color Border = Color.FromArgb(45, 45, 68);               // #2D2D44
    public static readonly Color BorderLight = Color.FromArgb(55, 55, 80);          // #373750
    public static readonly Color Separator = Color.FromArgb(35, 35, 55);            // #232337

    // ─── Glassmorphism Colors ───────────────────────────────────
    public static readonly Color GlassBackground = Color.FromArgb(40, 26, 26, 46);  // Translucent surface
    public static readonly Color GlassBorder = Color.FromArgb(60, 255, 255, 255);    // Subtle white border
    public static readonly Color GlassHighlight = Color.FromArgb(20, 255, 255, 255); // Top highlight

    // ─── Dashboard Card Gradient Pairs ──────────────────────────
    public static readonly (Color Start, Color End) GradientBooks = (
        Color.FromArgb(108, 99, 255), Color.FromArgb(139, 131, 255));
    public static readonly (Color Start, Color End) GradientMembers = (
        Color.FromArgb(0, 217, 255), Color.FromArgb(51, 228, 255));
    public static readonly (Color Start, Color End) GradientLoans = (
        Color.FromArgb(0, 230, 118), Color.FromArgb(40, 255, 148));
    public static readonly (Color Start, Color End) GradientOverdue = (
        Color.FromArgb(255, 82, 82), Color.FromArgb(255, 120, 120));

    // ─── Sidebar ────────────────────────────────────────────────
    public static readonly Color SidebarBackground = Color.FromArgb(12, 12, 22);    // #0C0C16
    public static readonly Color SidebarActive = Color.FromArgb(108, 99, 255);      // Primary
    public static readonly Color SidebarHover = Color.FromArgb(25, 25, 42);         // #19192A
    public static readonly Color SidebarText = Color.FromArgb(160, 160, 180);       // #A0A0B4
    public static readonly Color SidebarTextActive = Color.FromArgb(255, 255, 255);

    // ─── Typography ─────────────────────────────────────────
    public static readonly Font FontTitle         = new("Segoe UI Semibold", 24f, FontStyle.Bold);
    public static readonly Font FontSubtitle      = new("Segoe UI Semibold", 18f, FontStyle.Bold);
    public static readonly Font FontHeading       = new("Segoe UI Semibold", 14f, FontStyle.Bold);
    public static readonly Font FontBody          = new("Segoe UI", 11f);
    public static readonly Font FontBodySmall     = new("Segoe UI", 10f);
    public static readonly Font FontCaption       = new("Segoe UI", 8.5f);
    public static readonly Font FontButton        = new("Segoe UI Semibold", 10.5f, FontStyle.Bold);
    public static readonly Font FontCardValue     = new("Segoe UI Semibold", 30f, FontStyle.Bold);
    public static readonly Font FontCardLabel     = new("Segoe UI", 10.5f);
    public static readonly Font FontSidebar       = new("Segoe UI Semibold", 10.5f);
    public static readonly Font FontSidebarBrand  = new("Segoe UI Semibold", 16f, FontStyle.Bold);
    public static readonly Font FontMono          = new("Cascadia Code", 10f);
    public static readonly Font FontBadge         = new("Segoe UI Semibold", 8.5f, FontStyle.Bold);

    // ─── Sizing ─────────────────────────────────────────────
    public const int SidebarWidth          = 260;
    public const int SidebarCollapsedWidth = 70;
    public const int TopBarHeight          = 0;
    public const int CardBorderRadius      = 16;
    public const int ButtonBorderRadius    = 12;
    public const int InputBorderRadius     = 10;
    public const int CardPadding           = 24;
    public const int GridRowHeight         = 44;
    public const int PagePaddingH          = 32;
    public const int PagePaddingV          = 24;
    public const int SectionGap            = 16;

    // ─── Loading / Skeleton ─────────────────────────────────
    public static readonly Color LoadingShimmer = Color.FromArgb(40, 40, 66);
    public static readonly Color LoadingBase    = Color.FromArgb(28, 28, 50);

    // ─── Neumorphism Shadow Colors ──────────────────────────
    public static readonly Color NeuShadowDark  = Color.FromArgb(80, 0, 0, 0);
    public static readonly Color NeuShadowLight = Color.FromArgb(30, 255, 255, 255);
    public static readonly Color NeuInsetDark   = Color.FromArgb(60, 0, 0, 0);
    public static readonly Color NeuInsetLight  = Color.FromArgb(15, 255, 255, 255);
}
