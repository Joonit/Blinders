using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("Blinders")]
[assembly: AssemblyProduct("Blinders")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace Blinders
{
    static class Native
    {
        public const uint SWP_NOSIZE = 0x1, SWP_NOMOVE = 0x2, SWP_NOACTIVATE = 0x10;
        public const int WM_HOTKEY = 0x312;
        public const uint MOD_NOREPEAT = 0x4000;

        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
        [DllImport("user32.dll", CharSet = CharSet.Auto)] public static extern int GetClassName(IntPtr h, StringBuilder sb, int max);
        [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr h, int id, uint mod, uint vk);
        [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr h, int id);
        [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
        [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr h);
        [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr ctx);
        [DllImport("user32.dll")] public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr ctx);
        [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
        [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr h);
        [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr h, int index);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int RegisterWindowMessage(string name);
        [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, int msg, IntPtr w, IntPtr l);
        [DllImport("user32.dll")] public static extern int GetSystemMetrics(int index);
        [DllImport("user32.dll")] public static extern short GetKeyState(int vk);
        [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr h, int attr, out RECT r, int size);
        [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr h);
        [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr h);
        [DllImport("user32.dll")] public static extern IntPtr GetAncestor(IntPtr h, uint flags);
        [DllImport("kernel32.dll")] public static extern IntPtr OpenProcess(uint access, bool inherit, uint pid);
        [DllImport("kernel32.dll")] public static extern bool CloseHandle(IntPtr h);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] public static extern bool QueryFullProcessImageName(IntPtr h, int flags, StringBuilder name, ref int size);

        public delegate void WinEventProc(IntPtr hook, uint ev, IntPtr hwnd, int idObject, int idChild, uint thread, uint time);
        [DllImport("user32.dll")] public static extern IntPtr SetWinEventHook(uint min, uint max, IntPtr mod, WinEventProc proc, uint pid, uint tid, uint flags);
        [DllImport("user32.dll")] public static extern bool UnhookWinEvent(IntPtr hook);

        /// <summary>Process name (without .exe) without creating a Process object.</summary>
        public static string ProcessName(uint pid)
        {
            IntPtr h = OpenProcess(0x1000 /*QUERY_LIMITED_INFORMATION*/, false, pid);
            if (h == IntPtr.Zero) return null;
            try
            {
                StringBuilder sb = new StringBuilder(1024);
                int size = sb.Capacity;
                return QueryFullProcessImageName(h, 0, sb, ref size) ? Path.GetFileNameWithoutExtension(sb.ToString()) : null;
            }
            finally { CloseHandle(h); }
        }
        public static readonly IntPtr HWND_TOP = IntPtr.Zero, HWND_TOPMOST = new IntPtr(-1);

        /// <summary>Visible bounds of a window (without the invisible resize borders Windows 10/11 add).</summary>
        public static bool VisibleBounds(IntPtr h, out Rectangle r)
        {
            RECT rc;
            if (DwmGetWindowAttribute(h, 9 /*DWMWA_EXTENDED_FRAME_BOUNDS*/, out rc, Marshal.SizeOf(typeof(RECT))) != 0
                && !GetWindowRect(h, out rc)) { r = Rectangle.Empty; return false; }
            r = Rectangle.FromLTRB(rc.Left, rc.Top, rc.Right, rc.Bottom);
            return r.Width > 0 && r.Height > 0;
        }
        public static readonly IntPtr HWND_BROADCAST = new IntPtr(0xffff);

        // messages between instances of Blinders
        public static readonly int MsgShowSettings = RegisterWindowMessage("Blinders.ShowSettings");
        public static readonly int MsgExit = RegisterWindowMessage("Blinders.Exit");

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left, Top, Right, Bottom; }
    }

    static class Util
    {
        public static string MonKey(Screen s)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in s.DeviceName) if (char.IsLetterOrDigit(c)) sb.Append(c);
            return sb.ToString();
        }

        /// <summary>Run an action under a thread DPI awareness context (-2 system, -4 per-monitor V2).</summary>
        public static void WithDpi(int ctx, Action a)
        {
            IntPtr prev = IntPtr.Zero;
            try { prev = Native.SetThreadDpiAwarenessContext(new IntPtr(ctx)); } catch { }
            try { a(); }
            finally
            {
                if (prev != IntPtr.Zero) { try { Native.SetThreadDpiAwarenessContext(prev); } catch { } }
            }
        }

        public static int S(Control c, int v)
        {
            // Scale by the real font size (Segoe UI 9pt = 15px at 100%): text width is what decides the layout,
            // and it stays correct whatever DPI context the window was created in.
            int h = 15;
            try { h = c.Font.Height; } catch { }
            return (int)Math.Round(v * h / 15.0);
        }

        public static string FormatTime(int minutes)
        {
            minutes = ((minutes % 1440) + 1440) % 1440;
            return (minutes / 60).ToString("00") + ":" + (minutes % 60).ToString("00");
        }

        public static int ParseTime(string v, int def)
        {
            TimeSpan t;
            if (TimeSpan.TryParse(v, CultureInfo.InvariantCulture, out t)) return ((int)t.TotalMinutes % 1440 + 1440) % 1440;
            return def;
        }

        public static FlowLayoutPanel Row()
        {
            FlowLayoutPanel r = new FlowLayoutPanel();
            r.AutoSize = true;
            r.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            r.WrapContents = false;
            r.FlowDirection = FlowDirection.LeftToRight;
            r.Margin = new Padding(0, 2, 0, 2);
            return r;
        }
    }

    /// <summary>A hotkey like "Ctrl+Shift+Q".</summary>
    class Hk
    {
        public uint Mod;      // 1 Alt, 2 Ctrl, 4 Shift, 8 Win
        public Keys Key;
        public bool Valid;

        public static Hk Parse(string s)
        {
            Hk h = new Hk();
            if (s == null) return h;
            foreach (string raw in s.Split('+'))
            {
                string t = raw.Trim();
                if (t.Length == 0) continue;
                string l = t.ToLowerInvariant();
                if (l == "ctrl" || l == "control") h.Mod |= 2;
                else if (l == "shift") h.Mod |= 4;
                else if (l == "alt") h.Mod |= 1;
                else if (l == "win") h.Mod |= 8;
                else
                {
                    try { h.Key = (Keys)Enum.Parse(typeof(Keys), t, true); h.Valid = true; }
                    catch { return new Hk(); }
                }
            }
            if (h.Mod == 0) h.Valid = false;
            return h;
        }

        public static string Format(uint mod, Keys key)
        {
            StringBuilder sb = new StringBuilder();
            if ((mod & 2) != 0) sb.Append("Ctrl+");
            if ((mod & 4) != 0) sb.Append("Shift+");
            if ((mod & 1) != 0) sb.Append("Alt+");
            if ((mod & 8) != 0) sb.Append("Win+");
            sb.Append(key.ToString());
            return sb.ToString();
        }
    }

    /// <summary>Click-through, non-activating, tinted layered window that dims one monitor.</summary>
    class DimForm : Form
    {
        public string Mon;
        public Rectangle Rect;
        public double Cur, Target;   // 0..255
        public Rectangle Hole = Rectangle.Empty;   // spotlight cut-out, in screen coordinates

        /// <summary>Spotlight mode: cut the active window out of this layer (empty = no hole).</summary>
        public void SetHole(Rectangle screenHole)
        {
            Rectangle h = Rectangle.Intersect(screenHole, Rect);
            if (h.Width <= 0 || h.Height <= 0) h = Rectangle.Empty;   // the active window is on another monitor
            if (h == Hole) return;
            Hole = h;
            Region old = Region;
            if (h.IsEmpty) Region = null;
            else
            {
                Region r = new Region(new Rectangle(0, 0, Rect.Width, Rect.Height));
                r.Exclude(new Rectangle(h.X - Rect.X, h.Y - Rect.Y, h.Width, h.Height));
                Region = r;
            }
            if (old != null) old.Dispose();
        }

        public DimForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.Black;
            AutoScaleMode = AutoScaleMode.None;
            Opacity = 0;
        }
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // LAYERED | TRANSPARENT | TOOLWINDOW | NOACTIVATE
                cp.ExStyle |= 0x80000 | 0x20 | 0x80 | 0x8000000;
                return cp;
            }
        }
    }

    /// <summary>One edge of the accent border drawn around the active window (click-through, never activates).</summary>
    class EdgeForm : Form
    {
        public EdgeForm(Color c)
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;
            BackColor = c;
            Opacity = 0.95;
            Size = new Size(1, 1);
        }
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x80000 | 0x20 | 0x80 | 0x8000000; // LAYERED | TRANSPARENT | TOOLWINDOW | NOACTIVATE
                return cp;
            }
        }
    }

    /// <summary>Hidden top-level window: receives WM_HOTKEY and the messages other Blinders instances broadcast.</summary>
    class HotkeyWindow : NativeWindow
    {
        public event Action<int> Hotkey;
        public event Action ShowRequested, ExitRequested;
        public HotkeyWindow() { CreateHandle(new CreateParams()); }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.WM_HOTKEY && Hotkey != null) Hotkey(m.WParam.ToInt32());
            else if (m.Msg == Native.MsgShowSettings && ShowRequested != null) ShowRequested();
            else if (m.Msg == Native.MsgExit && ExitRequested != null) ExitRequested();
            base.WndProc(ref m);
        }
    }

    class Settings
    {
        public static readonly string[] Actions = { "Quit", "Darker", "Brighter", "Toggle", "ExcludeApp" };
        // Ctrl+Alt+Win: practically unused by other apps, and (unlike Ctrl+Alt) never types an AltGr character.
        static readonly string[] ActionDefaults = {
            "Ctrl+Alt+Win+Q", "Ctrl+Alt+Win+Up", "Ctrl+Alt+Win+Down", "Ctrl+Alt+Win+D", "Ctrl+Alt+Win+E" };

        public int Alpha = 150;
        public bool Enabled = true;
        public string Bitmap = "";
        public List<string> Exclude = new List<string>();
        public Dictionary<string, bool> MonEnabled = new Dictionary<string, bool>();
        public Dictionary<string, int> MonAlpha = new Dictionary<string, int>();
        public Dictionary<string, string> Hotkeys = new Dictionary<string, string>();

        public bool FocusMode = false;        // keep the whole monitor of the active window bright
        public bool AutoFullscreen = true;    // don't dim the monitor that has a fullscreen app on it
        public int FadeMs = 200;
        public Color Tint = Color.Black;
        public string Language = "auto";      // "auto" = system language (English if unsupported)
        public bool Border = false;           // accent border around the active window
        public bool Spotlight = false;        // dim layer on top of everything with a hole for the active window

        public bool ScheduleEnabled = false;
        public int NightStart = 22 * 60;      // minutes from midnight
        public int NightEnd = 7 * 60;
        public int NightAlpha = 200;

        public static readonly string ExeDir = Path.GetDirectoryName(Application.ExecutablePath);

        /// <summary>Portable mode: a Blinders.ini next to the exe wins. Otherwise settings live in %APPDATA%\Blinders
        /// (the exe folder may be read-only, e.g. Program Files).</summary>
        public static readonly bool Portable = File.Exists(Path.Combine(ExeDir, "Blinders.ini"));

        public static string DataDir
        {
            get
            {
                return Portable ? ExeDir
                    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Blinders");
            }
        }

        public static string FilePath { get { return Path.Combine(DataDir, "Blinders.ini"); } }

        /// <summary>True when no settings file existed at startup.</summary>
        public static bool FirstRun;

        public bool InNight(DateTime now)
        {
            int m = now.Hour * 60 + now.Minute;
            if (NightStart < NightEnd) return m >= NightStart && m < NightEnd;
            if (NightStart > NightEnd) return m >= NightStart || m < NightEnd;
            return false;
        }

        public bool NightActive { get { return ScheduleEnabled && InNight(DateTime.Now); } }

        public int EffectiveAlpha() { return NightActive ? NightAlpha : Alpha; }

        public int AlphaFor(string mon)
        {
            int a;
            return MonAlpha.TryGetValue(mon, out a) ? a : EffectiveAlpha();
        }
        public bool EnabledFor(string mon)
        {
            bool e;
            return MonEnabled.TryGetValue(mon, out e) ? e : true;
        }

        public string HotkeyText(string action)
        {
            string v;
            if (Hotkeys.TryGetValue(action, out v) && Hk.Parse(v).Valid) return v;
            return ActionDefaults[Array.IndexOf(Actions, action)];
        }

        public void ResetHotkeys() { Hotkeys.Clear(); }

        public static Settings Load()
        {
            Settings s = new Settings();
            try
            {
                if (!File.Exists(FilePath)) { FirstRun = true; return s; }
                foreach (string raw in File.ReadAllLines(FilePath, Encoding.UTF8))
                {
                    string line = raw.Trim();
                    int eq = line.IndexOf('=');
                    if (eq <= 0 || line.StartsWith("[") || line.StartsWith(";")) continue;
                    string k = line.Substring(0, eq).Trim();
                    string v = line.Substring(eq + 1).Trim();
                    int n;
                    if (k == "Alpha") { if (int.TryParse(v, out n)) s.Alpha = Clamp(n); }
                    else if (k == "Enabled") s.Enabled = v != "0";
                    else if (k == "Bitmap") s.Bitmap = v;
                    else if (k == "Language") s.Language = v.Length > 0 ? v : "auto";
                    else if (k == "FocusMode") s.FocusMode = v == "1";
                    else if (k == "Border") s.Border = v == "1";
                    else if (k == "Spotlight") s.Spotlight = v == "1";
                    else if (k == "AutoFullscreen") s.AutoFullscreen = v != "0";
                    else if (k == "FadeMs") { if (int.TryParse(v, out n)) s.FadeMs = Math.Max(0, Math.Min(2000, n)); }
                    else if (k == "Tint")
                    {
                        string[] p = v.Split(',');
                        int r, g, b;
                        if (p.Length == 3 && int.TryParse(p[0], out r) && int.TryParse(p[1], out g) && int.TryParse(p[2], out b))
                            s.Tint = Color.FromArgb(Math.Max(0, Math.Min(255, r)), Math.Max(0, Math.Min(255, g)), Math.Max(0, Math.Min(255, b)));
                    }
                    else if (k == "ScheduleEnabled") s.ScheduleEnabled = v == "1";
                    else if (k == "NightStart") s.NightStart = Util.ParseTime(v, s.NightStart);
                    else if (k == "NightEnd") s.NightEnd = Util.ParseTime(v, s.NightEnd);
                    else if (k == "NightAlpha") { if (int.TryParse(v, out n)) s.NightAlpha = Clamp(n); }
                    else if (k == "Exclude")
                    {
                        foreach (string p in v.Split(','))
                            if (p.Trim().Length > 0) s.Exclude.Add(p.Trim());
                    }
                    else if (k.StartsWith("Hotkey."))
                    {
                        string act = k.Substring(7);
                        if (Array.IndexOf(Actions, act) >= 0 && Hk.Parse(v).Valid) s.Hotkeys[act] = v;
                    }
                    else if (k.StartsWith("Monitor."))
                    {
                        string rest = k.Substring(8);
                        int dot = rest.LastIndexOf('.');
                        if (dot <= 0) continue;
                        string mon = rest.Substring(0, dot), prop = rest.Substring(dot + 1);
                        if (prop == "Enabled") s.MonEnabled[mon] = v != "0";
                        else if (prop == "Alpha" && int.TryParse(v, out n)) s.MonAlpha[mon] = Clamp(n);
                    }
                }
            }
            catch { }
            return s;
        }

        public void Save()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("[Blinders]");
                sb.AppendLine("Alpha=" + Alpha);
                sb.AppendLine("Enabled=" + (Enabled ? "1" : "0"));
                sb.AppendLine("Bitmap=" + Bitmap);
                sb.AppendLine("Language=" + Language);
                sb.AppendLine("FocusMode=" + (FocusMode ? "1" : "0"));
                sb.AppendLine("Border=" + (Border ? "1" : "0"));
                sb.AppendLine("Spotlight=" + (Spotlight ? "1" : "0"));
                sb.AppendLine("AutoFullscreen=" + (AutoFullscreen ? "1" : "0"));
                sb.AppendLine("FadeMs=" + FadeMs);
                sb.AppendLine("Tint=" + Tint.R + "," + Tint.G + "," + Tint.B);
                sb.AppendLine("ScheduleEnabled=" + (ScheduleEnabled ? "1" : "0"));
                sb.AppendLine("NightStart=" + Util.FormatTime(NightStart));
                sb.AppendLine("NightEnd=" + Util.FormatTime(NightEnd));
                sb.AppendLine("NightAlpha=" + NightAlpha);
                sb.AppendLine("Exclude=" + string.Join(",", Exclude.ToArray()));
                foreach (KeyValuePair<string, string> kv in Hotkeys)
                    sb.AppendLine("Hotkey." + kv.Key + "=" + kv.Value);
                foreach (KeyValuePair<string, bool> kv in MonEnabled)
                    sb.AppendLine("Monitor." + kv.Key + ".Enabled=" + (kv.Value ? "1" : "0"));
                foreach (KeyValuePair<string, int> kv in MonAlpha)
                    sb.AppendLine("Monitor." + kv.Key + ".Alpha=" + kv.Value);
                Directory.CreateDirectory(DataDir);
                File.WriteAllText(FilePath, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        public static int Clamp(int a) { return Math.Max(5, Math.Min(250, a)); }
    }


    // ------------------------------------------------------------------ main logic

    class BlindersContext : ApplicationContext, ISettingsHost
    {
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        static readonly int[] Presets = { 50, 100, 150, 200, 230 };

        Settings cfg;
        NotifyIcon tray;
        ContextMenuStrip menu;
        Timer tickTimer, fadeTimer, rebuildTimer, saveTimer;
        HotkeyWindow hk;
        List<DimForm> forms = new List<DimForm>();
        Image tile;
        uint ownPid;
        IntPtr lastFg = IntPtr.Zero;
        DateTime pauseUntil = DateTime.MinValue;
        SettingsForm settingsForm;
        bool hotkeysRegistered;
        string lastFailedHotkeys = "";
        int trayState = -1;
        EdgeForm[] edges;
        Rectangle edgeRect = Rectangle.Empty;
        Native.WinEventProc winEventProc;   // kept alive: the hook calls it from native code
        List<IntPtr> hooks = new List<IntPtr>();
        bool updating;
        Dictionary<uint, string> procNames = new Dictionary<uint, string>();
        bool lastDark = Theme.SystemIsDark();

        public BlindersContext(Settings s)
        {
            cfg = s;
            ownPid = (uint)Process.GetCurrentProcess().Id;

            menu = new ContextMenuStrip();
            menu.Opening += delegate { BuildMenu(); };

            tray = new NotifyIcon();
            SetTrayState(Art.On);
            tray.Text = "Blinders";
            tray.ContextMenuStrip = menu;
            tray.Visible = true;
            tray.MouseClick += delegate(object o, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left) ShowSettings();
            };

            hk = new HotkeyWindow();
            hk.Hotkey += OnHotkey;
            hk.ShowRequested += delegate { ShowSettings(); };   // someone started Blinders again
            hk.ExitRequested += delegate { ExitThread(); };      // a newer build is taking over
            RegisterHotkeys();

            SystemEvents.DisplaySettingsChanged += OnDisplayChange;
            SystemEvents.PowerModeChanged += OnPower;
            SystemEvents.SessionSwitch += OnSession;
            SystemEvents.UserPreferenceChanged += OnUserPreference;

            edges = new EdgeForm[4];
            for (int i = 0; i < 4; i++) edges[i] = new EdgeForm(Art.AccentColor);

            rebuildTimer = new Timer(); rebuildTimer.Interval = 600;
            rebuildTimer.Tick += delegate { rebuildTimer.Stop(); Rebuild(); };

            saveTimer = new Timer(); saveTimer.Interval = 500;
            saveTimer.Tick += delegate { saveTimer.Stop(); cfg.Save(); };

            fadeTimer = new Timer(); fadeTimer.Interval = 30;
            fadeTimer.Tick += delegate { FadeStep(); };

            LoadTile();
            Rebuild();

            // Event-driven: react the moment the foreground window changes, moves or a window appears.
            // The timer is only a safety net (schedule, pause expiry, z-order drift).
            winEventProc = OnWinEvent;
            uint[][] ranges = {
                new uint[] { 0x0003, 0x0003 },   // EVENT_SYSTEM_FOREGROUND
                new uint[] { 0x0016, 0x0017 },   // EVENT_SYSTEM_MINIMIZESTART..END
                new uint[] { 0x000B, 0x000B },   // EVENT_SYSTEM_MOVESIZEEND
                new uint[] { 0x8002, 0x8002 },   // EVENT_OBJECT_SHOW
                new uint[] { 0x800B, 0x800B } }; // EVENT_OBJECT_LOCATIONCHANGE
            foreach (uint[] r in ranges)
            {
                IntPtr h = Native.SetWinEventHook(r[0], r[1], IntPtr.Zero, winEventProc, 0, 0, 0x0002 /*OUTOFCONTEXT|SKIPOWNPROCESS*/);
                if (h != IntPtr.Zero) hooks.Add(h);
            }

            tickTimer = new Timer();
            tickTimer.Interval = hooks.Count > 0 ? 500 : 150;
            tickTimer.Tick += delegate { UpdateState(); };
            tickTimer.Start();
        }

        // ---------- windows ----------

        void OnDisplayChange(object s, EventArgs e) { rebuildTimer.Stop(); rebuildTimer.Start(); }
        void OnPower(object s, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume) { rebuildTimer.Stop(); rebuildTimer.Start(); }
        }
        void OnSession(object s, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionUnlock || e.Reason == SessionSwitchReason.ConsoleConnect
                || e.Reason == SessionSwitchReason.RemoteConnect)
            { rebuildTimer.Stop(); rebuildTimer.Start(); }
        }

        /// <summary>Windows switched between light and dark: redraw the settings window in the new theme.</summary>
        void OnUserPreference(object s, UserPreferenceChangedEventArgs e)
        {
            bool dark = Theme.SystemIsDark();
            if (dark == lastDark) return;
            lastDark = dark;
            if (settingsForm != null && !settingsForm.IsDisposed) settingsForm.BeginInvoke(new Action(RebuildSettings));
        }

        void Rebuild() { Util.WithDpi(-4, RebuildCore); }

        void RebuildCore()
        {
            foreach (DimForm f in forms) { f.Hide(); f.Dispose(); }
            forms.Clear();
            foreach (Screen sc in Screen.AllScreens)
            {
                DimForm f = new DimForm();
                f.Mon = Util.MonKey(sc);
                f.Rect = sc.Bounds;
                f.Bounds = sc.Bounds;
                f.BackColor = cfg.Tint;
                ApplyTile(f);
                forms.Add(f);
            }
            UpdateState();
        }

        void ApplyTint()
        {
            foreach (DimForm f in forms) f.BackColor = cfg.Tint;
        }

        void LoadTile()
        {
            if (tile != null) { tile.Dispose(); tile = null; }
            try
            {
                if (cfg.Bitmap.Length > 0 && File.Exists(cfg.Bitmap))
                    tile = new Bitmap(cfg.Bitmap);
            }
            catch { tile = null; }
        }

        void ApplyTile(DimForm f)
        {
            f.BackgroundImage = tile;
            f.BackgroundImageLayout = ImageLayout.Tile;
        }

        bool IsPaused { get { return DateTime.Now < pauseUntil; } }

        void OnWinEvent(IntPtr hook, uint ev, IntPtr hwnd, int idObject, int idChild, uint thread, uint time)
        {
            if (idObject != 0 || idChild != 0 || hwnd == IntPtr.Zero) return;   // whole windows only
            if (ev == 0x800B && hwnd != lastFg) return;                          // only the active window moving
            if (ev == 0x8002 && Native.GetAncestor(hwnd, 2 /*GA_ROOT*/) != hwnd) return;
            UpdateState();
        }

        string ProcName(uint pid)
        {
            string n;
            if (procNames.TryGetValue(pid, out n)) return n;
            if (procNames.Count > 256) procNames.Clear();
            n = Native.ProcessName(pid) ?? "";
            procNames[pid] = n;
            return n;
        }

        bool IsExcluded(uint pid)
        {
            if (cfg.Exclude.Count == 0) return false;
            string name = ProcName(pid);
            foreach (string e in cfg.Exclude)
                if (string.Equals(e, name, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        static string ClassOf(IntPtr h)
        {
            StringBuilder sb = new StringBuilder(64);
            Native.GetClassName(h, sb, 64);
            return sb.ToString();
        }

        static bool IsDesktop(IntPtr h)
        {
            string c = ClassOf(h);
            return c == "Progman" || c == "WorkerW";
        }

        /// <summary>Borderless window covering a whole monitor (games, F11 video, presentations).</summary>
        static bool IsFullscreen(IntPtr h)
        {
            try
            {
                if (ClassOf(h) == "Shell_TrayWnd") return false;
                int style = Native.GetWindowLong(h, -16);
                if ((style & 0xC00000) != 0) return false; // has a title bar -> normal/maximized window
                Native.RECT r;
                if (!Native.GetWindowRect(h, out r)) return false;
                Rectangle b = Screen.FromHandle(h).Bounds;
                return r.Left <= b.Left && r.Top <= b.Top && r.Right >= b.Right && r.Bottom >= b.Bottom;
            }
            catch { return false; }
        }

        // Dialogs run under the system DPI context; the dim logic must always see real per-monitor pixels.
        void UpdateState()
        {
            if (updating) return;   // window events can arrive while we move our own windows
            updating = true;
            try { Util.WithDpi(-4, UpdateStateCore); }
            finally { updating = false; }
        }

        void UpdateStateCore()
        {
            IntPtr fg = Native.GetForegroundWindow();
            uint pid = 0;
            if (fg != IntPtr.Zero) Native.GetWindowThreadProcessId(fg, out pid);
            IntPtr realFg = fg;
            bool ownFg = fg != IntPtr.Zero && pid == ownPid;   // our settings window, a dialog or the tray menu

            if (fg == IntPtr.Zero || pid == ownPid)
            {
                // tray menu / dialogs / window switch in progress: keep using the last real foreground window
                if (lastFg != IntPtr.Zero && Native.IsWindow(lastFg)) { fg = lastFg; Native.GetWindowThreadProcessId(fg, out pid); }
                else fg = IntPtr.Zero;
            }
            else lastFg = fg;

            bool dim = cfg.Enabled && !IsPaused && fg != IntPtr.Zero && !IsDesktop(fg) && !IsExcluded(pid);

            // A fullscreen window (or focus-monitor mode) only exempts ITS OWN monitor;
            // the other monitors keep dimming.
            bool skipOwnMonitor = dim && (cfg.FocusMode || (cfg.AutoFullscreen && IsFullscreen(fg)));
            Rectangle fgBounds = Rectangle.Empty;
            if (skipOwnMonitor) fgBounds = Screen.FromHandle(fg).Bounds;

            // Spotlight: the layers sit above everything (taskbar, always-on-top windows) with a hole for the active window.
            Rectangle hole = Rectangle.Empty;
            // (when our own settings window is active, the hole follows it so it is never covered)
            if (dim && cfg.Spotlight) Native.VisibleBounds(ownFg ? realFg : fg, out hole);

            bool needFade = false;
            foreach (DimForm f in forms)
            {
                f.SetHole(cfg.Spotlight ? hole : Rectangle.Empty);
                bool on = dim && cfg.EnabledFor(f.Mon) && !(skipOwnMonitor && f.Rect == fgBounds);
                f.Target = on ? cfg.AlphaFor(f.Mon) : 0;
                if (f.Target > 0 || f.Cur > 0)
                {
                    bool wasVisible = f.Visible;
                    if (!wasVisible) f.Show();
                    // spotlight + our own menu/dialog active: don't re-raise the layer over it
                    if (!(cfg.Spotlight && ownFg && wasVisible)) Place(f, fg);
                }
                if (Math.Abs(f.Cur - f.Target) > 0.01) needFade = true;
            }

            bool showBorder = cfg.Border && cfg.Enabled && !IsPaused && fg != IntPtr.Zero && !IsDesktop(fg)
                              && !Native.IsIconic(fg) && !(cfg.AutoFullscreen && IsFullscreen(fg));
            UpdateBorder(fg, showBorder);
            if (needFade)
            {
                if (cfg.FadeMs <= 0) FadeStep();
                else if (!fadeTimer.Enabled) fadeTimer.Start();
            }

            SetTrayState(!cfg.Enabled ? Art.Off : IsPaused ? Art.Paused : Art.On);
            string t = "Blinders";
            if (!cfg.Enabled) t += " (" + L.T("tray.off") + ")";
            else if (IsPaused) t += " (" + L.T("tray.paused") + ")";
            else t += " (" + cfg.EffectiveAlpha() + (cfg.NightActive ? ", " + L.T("menu.night") + ")" : ")");
            if (tray.Text != t) tray.Text = t;
        }

        /// <summary>Put the dim window directly behind the foreground window, on its exact pixel rect.</summary>
        void Place(DimForm f, IntPtr fg)
        {
            if (fg == IntPtr.Zero) return;
            Rectangle want = f.Rect;
            Native.RECT cur;
            Native.GetWindowRect(f.Handle, out cur);
            bool moved = cur.Left != want.X || cur.Top != want.Y
                      || cur.Right - cur.Left != want.Width || cur.Bottom - cur.Top != want.Height;
            // normal: directly behind the active window. spotlight: above everything (the hole shows the active window)
            IntPtr after = cfg.Spotlight ? Native.HWND_TOPMOST : fg;
            if (moved)
                Native.SetWindowPos(f.Handle, after, want.X, want.Y, want.Width, want.Height, Native.SWP_NOACTIVATE);
            else
                Native.SetWindowPos(f.Handle, after, 0, 0, 0, 0,
                    Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);
        }

        /// <summary>Accent frame drawn just inside the active window's visible edges, directly above it.</summary>
        void UpdateBorder(IntPtr fg, bool show)
        {
            Rectangle r;
            if (!show || !Native.VisibleBounds(fg, out r))
            {
                if (edgeRect != Rectangle.Empty) { foreach (EdgeForm e in edges) e.Hide(); edgeRect = Rectangle.Empty; }
                return;
            }
            uint wdpi = 96;
            try { wdpi = Native.GetDpiForWindow(fg); } catch { }
            int t = Math.Max(2, (int)Math.Round(3 * (wdpi == 0 ? 96 : wdpi) / 96.0));
            Rectangle[] parts = {
                new Rectangle(r.Left, r.Top, r.Width, t),
                new Rectangle(r.Left, r.Bottom - t, r.Width, t),
                new Rectangle(r.Left, r.Top + t, t, r.Height - 2 * t),
                new Rectangle(r.Right - t, r.Top + t, t, r.Height - 2 * t) };
            bool moved = r != edgeRect;
            edgeRect = r;
            for (int i = 0; i < 4; i++)
            {
                EdgeForm e = edges[i];
                if (!e.Visible) e.Show();
                // above the active window; in spotlight mode the dim layer is topmost, so the frame must be too
                IntPtr after = cfg.Spotlight ? Native.HWND_TOPMOST : Native.HWND_TOP;
                if (moved)
                    Native.SetWindowPos(e.Handle, after, parts[i].X, parts[i].Y, parts[i].Width, parts[i].Height, Native.SWP_NOACTIVATE);
                else
                    Native.SetWindowPos(e.Handle, after, 0, 0, 0, 0, Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);
            }
        }

        void FadeStep()
        {
            bool any = false;
            double step = cfg.FadeMs <= 0 ? 255 : 255.0 * fadeTimer.Interval / cfg.FadeMs;
            foreach (DimForm f in forms)
            {
                if (f.Cur != f.Target)
                {
                    if (Math.Abs(f.Target - f.Cur) <= step) f.Cur = f.Target;
                    else f.Cur += f.Cur < f.Target ? step : -step;
                    f.Opacity = Math.Max(0, Math.Min(1, f.Cur / 255.0));
                    if (f.Cur != f.Target) any = true;
                }
                if (f.Cur <= 0 && f.Target <= 0 && f.Visible) f.Hide();
            }
            if (!any) fadeTimer.Stop();
        }

        // ---------- actions ----------

        void RegisterHotkeys()
        {
            UnregisterHotkeys();
            List<string> failed = new List<string>();
            for (int i = 0; i < Settings.Actions.Length; i++)
            {
                Hk h = Hk.Parse(cfg.HotkeyText(Settings.Actions[i]));
                if (!h.Valid || !Native.RegisterHotKey(hk.Handle, i + 1, h.Mod | Native.MOD_NOREPEAT, (uint)h.Key))
                    failed.Add(L.T("act." + Settings.Actions[i]));
            }
            hotkeysRegistered = true;
            string failedText = string.Join(", ", failed.ToArray());
            if (failed.Count > 0 && failedText != lastFailedHotkeys)
                tray.ShowBalloonTip(3000, "Blinders", L.T("bal.conflict", failedText), ToolTipIcon.Warning);
            lastFailedHotkeys = failedText;
        }

        void UnregisterHotkeys()
        {
            if (!hotkeysRegistered) return;
            for (int i = 1; i <= Settings.Actions.Length; i++) Native.UnregisterHotKey(hk.Handle, i);
            hotkeysRegistered = false;
        }

        void OnHotkey(int id)
        {
            switch (Settings.Actions[id - 1])
            {
                case "Quit": ExitThread(); break;
                case "Darker": AdjustAlpha(15); break;
                case "Brighter": AdjustAlpha(-15); break;
                case "Toggle": ToggleEnabled(); break;
                case "ExcludeApp": ToggleExcludeCurrent(); break;
            }
        }

        void SettingsChanged()
        {
            ApplyTint();
            saveTimer.Stop(); saveTimer.Start();
            UpdateState();
        }

        void SyncSettingsForm()
        {
            if (settingsForm != null && !settingsForm.IsDisposed) settingsForm.Sync();
        }

        void AdjustAlpha(int delta)
        {
            if (cfg.NightActive) cfg.NightAlpha = Settings.Clamp(cfg.NightAlpha + delta);
            else cfg.Alpha = Settings.Clamp(cfg.Alpha + delta);
            SettingsChanged();
            SyncSettingsForm();
        }

        void SetAlpha(int a)
        {
            if (cfg.NightActive) cfg.NightAlpha = Settings.Clamp(a);
            else cfg.Alpha = Settings.Clamp(a);
            SettingsChanged();
            SyncSettingsForm();
        }

        void ToggleEnabled()
        {
            cfg.Enabled = !cfg.Enabled;
            pauseUntil = DateTime.MinValue;
            SettingsChanged();
            SyncSettingsForm();
        }

        void PauseFor(int minutes)
        {
            pauseUntil = minutes <= 0 ? DateTime.MinValue : DateTime.Now.AddMinutes(minutes);
            UpdateState();
        }

        void ToggleExcludeCurrent()
        {
            try
            {
                IntPtr fg = Native.GetForegroundWindow();
                uint pid;
                Native.GetWindowThreadProcessId(fg, out pid);
                if (pid == 0 || pid == ownPid) return;
                string name = Native.ProcessName(pid);
                if (string.IsNullOrEmpty(name)) return;
                int idx = cfg.Exclude.FindIndex(delegate(string x) { return string.Equals(x, name, StringComparison.OrdinalIgnoreCase); });
                if (idx >= 0) { cfg.Exclude.RemoveAt(idx); tray.ShowBalloonTip(1500, "Blinders", L.T("bal.unexcluded", name), ToolTipIcon.Info); }
                else { cfg.Exclude.Add(name); tray.ShowBalloonTip(1500, "Blinders", L.T("bal.excluded", name), ToolTipIcon.Info); }
                SettingsChanged();
                SyncSettingsForm();
            }
            catch { }
        }

        static bool AutoStartOn()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RunKey))
                    return k != null && k.GetValue("Blinders") != null;
            }
            catch { return false; }
        }

        static void SetAutoStart(bool on)
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (k == null) return;
                    if (on) k.SetValue("Blinders", "\"" + Application.ExecutablePath + "\"");
                    else k.DeleteValue("Blinders", false);
                }
            }
            catch { }
        }

        public void ShowSettings()
        {
            if (settingsForm == null || settingsForm.IsDisposed)
                settingsForm = new SettingsForm(cfg, this, null);   // sized for the monitor under the mouse
            else settingsForm.Sync();
            settingsForm.Show();
            if (settingsForm.WindowState == FormWindowState.Minimized) settingsForm.WindowState = FormWindowState.Normal;
            settingsForm.Activate();
        }

        // ---------- ISettingsHost (called by the settings window) ----------

        public void Changed() { SettingsChanged(); }
        public void HotkeysSuspend() { UnregisterHotkeys(); }
        public void HotkeysResume() { if (!hotkeysRegistered) RegisterHotkeys(); }
        public bool AutoStart { get { return AutoStartOn(); } set { SetAutoStart(value); } }

        public void BitmapChanged()
        {
            LoadTile();
            Rebuild();
            SettingsChanged();
        }

        public void LanguageChanged()
        {
            L.Init(cfg.Language);
            SettingsChanged();
            // rebuild the window in the new language, after the current event has finished
            if (settingsForm != null && !settingsForm.IsDisposed)
                settingsForm.BeginInvoke(new Action(RebuildSettings));
        }

        /// <summary>Recreate the settings window in place (new language, theme or monitor DPI).</summary>
        public void RebuildSettings()
        {
            if (settingsForm == null || settingsForm.IsDisposed) return;
            Point loc = settingsForm.Location;
            int tab = settingsForm.SelectedTab;
            settingsForm.Close();
            settingsForm.Dispose();
            settingsForm = new SettingsForm(cfg, this, loc);
            settingsForm.SelectedTab = tab;
            settingsForm.Show();
            settingsForm.Activate();
        }

        // ---------- menu ----------

        ToolStripMenuItem Item(string text, bool check, EventHandler click)
        {
            ToolStripMenuItem it = new ToolStripMenuItem(text);
            it.Checked = check;
            if (click != null) it.Click += click;
            return it;
        }

        void BuildMenu()
        {
            menu.Items.Clear();
            menu.Items.Add(Item(L.T("menu.settings"), false, delegate { ShowSettings(); }));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(Item(L.T("menu.toggle") + "\t" + cfg.HotkeyText("Toggle"), cfg.Enabled, delegate { ToggleEnabled(); }));

            ToolStripMenuItem pause = new ToolStripMenuItem(IsPaused ? L.T("menu.paused", pauseUntil.ToString("HH:mm")) : L.T("menu.pause"));
            pause.DropDownItems.Add(Item(L.T("menu.p10"), false, delegate { PauseFor(10); }));
            pause.DropDownItems.Add(Item(L.T("menu.p60"), false, delegate { PauseFor(60); }));
            pause.DropDownItems.Add(Item(L.T("menu.resume"), false, delegate { PauseFor(0); }));
            menu.Items.Add(pause);

            ToolStripMenuItem bright = new ToolStripMenuItem(
                L.T("menu.darkness", cfg.EffectiveAlpha() + (cfg.NightActive ? ", " + L.T("menu.night") : "")));
            bright.DropDownItems.Add(Item(L.T("act.Darker") + "\t" + cfg.HotkeyText("Darker"), false, delegate { AdjustAlpha(15); }));
            bright.DropDownItems.Add(Item(L.T("act.Brighter") + "\t" + cfg.HotkeyText("Brighter"), false, delegate { AdjustAlpha(-15); }));
            bright.DropDownItems.Add(new ToolStripSeparator());
            foreach (int p in Presets)
            {
                int pv = p;
                bright.DropDownItems.Add(Item(pv.ToString(), cfg.EffectiveAlpha() == pv, delegate { SetAlpha(pv); }));
            }
            menu.Items.Add(bright);

            menu.Items.Add(Item(L.T("menu.focus"), cfg.FocusMode, delegate { cfg.FocusMode = !cfg.FocusMode; SettingsChanged(); SyncSettingsForm(); }));
            menu.Items.Add(Item(L.T("menu.full"), cfg.AutoFullscreen, delegate { cfg.AutoFullscreen = !cfg.AutoFullscreen; SettingsChanged(); SyncSettingsForm(); }));

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(Item(L.T("more.autostart"), AutoStartOn(), delegate { SetAutoStart(!AutoStartOn()); SyncSettingsForm(); }));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(Item(L.T("act.Quit") + "\t" + cfg.HotkeyText("Quit"), false, delegate { ExitThread(); }));
        }

        // ---------- lifecycle ----------

        /// <summary>Tray icon shows the state: on / off (outline) / paused (bars). Sized for the tray's DPI.</summary>
        void SetTrayState(int state)
        {
            if (state == trayState) return;
            trayState = state;
            int size = Native.GetSystemMetrics(49); // SM_CXSMICON
            if (size <= 0) size = 16;
            Icon old = tray.Icon;
            tray.Icon = Art.MakeIcon(state, size);
            if (old != null) old.Dispose();
        }

        /// <summary>First run: Windows hides new tray icons, so open the settings and say where Blinders lives.</summary>
        public void Welcome()
        {
            ShowSettings();
            tray.ShowBalloonTip(6000, "Blinders", L.T("bal.firstrun"), ToolTipIcon.Info);
        }

        protected override void ExitThreadCore()
        {
            tickTimer.Stop(); fadeTimer.Stop(); rebuildTimer.Stop(); saveTimer.Stop();
            SystemEvents.DisplaySettingsChanged -= OnDisplayChange;
            SystemEvents.PowerModeChanged -= OnPower;
            SystemEvents.SessionSwitch -= OnSession;
            SystemEvents.UserPreferenceChanged -= OnUserPreference;
            foreach (IntPtr h in hooks) Native.UnhookWinEvent(h);
            hooks.Clear();
            cfg.Save();
            foreach (EdgeForm e in edges) e.Dispose();
            UnregisterHotkeys();
            tray.Visible = false;
            tray.Dispose();
            if (settingsForm != null && !settingsForm.IsDisposed) settingsForm.Dispose();
            foreach (DimForm f in forms) f.Dispose();
            base.ExitThreadCore();
        }
    }

    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Per-monitor DPI awareness (V2) so every monitor is addressed in real pixels
            try { if (!Native.SetProcessDpiAwarenessContext(new IntPtr(-4))) Native.SetProcessDPIAware(); }
            catch { Native.SetProcessDPIAware(); }

            // --write-icon <path>: regenerate the app icon used by build.bat
            if (args.Length == 2 && args[0] == "--write-icon")
            {
                File.WriteAllBytes(args[1], Art.IcoBytes(Art.AppSizes, Art.On));
                return;
            }

            // Single instance. Same build already running -> ask it to show its settings and stop here.
            // Different build (an update) -> ask the old one to exit cleanly (it saves), kill it only if it hangs.
            using (Process me = Process.GetCurrentProcess())
            {
                string myPath = Application.ExecutablePath;
                string myVer = FileVersionInfo.GetVersionInfo(myPath).FileVersion;
                foreach (Process p in Process.GetProcessesByName(me.ProcessName))
                {
                    using (p)
                    {
                        if (p.Id == me.Id) continue;
                        bool same = false;
                        try
                        {
                            same = string.Equals(p.MainModule.FileName, myPath, StringComparison.OrdinalIgnoreCase)
                                && p.MainModule.FileVersionInfo.FileVersion == myVer;
                        }
                        catch { }
                        if (same)
                        {
                            Native.PostMessage(Native.HWND_BROADCAST, Native.MsgShowSettings, IntPtr.Zero, IntPtr.Zero);
                            return;
                        }
                        Native.PostMessage(Native.HWND_BROADCAST, Native.MsgExit, IntPtr.Zero, IntPtr.Zero);
                        try { if (!p.WaitForExit(3000)) { p.Kill(); p.WaitForExit(2000); } } catch { }
                    }
                }
            }

            Settings cfg = Settings.Load();
            L.Init(cfg.Language);
            // Command line: Blinders.exe [--settings] [alpha] [bitmap]  (alpha/bitmap override saved values for this run)
            bool openSettings = false;
            List<string> rest = new List<string>();
            foreach (string x in args)
            {
                if (x == "--settings") openSettings = true; else rest.Add(x);
            }
            int a;
            if (rest.Count > 0 && int.TryParse(rest[0], out a)) cfg.Alpha = Settings.Clamp(a);
            if (rest.Count > 1) cfg.Bitmap = rest[1];

            Application.EnableVisualStyles();
            BlindersContext ctx = new BlindersContext(cfg);
            if (Settings.FirstRun) ctx.Welcome();
            else if (openSettings) ctx.ShowSettings();
            Application.Run(ctx);
        }
    }
}
