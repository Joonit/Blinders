using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Blinders
{
    /// <summary>What the settings window needs from the running app.</summary>
    interface ISettingsHost
    {
        void Changed();            // apply + save
        void HotkeysSuspend();     // so keys can be captured in the hotkey boxes
        void HotkeysResume();
        bool AutoStart { get; set; }
        void BitmapChanged();
        void LanguageChanged();
        void RebuildSettings();    // recreate the window (new language, theme or DPI)
    }

    class MonInfo
    {
        public string Key;
        public Rectangle Bounds;
        public bool Primary;
        public int Index;
    }

    static class UiNative
    {
        [DllImport("dwmapi.dll")] public static extern int DwmSetWindowAttribute(IntPtr h, int attr, ref int value, int size);
        [DllImport("user32.dll")] public static extern IntPtr MonitorFromPoint(Point pt, uint flags);
        [DllImport("shcore.dll")] public static extern int GetDpiForMonitor(IntPtr mon, int type, out uint dpiX, out uint dpiY);
        [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr h);

        public static int DpiAt(Point p)
        {
            try
            {
                uint x, y;
                if (GetDpiForMonitor(MonitorFromPoint(p, 2 /*NEAREST*/), 0 /*EFFECTIVE*/, out x, out y) == 0 && x > 0) return (int)x;
            }
            catch { }
            return 96;
        }
    }

    static class UiUtil
    {
        /// <summary>Monitors in real pixels (independent of the calling window's DPI context).</summary>
        public static List<MonInfo> RealScreens()
        {
            List<MonInfo> l = new List<MonInfo>();
            Util.WithDpi(-4, delegate
            {
                Screen[] ss = Screen.AllScreens;
                for (int i = 0; i < ss.Length; i++)
                {
                    MonInfo m = new MonInfo();
                    m.Key = Util.MonKey(ss[i]);
                    m.Bounds = ss[i].Bounds;
                    m.Primary = ss[i].Primary;
                    m.Index = i;
                    l.Add(m);
                }
            });
            return l;
        }

        public static List<string> RunningApps()
        {
            SortedDictionary<string, bool> set = new SortedDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (Process p in Process.GetProcesses())
            {
                using (p)
                {
                    try
                    {
                        if (p.MainWindowHandle != IntPtr.Zero && p.MainWindowTitle.Length > 0) set[p.ProcessName] = true;
                    }
                    catch { }
                }
            }
            return new List<string>(set.Keys);
        }

        /// <summary>Color of "top" drawn over "under" with the given alpha (0..255).</summary>
        public static Color Blend(Color top, Color under, int alpha)
        {
            int a = Math.Max(0, Math.Min(255, alpha));
            return Color.FromArgb(
                (under.R * (255 - a) + top.R * a) / 255,
                (under.G * (255 - a) + top.G * a) / 255,
                (under.B * (255 - a) + top.B * a) / 255);
        }
    }

    /// <summary>Light / dark palette. Follows the Windows "app mode" setting.</summary>
    class Theme
    {
        public bool Dark;
        public Color Bg, Nav, NavSel, NavHover, Text, SubText, Line, Input, Button, ButtonHover, Accent, MapBg;

        public static bool SystemIsDark()
        {
            // BLINDERS_THEME=dark|light overrides the system setting (screenshots, testing)
            string force = Environment.GetEnvironmentVariable("BLINDERS_THEME");
            if (force == "dark") return true;
            if (force == "light") return false;
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    object v = k == null ? null : k.GetValue("AppsUseLightTheme");
                    return v is int && (int)v == 0;
                }
            }
            catch { return false; }
        }

        public static Theme Current()
        {
            Theme t = new Theme();
            t.Dark = SystemIsDark();
            t.Accent = Art.AccentColor;
            if (t.Dark)
            {
                t.Bg = Color.FromArgb(32, 32, 32); t.Nav = Color.FromArgb(25, 25, 25);
                t.NavSel = Color.FromArgb(45, 45, 45); t.NavHover = Color.FromArgb(38, 38, 38);
                t.Text = Color.FromArgb(242, 242, 242); t.SubText = Color.FromArgb(166, 166, 166);
                t.Line = Color.FromArgb(64, 64, 64); t.Input = Color.FromArgb(43, 43, 43);
                t.Button = Color.FromArgb(45, 45, 45); t.ButtonHover = Color.FromArgb(58, 58, 58);
                t.MapBg = Color.FromArgb(40, 40, 40);
            }
            else
            {
                t.Bg = Color.FromArgb(249, 249, 249); t.Nav = Color.FromArgb(238, 238, 238);
                t.NavSel = Color.FromArgb(255, 255, 255); t.NavHover = Color.FromArgb(230, 230, 230);
                t.Text = Color.FromArgb(26, 26, 26); t.SubText = Color.FromArgb(96, 96, 96);
                t.Line = Color.FromArgb(208, 208, 208); t.Input = Color.White;
                t.Button = Color.FromArgb(253, 253, 253); t.ButtonHover = Color.FromArgb(240, 240, 240);
                t.MapBg = Color.FromArgb(240, 241, 243);
            }
            return t;
        }
    }

    class LangItem
    {
        public string Code, Name;
        public override string ToString() { return Name; }
    }

    /// <summary>Sidebar entry with an accent bar when selected.</summary>
    class NavButton : Button
    {
        public bool Selected;
        public Theme T;
        bool hover;

        public NavButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            TextAlign = ContentAlignment.MiddleLeft;
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.Selectable, true);
        }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            Color bg = Selected ? T.NavSel : hover ? T.NavHover : T.Nav;
            e.Graphics.Clear(bg);
            if (Selected)
            {
                int bw = Math.Max(3, Height / 10), bh = Height / 2;
                int x = RightToLeft == RightToLeft.Yes ? Width - bw - 2 : 2;
                using (Brush b = new SolidBrush(T.Accent)) e.Graphics.FillRectangle(b, x, (Height - bh) / 2, bw, bh);
            }
            TextFormatFlags f = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
            if (RightToLeft == RightToLeft.Yes) f |= TextFormatFlags.Right | TextFormatFlags.RightToLeft;
            Rectangle r = new Rectangle(Padding.Left, 0, Width - Padding.Horizontal, Height);
            using (Font fnt = Selected ? new Font(Font, FontStyle.Bold) : (Font)Font.Clone())
                TextRenderer.DrawText(e.Graphics, Text, fnt, r, T.Text, f);
            if (Focused && ShowFocusCues)
                ControlPaint.DrawFocusRectangle(e.Graphics, new Rectangle(1, 1, Width - 2, Height - 2));
        }
    }

    /// <summary>Clickable map of the monitors. Keyboard: arrows / Home / End select, Space toggles dimming.</summary>
    class MonitorMap : Control
    {
        public List<MonInfo> Mons;
        public Func<string, bool> IsOn;
        public Theme T;
        public int Selected;
        public event Action<int> SelectedChanged;
        public event Action ToggleRequested;
        List<RectangleF> rects = new List<RectangleF>();

        public MonitorMap()
        {
            SetStyle(ControlStyles.Selectable | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            TabStop = true;
            AccessibleRole = AccessibleRole.List;
        }

        void Select(int i)
        {
            if (Mons == null || i < 0 || i >= Mons.Count) return;
            Selected = i;
            Invalidate();
            AccessibilityNotifyClients(AccessibleEvents.Selection, i);
            if (SelectedChanged != null) SelectedChanged(i);
        }

        protected override bool IsInputKey(Keys k)
        {
            return k == Keys.Left || k == Keys.Right || k == Keys.Up || k == Keys.Down || base.IsInputKey(k);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (Mons == null || Mons.Count == 0) return;
            if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Down) Select((Selected + 1) % Mons.Count);
            else if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Up) Select((Selected + Mons.Count - 1) % Mons.Count);
            else if (e.KeyCode == Keys.Home) Select(0);
            else if (e.KeyCode == Keys.End) Select(Mons.Count - 1);
            else if (e.KeyCode == Keys.Space && ToggleRequested != null) ToggleRequested();
            base.OnKeyDown(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            Focus();
            for (int i = 0; i < rects.Count; i++)
                if (rects[i].Contains(e.Location)) { Select(i); break; }
            base.OnMouseDown(e);
        }

        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(T.MapBg);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            rects.Clear();
            if (Mons == null || Mons.Count == 0) return;

            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (MonInfo m in Mons)
            {
                minX = Math.Min(minX, m.Bounds.Left); minY = Math.Min(minY, m.Bounds.Top);
                maxX = Math.Max(maxX, m.Bounds.Right); maxY = Math.Max(maxY, m.Bounds.Bottom);
            }
            float pad = Font.Height;
            float availW = ClientSize.Width - 2 * pad, availH = ClientSize.Height - 2 * pad;
            float sc = Math.Min(availW / (maxX - minX), availH / (maxY - minY));
            float ox = pad + (availW - (maxX - minX) * sc) / 2f;
            float oy = pad + (availH - (maxY - minY) * sc) / 2f;
            float gap = Math.Max(2f, Font.Height / 6f);

            using (Font f = new Font(Font.FontFamily, Font.Size * 1.4f, FontStyle.Bold, Font.Unit))
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center; sf.LineAlignment = StringAlignment.Center;
                for (int i = 0; i < Mons.Count; i++)
                {
                    MonInfo m = Mons[i];
                    RectangleF rc = new RectangleF(ox + (m.Bounds.X - minX) * sc + gap / 2, oy + (m.Bounds.Y - minY) * sc + gap / 2,
                                                   m.Bounds.Width * sc - gap, m.Bounds.Height * sc - gap);
                    rects.Add(rc);
                    bool on = IsOn(m.Key);
                    Color fill = on ? Color.FromArgb(55, 65, 81) : (T.Dark ? Color.FromArgb(90, 94, 102) : Color.FromArgb(205, 208, 214));
                    using (Brush b = new SolidBrush(fill)) g.FillRectangle(b, rc);
                    bool sel = i == Selected;
                    using (Pen pn = new Pen(sel ? T.Accent : T.Line, sel ? Math.Max(2f, Font.Height / 6f) : 1f))
                        g.DrawRectangle(pn, rc.X, rc.Y, rc.Width, rc.Height);
                    using (Brush tb = new SolidBrush(on ? Color.White : Color.FromArgb(40, 40, 40)))
                        g.DrawString((m.Index + 1) + (m.Primary ? " ★" : ""), f, tb, rc, sf);
                }
            }
            if (Focused && ShowFocusCues)
                ControlPaint.DrawFocusRectangle(g, new Rectangle(2, 2, Width - 4, Height - 4));
        }
    }

    /// <summary>Hour : minute entry built from two flat drop-downs, so it follows the dark theme
    /// (DateTimePicker and NumericUpDown keep a white box).</summary>
    class TimeBox : FlowLayoutPanel
    {
        ComboBox h = new ComboBox(), m = new ComboBox();
        public event Action Changed;
        public TimeBox(int width, Theme t)
        {
            AutoSize = true; WrapContents = false; Margin = new Padding(0, 0, width / 3, 0);
            for (int i = 0; i < 24; i++) h.Items.Add(i.ToString("00"));
            for (int i = 0; i < 60; i += 5) m.Items.Add(i.ToString("00"));
            foreach (ComboBox c in new[] { h, m })
            {
                c.DropDownStyle = ComboBoxStyle.DropDownList; c.FlatStyle = FlatStyle.Flat;
                c.Width = width; c.BackColor = t.Input; c.ForeColor = t.Text;
                c.SelectedIndexChanged += delegate { if (Changed != null) Changed(); };
            }
            Label colon = new Label(); colon.Text = ":"; colon.AutoSize = true; colon.ForeColor = t.Text;
            colon.Margin = new Padding(2, 4, 2, 0);
            Controls.Add(h); Controls.Add(colon); Controls.Add(m);
        }
        public int Minutes
        {
            get { return Math.Max(0, h.SelectedIndex) * 60 + Math.Max(0, m.SelectedIndex) * 5; }
            set { h.SelectedIndex = (value / 60) % 24; m.SelectedIndex = Math.Min(11, (value % 60) / 5); }
        }
        public string Name2 { set { h.AccessibleName = value + " (h)"; m.AccessibleName = value + " (min)"; } }
    }

    class SettingsForm : Form
    {
        readonly Settings cfg;
        readonly ISettingsHost host;
        readonly Theme T;
        readonly int dpi;
        bool syncing, inSizeMove, dpiDirty;

        Panel content;
        List<NavButton> navs = new List<NavButton>();
        List<Control> pages = new List<Control>();
        int selected;

        CheckBox chkEnabled, chkFocus, chkFull, chkSpot, chkBorder, chkAuto, chkSchedule, chkMonOn, chkMonFollow;
        TrackBar tbAlpha, tbNight, tbMon, tbFade;
        Label lblAlpha, lblNight, lblMon, lblFade, lblSchedStatus, lblMonTitle, lblBitmap, pvActive, pvDim;
        TimeBox tStart, tEnd;
        Panel tintSwatch;
        MonitorMap map;
        ListBox lstExclude;
        ComboBox cmbApps, cmbLang;
        TextBox[] hkBoxes = new TextBox[Settings.Actions.Length];
        List<MonInfo> mons;

        int W(int v) { return Util.S(this, v); }

        public int SelectedTab
        {
            get { return selected; }
            set { ShowPage(value); }
        }

        /// <param name="at">top-left to open at (when rebuilding), or null to center on the monitor under the mouse.</param>
        public SettingsForm(Settings s, ISettingsHost h, Point? at)
        {
            cfg = s; host = h;
            T = Theme.Current();
            mons = UiUtil.RealScreens();

            // Size everything for the monitor the window opens on: crisp text on every monitor.
            Point anchor = at.HasValue ? at.Value : Cursor.Position;
            dpi = UiNative.DpiAt(anchor);
            Font = new Font("Segoe UI", 9f * dpi / 72f, GraphicsUnit.Pixel);
            AutoScaleMode = AutoScaleMode.None;

            Text = L.T("settings.title");
            Icon = Art.MakeIcon(Art.On, 32);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            KeyPreview = true;
            BackColor = T.Bg; ForeColor = T.Text;
            if (L.IsRtl) { RightToLeft = RightToLeft.Yes; RightToLeftLayout = true; }
            ClientSize = new Size(W(780), W(630));

            StartPosition = FormStartPosition.Manual;
            if (at.HasValue) Location = at.Value;
            else
            {
                Rectangle wa = Screen.FromPoint(anchor).WorkingArea;
                Location = new Point(wa.X + (wa.Width - Width) / 2, wa.Y + (wa.Height - Height) / 2);
            }

            // sidebar
            Panel nav = new Panel();
            nav.Dock = DockStyle.Left; nav.Width = W(200); nav.BackColor = T.Nav;
            nav.Padding = new Padding(W(8), W(12), W(8), W(8));
            Label brand = new Label();
            brand.Text = "Blinders"; brand.ForeColor = T.Text; brand.AutoSize = false;
            brand.Dock = DockStyle.Top; brand.Height = W(44);
            brand.Font = new Font(Font.FontFamily, Font.Size * 1.45f, FontStyle.Bold, GraphicsUnit.Pixel);
            brand.Padding = new Padding(W(8), 0, 0, W(8));
            Label ver = new Label();
            ver.Text = L.T("about.version", typeof(SettingsForm).Assembly.GetName().Version.ToString(3));
            ver.ForeColor = T.SubText; ver.Dock = DockStyle.Bottom; ver.Height = W(28);
            ver.Padding = new Padding(W(10), 0, 0, 0); ver.TextAlign = ContentAlignment.MiddleLeft;
            Panel navList = new Panel(); navList.Dock = DockStyle.Fill;

            content = new Panel();
            content.Dock = DockStyle.Fill; content.BackColor = T.Bg;

            string[] keys = { "tab.general", "tab.schedule", "tab.monitors", "tab.apps", "tab.hotkeys", "tab.more" };
            Func<FlowLayoutPanel>[] builders = { BuildGeneral, BuildSchedule, BuildMonitors, BuildApps, BuildHotkeys, BuildMore };
            for (int i = keys.Length - 1; i >= 0; i--)   // Dock=Top stacks in reverse
            {
                NavButton b = new NavButton();
                b.T = T; b.Text = L.T(keys[i]); b.Dock = DockStyle.Top; b.Height = W(40);
                b.Padding = new Padding(W(16), 0, W(8), 0);
                b.ForeColor = T.Text; b.BackColor = T.Nav;
                b.AccessibleRole = AccessibleRole.PageTab;
                int idx = i;
                b.Click += delegate { ShowPage(idx); };
                navList.Controls.Add(b);
                navs.Insert(0, b);
            }
            for (int i = 0; i < keys.Length; i++)
            {
                FlowLayoutPanel p = builders[i]();
                p.Visible = false;
                p.Controls.SetChildIndex(p.Controls[0], 0);
                content.Controls.Add(p);
                pages.Add(p);
            }
            nav.Controls.Add(navList); nav.Controls.Add(brand); nav.Controls.Add(ver);
            Controls.Add(content);
            Controls.Add(nav);

            KeyDown += delegate(object o, KeyEventArgs e) { if (e.KeyCode == Keys.Escape && !(ActiveControl is TextBox)) Close(); };
            FormClosed += delegate { host.HotkeysResume(); };
            Deactivate += delegate { host.HotkeysResume(); };
            Sync();
            ShowPage(0);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            int on = T.Dark ? 1 : 0;
            try { UiNative.DwmSetWindowAttribute(Handle, 20 /*DWMWA_USE_IMMERSIVE_DARK_MODE*/, ref on, 4); } catch { }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_DPICHANGED = 0x02E0, WM_ENTERSIZEMOVE = 0x0231, WM_EXITSIZEMOVE = 0x0232;
            if (m.Msg == WM_ENTERSIZEMOVE) inSizeMove = true;
            else if (m.Msg == WM_EXITSIZEMOVE)
            {
                inSizeMove = false;
                if (dpiDirty) { dpiDirty = false; BeginInvoke(new Action(host.RebuildSettings)); }
            }
            else if (m.Msg == WM_DPICHANGED)
            {
                // Moved to a monitor with another scale: redraw everything at the new DPI once the move ends.
                int newDpi = m.WParam.ToInt32() & 0xFFFF;
                if (newDpi != dpi)
                {
                    if (inSizeMove) dpiDirty = true;
                    else BeginInvoke(new Action(host.RebuildSettings));
                }
                return;
            }
            base.WndProc(ref m);
        }

        void ShowPage(int i)
        {
            if (i < 0 || i >= pages.Count) return;
            selected = i;
            for (int k = 0; k < pages.Count; k++)
            {
                pages[k].Visible = k == i;
                navs[k].Selected = k == i;
                navs[k].Invalidate();
            }
        }

        // ---------------------------------------------------------------- helpers

        FlowLayoutPanel Page(string titleKey)
        {
            FlowLayoutPanel flow = new FlowLayoutPanel();
            flow.Dock = DockStyle.Fill;
            flow.FlowDirection = FlowDirection.TopDown;
            flow.WrapContents = false;
            flow.AutoScroll = true;
            flow.BackColor = T.Bg;
            flow.Padding = new Padding(W(28), W(18), W(20), W(16));
            Label title = new Label();
            title.Text = L.T(titleKey); title.AutoSize = true; title.ForeColor = T.Text;
            title.Font = new Font(Font.FontFamily, Font.Size * 1.6f, FontStyle.Bold, GraphicsUnit.Pixel);
            title.Margin = new Padding(0, 0, 0, W(12));
            flow.Controls.Add(title);
            return flow;
        }

        int ContentWidth { get { return W(780) - W(200) - W(48) - W(24); } }

        Label Txt(string t)
        {
            Label l = new Label();
            l.Text = t; l.AutoSize = true; l.ForeColor = T.Text;
            l.MaximumSize = new Size(ContentWidth, 0);
            l.Margin = new Padding(0, W(5), W(8), W(3));
            return l;
        }

        Label Hint(string t)
        {
            Label l = Txt(t);
            l.ForeColor = T.SubText;
            l.Margin = new Padding(0, 0, 0, W(8));
            return l;
        }

        Label Section(string t)
        {
            Label l = Txt(t);
            l.Font = new Font(Font, FontStyle.Bold);
            l.Margin = new Padding(0, W(14), 0, W(4));
            return l;
        }

        /// <summary>Check box that wraps long text (an AutoSize CheckBox never wraps, it just gets cut off).</summary>
        CheckBox Chk(string t, bool bold = false)
        {
            CheckBox c = new CheckBox();
            c.Text = t; c.ForeColor = T.Text;
            if (bold) c.Font = new Font(Font, FontStyle.Bold);
            c.FlatStyle = FlatStyle.Flat;
            c.FlatAppearance.BorderColor = T.SubText;
            c.FlatAppearance.CheckedBackColor = T.Accent;
            c.AutoSize = false;
            c.CheckAlign = ContentAlignment.TopLeft;
            c.TextAlign = ContentAlignment.TopLeft;
            // measure against a conservative width (check glyph + inner padding), so the text never wraps
            // into a line the box has no room for; fallback fonts (Arabic, Thai...) also draw a bit taller
            int reserve = W(44);
            TextFormatFlags tf = TextFormatFlags.WordBreak | (L.IsRtl ? TextFormatFlags.RightToLeft : 0);
            Size sz = TextRenderer.MeasureText(t, c.Font, new Size(ContentWidth - reserve, 0), tf);
            c.Size = new Size(ContentWidth, Math.Max(W(24), sz.Height + W(9)));   // full width: always more room than measured
            c.Margin = new Padding(0, W(2), 0, W(2));
            return c;
        }

        FlowLayoutPanel Row()
        {
            FlowLayoutPanel r = new FlowLayoutPanel();
            r.AutoSize = true;
            r.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            r.WrapContents = false;
            r.FlowDirection = FlowDirection.LeftToRight;
            r.Margin = new Padding(0, W(2), 0, W(2));
            return r;
        }

        Button Btn(string t)
        {
            Button b = new Button();
            b.Text = t; b.AutoSize = true;
            b.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderColor = T.Line;
            b.FlatAppearance.MouseOverBackColor = T.ButtonHover;
            b.BackColor = T.Button; b.ForeColor = T.Text;
            b.Padding = new Padding(W(10), W(3), W(10), W(3));
            b.Margin = new Padding(0, 0, W(8), 0);
            return b;
        }

        TrackBar Slider(int width, string name)
        {
            TrackBar tb = new TrackBar();
            tb.Minimum = 5; tb.Maximum = 250;
            tb.SmallChange = 5; tb.LargeChange = 15;
            tb.TickStyle = TickStyle.None;
            tb.AutoSize = false;
            tb.Width = W(width); tb.Height = W(30);
            tb.BackColor = T.Bg;
            tb.AccessibleName = name;
            tb.Margin = new Padding(0, W(2), W(8), 0);
            return tb;
        }

        Label ValueLabel()
        {
            Label l = Txt("");
            l.AutoSize = false; l.Width = W(44); l.Height = W(28);
            l.TextAlign = ContentAlignment.MiddleLeft;
            return l;
        }

        static int Clamp(int v) { return Math.Max(5, Math.Min(250, v)); }

        // ---------------------------------------------------------------- General

        FlowLayoutPanel BuildGeneral()
        {
            FlowLayoutPanel p = Page("tab.general");

            chkEnabled = Chk(L.T("gen.enable"), true);
            chkEnabled.CheckedChanged += delegate { if (syncing) return; cfg.Enabled = chkEnabled.Checked; host.Changed(); };
            p.Controls.Add(chkEnabled);

            p.Controls.Add(Section(L.T("gen.darkness")));
            FlowLayoutPanel r = Row();
            tbAlpha = Slider(340, L.T("gen.darkness"));
            lblAlpha = ValueLabel();
            tbAlpha.ValueChanged += delegate
            {
                lblAlpha.Text = tbAlpha.Value.ToString();
                UpdatePreview();
                if (syncing) return;
                cfg.Alpha = tbAlpha.Value; host.Changed();
            };
            r.Controls.Add(tbAlpha); r.Controls.Add(lblAlpha);
            p.Controls.Add(r);
            p.Controls.Add(Hint(L.T("gen.darkness.tip")));

            // preview: how the active window and the rest will look
            r = Row();
            pvActive = new Label();
            pvActive.Text = L.T("gen.pv.active");
            pvActive.BackColor = Color.White; pvActive.ForeColor = Color.FromArgb(40, 40, 40);
            pvActive.TextAlign = ContentAlignment.MiddleCenter; pvActive.BorderStyle = BorderStyle.FixedSingle;
            pvActive.AutoSize = false; pvActive.Size = new Size(W(170), W(46)); pvActive.Margin = new Padding(0, 0, W(8), 0);
            pvDim = new Label();
            pvDim.Text = L.T("gen.pv.dim");
            pvDim.TextAlign = ContentAlignment.MiddleCenter; pvDim.BorderStyle = BorderStyle.FixedSingle;
            pvDim.AutoSize = false; pvDim.Size = new Size(W(170), W(46));
            r.Controls.Add(pvActive); r.Controls.Add(pvDim);
            p.Controls.Add(r);

            p.Controls.Add(Section(L.T("gen.tint")));
            r = Row();
            tintSwatch = new Panel();
            tintSwatch.Size = new Size(W(30), W(26));
            tintSwatch.BorderStyle = BorderStyle.FixedSingle;
            tintSwatch.Margin = new Padding(0, W(2), W(10), 0);
            r.Controls.Add(tintSwatch);
            r.Controls.Add(TintButton(L.T("tint.black"), Color.Black));
            r.Controls.Add(TintButton(L.T("tint.warm"), Color.FromArgb(40, 20, 0)));
            r.Controls.Add(TintButton(L.T("tint.navy"), Color.FromArgb(0, 0, 40)));
            Button custom = Btn(L.T("tint.custom"));
            custom.Click += delegate
            {
                using (ColorDialog d = new ColorDialog())
                {
                    d.Color = cfg.Tint; d.FullOpen = true;
                    if (d.ShowDialog(this) == DialogResult.OK) SetTint(d.Color);
                }
            };
            r.Controls.Add(custom);
            p.Controls.Add(r);

            p.Controls.Add(Section(L.T("gen.fade")));
            r = Row();
            tbFade = Slider(340, L.T("gen.fade"));
            tbFade.Minimum = 0; tbFade.Maximum = 2000; tbFade.SmallChange = 50; tbFade.LargeChange = 100;
            lblFade = ValueLabel(); lblFade.Width = W(64);
            tbFade.ValueChanged += delegate
            {
                int v = (tbFade.Value + 25) / 50 * 50;   // snap to 50 ms
                lblFade.Text = v + " ms";
                if (syncing) return;
                cfg.FadeMs = v; host.Changed();
            };
            r.Controls.Add(tbFade); r.Controls.Add(lblFade);
            p.Controls.Add(r);

            Label gapL = Txt(""); gapL.Height = W(6); p.Controls.Add(gapL);
            chkBorder = Chk(L.T("gen.border"));
            chkBorder.CheckedChanged += delegate { if (syncing) return; cfg.Border = chkBorder.Checked; host.Changed(); };
            p.Controls.Add(chkBorder);
            chkFocus = Chk(L.T("gen.focus"));
            chkFocus.CheckedChanged += delegate { if (syncing) return; cfg.FocusMode = chkFocus.Checked; host.Changed(); };
            p.Controls.Add(chkFocus);
            chkFull = Chk(L.T("gen.full"));
            chkFull.CheckedChanged += delegate { if (syncing) return; cfg.AutoFullscreen = chkFull.Checked; host.Changed(); };
            p.Controls.Add(chkFull);
            chkSpot = Chk(L.T("gen.spotlight"));
            chkSpot.CheckedChanged += delegate { if (syncing) return; cfg.Spotlight = chkSpot.Checked; host.Changed(); };
            p.Controls.Add(chkSpot);
            return p;
        }

        Button TintButton(string text, Color c)
        {
            Button b = Btn(text);
            b.Click += delegate { SetTint(c); };
            return b;
        }

        void SetTint(Color c)
        {
            cfg.Tint = c;
            UpdatePreview();
            host.Changed();
        }

        void UpdatePreview()
        {
            if (pvDim == null || tbAlpha == null) return;
            int a = tbAlpha.Value;
            pvDim.BackColor = UiUtil.Blend(cfg.Tint, Color.White, a);
            pvDim.ForeColor = UiUtil.Blend(cfg.Tint, Color.FromArgb(40, 40, 40), a);
            tintSwatch.BackColor = cfg.Tint;
        }

        // ---------------------------------------------------------------- Schedule

        FlowLayoutPanel BuildSchedule()
        {
            FlowLayoutPanel p = Page("tab.schedule");
            p.Controls.Add(Hint(L.T("sch.desc")));

            chkSchedule = Chk(L.T("sch.enable"));
            chkSchedule.CheckedChanged += delegate { if (syncing) return; cfg.ScheduleEnabled = chkSchedule.Checked; host.Changed(); UpdateSchedStatus(); };
            p.Controls.Add(chkSchedule);

            FlowLayoutPanel r = Row();
            tStart = new TimeBox(W(52), T); tEnd = new TimeBox(W(52), T);
            tStart.Name2 = L.T("sch.from"); tEnd.Name2 = L.T("sch.to");
            tStart.Changed += delegate { if (syncing) return; cfg.NightStart = tStart.Minutes; host.Changed(); UpdateSchedStatus(); };
            tEnd.Changed += delegate { if (syncing) return; cfg.NightEnd = tEnd.Minutes; host.Changed(); UpdateSchedStatus(); };
            r.Controls.Add(Txt(L.T("sch.from"))); r.Controls.Add(tStart);
            r.Controls.Add(Txt(L.T("sch.to"))); r.Controls.Add(tEnd);
            p.Controls.Add(r);

            p.Controls.Add(Section(L.T("sch.darkness")));
            r = Row();
            tbNight = Slider(340, L.T("sch.darkness"));
            lblNight = ValueLabel();
            tbNight.ValueChanged += delegate
            {
                lblNight.Text = tbNight.Value.ToString();
                if (syncing) return;
                cfg.NightAlpha = tbNight.Value; host.Changed();
            };
            r.Controls.Add(tbNight); r.Controls.Add(lblNight);
            p.Controls.Add(r);

            lblSchedStatus = Hint("");
            lblSchedStatus.Margin = new Padding(0, W(14), 0, 0);
            p.Controls.Add(lblSchedStatus);
            return p;
        }

        void UpdateSchedStatus()
        {
            if (lblSchedStatus == null) return;
            lblSchedStatus.Text = cfg.NightActive ? L.T("sch.status.night") : L.T("sch.status.day");
        }

        // ---------------------------------------------------------------- Monitors

        FlowLayoutPanel BuildMonitors()
        {
            FlowLayoutPanel p = Page("tab.monitors");
            p.Controls.Add(Hint(L.T("mon.desc")));

            map = new MonitorMap();
            map.T = T; map.Mons = mons; map.IsOn = cfg.EnabledFor; map.Font = Font;
            map.Size = new Size(ContentWidth, W(170));
            map.Margin = new Padding(0, 0, 0, W(8));
            map.RightToLeft = RightToLeft.No;   // the map shows real positions, never mirrored
            map.AccessibleName = L.T("tab.monitors");
            map.AccessibleDescription = L.T("mon.desc");
            map.SelectedChanged += delegate(int i) { SelectMonitor(i); };
            map.ToggleRequested += delegate { chkMonOn.Checked = !chkMonOn.Checked; };
            p.Controls.Add(map);

            lblMonTitle = Section("");
            p.Controls.Add(lblMonTitle);

            chkMonOn = Chk(L.T("mon.enable"));
            chkMonOn.CheckedChanged += delegate
            {
                if (syncing || map.Selected >= mons.Count) return;
                cfg.MonEnabled[mons[map.Selected].Key] = chkMonOn.Checked;
                host.Changed(); map.Invalidate();
            };
            p.Controls.Add(chkMonOn);

            chkMonFollow = Chk(L.T("mon.follow"));
            chkMonFollow.CheckedChanged += delegate
            {
                tbMon.Enabled = !chkMonFollow.Checked;
                if (syncing || map.Selected >= mons.Count) return;
                string key = mons[map.Selected].Key;
                if (chkMonFollow.Checked) cfg.MonAlpha.Remove(key); else cfg.MonAlpha[key] = tbMon.Value;
                host.Changed();
            };
            p.Controls.Add(chkMonFollow);

            FlowLayoutPanel r = Row();
            tbMon = Slider(340, L.T("gen.darkness"));
            lblMon = ValueLabel();
            tbMon.ValueChanged += delegate
            {
                lblMon.Text = tbMon.Value.ToString();
                if (syncing || chkMonFollow.Checked || map.Selected >= mons.Count) return;
                cfg.MonAlpha[mons[map.Selected].Key] = tbMon.Value; host.Changed();
            };
            r.Controls.Add(tbMon); r.Controls.Add(lblMon);
            p.Controls.Add(r);

            for (int i = 0; i < mons.Count; i++) if (mons[i].Primary) map.Selected = i;
            return p;
        }

        void SelectMonitor(int i)
        {
            if (i < 0 || i >= mons.Count) return;
            map.Selected = i;
            MonInfo m = mons[i];
            bool was = syncing;
            syncing = true;
            try
            {
                lblMonTitle.Text = L.T("mon.title", m.Index + 1, m.Bounds.Width, m.Bounds.Height) + (m.Primary ? "  ★" : "");
                chkMonOn.Checked = cfg.EnabledFor(m.Key);
                bool has = cfg.MonAlpha.ContainsKey(m.Key);
                chkMonFollow.Checked = !has;
                tbMon.Value = Clamp(has ? cfg.MonAlpha[m.Key] : cfg.EffectiveAlpha());
                tbMon.Enabled = has;
                lblMon.Text = tbMon.Value.ToString();
            }
            finally { syncing = was; }
            map.Invalidate();
        }

        // ---------------------------------------------------------------- Excluded apps

        FlowLayoutPanel BuildApps()
        {
            FlowLayoutPanel p = Page("tab.apps");
            p.Controls.Add(Hint(L.T("apps.desc")));

            lstExclude = new ListBox();
            lstExclude.Size = new Size(ContentWidth, W(150));
            lstExclude.SelectionMode = SelectionMode.MultiExtended;
            lstExclude.BackColor = T.Input; lstExclude.ForeColor = T.Text;
            lstExclude.BorderStyle = BorderStyle.FixedSingle;
            lstExclude.AccessibleName = L.T("tab.apps");
            lstExclude.Margin = new Padding(0, 0, 0, W(6));
            p.Controls.Add(lstExclude);

            Button rm = Btn(L.T("apps.remove"));
            rm.Click += delegate
            {
                for (int i = lstExclude.SelectedIndices.Count - 1; i >= 0; i--)
                    cfg.Exclude.RemoveAt(lstExclude.SelectedIndices[i]);
                RefreshExclude(); host.Changed();
            };
            p.Controls.Add(rm);

            p.Controls.Add(Section(L.T("apps.add")));
            p.Controls.Add(Hint(L.T("apps.hint")));
            FlowLayoutPanel r = Row();
            cmbApps = new ComboBox();
            cmbApps.DropDownStyle = ComboBoxStyle.DropDown;
            cmbApps.FlatStyle = FlatStyle.Flat;
            cmbApps.BackColor = T.Input; cmbApps.ForeColor = T.Text;
            cmbApps.Width = W(320);
            cmbApps.AccessibleName = L.T("apps.add");
            cmbApps.Margin = new Padding(0, W(2), W(8), 0);
            cmbApps.DropDown += delegate
            {
                string t = cmbApps.Text;
                cmbApps.Items.Clear();
                foreach (string n in UiUtil.RunningApps()) cmbApps.Items.Add(n);
                cmbApps.Text = t;
            };
            Button add = Btn(L.T("apps.add"));
            add.Click += delegate { AddExclude(); };
            cmbApps.KeyDown += delegate(object o, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; AddExclude(); }
            };
            r.Controls.Add(cmbApps); r.Controls.Add(add);
            p.Controls.Add(r);
            return p;
        }

        void AddExclude()
        {
            string n = cmbApps.Text.Trim();
            if (n.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) n = n.Substring(0, n.Length - 4);
            if (n.Length == 0) return;
            foreach (string x in cfg.Exclude)
                if (string.Equals(x, n, StringComparison.OrdinalIgnoreCase)) { cmbApps.Text = ""; return; }
            cfg.Exclude.Add(n);
            cmbApps.Text = "";
            RefreshExclude();
            host.Changed();
        }

        void RefreshExclude()
        {
            lstExclude.Items.Clear();
            foreach (string x in cfg.Exclude) lstExclude.Items.Add(x);
        }

        // ---------------------------------------------------------------- Hotkeys

        FlowLayoutPanel BuildHotkeys()
        {
            FlowLayoutPanel p = Page("tab.hotkeys");
            p.Controls.Add(Hint(L.T("hk.desc")));

            for (int i = 0; i < Settings.Actions.Length; i++)
            {
                string action = Settings.Actions[i];
                FlowLayoutPanel r = Row();
                Label l = Txt(L.T("act." + action));
                l.MinimumSize = new Size(W(300), W(28));
                l.MaximumSize = new Size(W(300), 0);   // long translations wrap instead of being cut off
                l.TextAlign = ContentAlignment.MiddleLeft;
                TextBox tb = new TextBox();
                tb.ReadOnly = true;
                tb.RightToLeft = RightToLeft.No;   // key names are always left-to-right
                tb.Width = W(210);
                tb.BackColor = T.Input; tb.ForeColor = T.Text; tb.BorderStyle = BorderStyle.FixedSingle;
                tb.AccessibleName = L.T("act." + action);
                tb.Margin = new Padding(0, W(3), 0, W(3));
                tb.GotFocus += delegate { host.HotkeysSuspend(); };
                tb.LostFocus += delegate { host.HotkeysResume(); };
                TextBox self = tb;
                tb.KeyDown += delegate(object o, KeyEventArgs e)
                {
                    if (e.KeyCode == Keys.Tab) return;          // keep keyboard navigation working
                    e.SuppressKeyPress = true; e.Handled = true;
                    Keys k = e.KeyCode;
                    if (k == Keys.ControlKey || k == Keys.ShiftKey || k == Keys.Menu || k == Keys.LWin || k == Keys.RWin || k == Keys.None) return;
                    uint mod = 0;
                    if (e.Control) mod |= 2;
                    if (e.Shift) mod |= 4;
                    if (e.Alt) mod |= 1;
                    if ((Native.GetKeyState(0x5B) & 0x8000) != 0 || (Native.GetKeyState(0x5C) & 0x8000) != 0) mod |= 8; // Win
                    if (mod == 0) return;
                    self.Text = Hk.Format(mod, k);
                    cfg.Hotkeys[action] = self.Text;
                    host.Changed();
                };
                hkBoxes[i] = tb;
                r.Controls.Add(l); r.Controls.Add(tb);
                p.Controls.Add(r);
            }

            Button reset = Btn(L.T("hk.reset"));
            reset.Margin = new Padding(0, W(14), 0, 0);
            reset.Click += delegate
            {
                cfg.ResetHotkeys();
                SyncHotkeys();
                host.Changed();
                host.HotkeysResume();
            };
            p.Controls.Add(reset);
            return p;
        }

        void SyncHotkeys()
        {
            for (int i = 0; i < Settings.Actions.Length; i++) hkBoxes[i].Text = cfg.HotkeyText(Settings.Actions[i]);
        }

        // ---------------------------------------------------------------- More

        FlowLayoutPanel BuildMore()
        {
            FlowLayoutPanel p = Page("tab.more");

            chkAuto = Chk(L.T("more.autostart"));
            chkAuto.CheckedChanged += delegate { if (syncing) return; host.AutoStart = chkAuto.Checked; };
            p.Controls.Add(chkAuto);

            p.Controls.Add(Section(L.T("more.bitmap")));
            FlowLayoutPanel r = Row();
            lblBitmap = Txt("");
            lblBitmap.AutoSize = false; lblBitmap.Width = W(250); lblBitmap.Height = W(28);
            lblBitmap.AutoEllipsis = true; lblBitmap.TextAlign = ContentAlignment.MiddleLeft;
            Button choose = Btn(L.T("more.bitmap.choose"));
            choose.Click += delegate
            {
                using (OpenFileDialog d = new OpenFileDialog())
                {
                    d.Filter = L.T("dlg.imagefilter");
                    if (d.ShowDialog(this) == DialogResult.OK)
                    {
                        cfg.Bitmap = d.FileName;
                        UpdateBitmapLabel();
                        host.BitmapChanged();
                    }
                }
            };
            Button clear = Btn(L.T("more.bitmap.clear"));
            clear.Click += delegate { cfg.Bitmap = ""; UpdateBitmapLabel(); host.BitmapChanged(); };
            r.Controls.Add(lblBitmap); r.Controls.Add(choose); r.Controls.Add(clear);
            p.Controls.Add(r);

            p.Controls.Add(Section(L.T("more.language")));
            cmbLang = new ComboBox();
            cmbLang.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbLang.FlatStyle = FlatStyle.Flat;
            cmbLang.BackColor = T.Input; cmbLang.ForeColor = T.Text;
            cmbLang.Width = W(300);
            cmbLang.AccessibleName = L.T("more.language");
            LangItem auto = new LangItem(); auto.Code = "auto"; auto.Name = L.T("lang.auto");
            cmbLang.Items.Add(auto);
            foreach (L.Lang l in L.Available)
            {
                LangItem it = new LangItem(); it.Code = l.Code; it.Name = l.Name;
                cmbLang.Items.Add(it);
            }
            for (int i = 0; i < cmbLang.Items.Count; i++)
                if (((LangItem)cmbLang.Items[i]).Code == cfg.Language) cmbLang.SelectedIndex = i;
            if (cmbLang.SelectedIndex < 0) cmbLang.SelectedIndex = 0;
            cmbLang.SelectionChangeCommitted += delegate
            {
                LangItem it = (LangItem)cmbLang.SelectedItem;
                if (it == null || it.Code == cfg.Language) return;
                cfg.Language = it.Code;
                host.LanguageChanged();
            };
            p.Controls.Add(cmbLang);

            Label help = Hint(L.T("about.langhelp"));
            help.Margin = new Padding(0, W(8), 0, W(6));
            p.Controls.Add(help);
            Button export = Btn(L.T("about.export"));
            export.Click += delegate
            {
                try
                {
                    string path = L.ExportTemplate();
                    MessageBox.Show(this, L.T("about.exported", path), "Blinders", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Process.Start("explorer.exe", "/select,\"" + path + "\"");
                }
                catch (Exception ex) { MessageBox.Show(this, ex.Message, "Blinders", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            };
            p.Controls.Add(export);

            p.Controls.Add(Section(" "));
            p.Controls.Add(Txt(L.T("about.desc")));
            return p;
        }

        void UpdateBitmapLabel()
        {
            lblBitmap.Text = cfg.Bitmap.Length == 0 ? L.T("more.bitmap.none") : Path.GetFileName(cfg.Bitmap);
        }

        // ---------------------------------------------------------------- sync

        /// <summary>Refresh every control from the settings (after a hotkey / menu change elsewhere).</summary>
        public void Sync()
        {
            bool was = syncing;
            syncing = true;
            try
            {
                chkEnabled.Checked = cfg.Enabled;
                tbAlpha.Value = Clamp(cfg.Alpha); lblAlpha.Text = tbAlpha.Value.ToString();
                tbFade.Value = Math.Max(0, Math.Min(2000, cfg.FadeMs)); lblFade.Text = tbFade.Value + " ms";
                chkBorder.Checked = cfg.Border;
                chkFocus.Checked = cfg.FocusMode;
                chkFull.Checked = cfg.AutoFullscreen;
                chkSpot.Checked = cfg.Spotlight;

                chkSchedule.Checked = cfg.ScheduleEnabled;
                tStart.Minutes = cfg.NightStart;
                tEnd.Minutes = cfg.NightEnd;
                tbNight.Value = Clamp(cfg.NightAlpha); lblNight.Text = tbNight.Value.ToString();

                RefreshExclude();
                SyncHotkeys();
                chkAuto.Checked = host.AutoStart;
                UpdateBitmapLabel();
            }
            finally { syncing = was; }
            UpdatePreview();
            UpdateSchedStatus();
            SelectMonitor(map.Selected);
        }
    }
}
