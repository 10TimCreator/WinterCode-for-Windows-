using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace WinterCode3В4s
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new WinterIDE());
        }
    }

    public static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);
        public const int WM_SETREDRAW = 0x000B;
        public const int WM_USER = 0x400;
        public const int EM_GETEVENTMASK = WM_USER + 59;
        public const int EM_SETEVENTMASK = WM_USER + 69;
        public const int WM_PAINT = 0x000F;
    }

    public class WinterAudio
    {
        [DllImport("winmm.dll")]
        private static extern long mciSendString(string command, StringBuilder returnValue, int returnLength, IntPtr winHandle);

        private Dictionary<string, string> loadedTracks = new Dictionary<string, string>();

        public void LoadAudio(string alias, string pathOrUrl)
        {
            string finalPath = pathOrUrl;
            if (pathOrUrl.StartsWith("http://") || pathOrUrl.StartsWith("https://"))
            {
                string tempFile = Path.Combine(Path.GetTempPath(), $"winter_audio_{alias}.mp3");
                try
                {
                    using (WebClient client = new WebClient())
                    {
                        client.DownloadFile(pathOrUrl, tempFile);
                    }
                    finalPath = tempFile;
                }
                catch (Exception ex) { throw new Exception($"Failed to download audio: {ex.Message}"); }
            }

            mciSendString($"close {alias}", null, 0, IntPtr.Zero);
            long result = mciSendString($"open \"{finalPath}\" type mpegvideo alias {alias}", null, 0, IntPtr.Zero);
            if (result != 0) throw new Exception($"Audio load failed. Code: {result}");

            loadedTracks[alias] = finalPath;
        }

        public void PlayAudio(string alias, double startSeconds = 0)
        {
            if (!loadedTracks.ContainsKey(alias)) return;
            mciSendString($"play {alias} from {(int)(startSeconds * 1000)}", null, 0, IntPtr.Zero);
        }

        public void PauseAudio(string alias) { mciSendString($"pause {alias}", null, 0, IntPtr.Zero); }
        public void StopAudio(string alias) { mciSendString($"stop {alias}", null, 0, IntPtr.Zero); }
        public void SetVolume(string alias, int volume) { mciSendString($"setaudio {alias} volume to {volume}", null, 0, IntPtr.Zero); }

        public void StopAll()
        {
            foreach (var alias in loadedTracks.Keys)
            {
                mciSendString($"stop {alias}", null, 0, IntPtr.Zero);
                mciSendString($"close {alias}", null, 0, IntPtr.Zero);
            }
            loadedTracks.Clear();
        }
    }

    public struct Vector3
    {
        public float X, Y, Z;
        public Vector3(float x, float y, float z) { X = x; Y = y; Z = z; }
    }

    public class Matrix4x4
    {
        public float[,] m = new float[4, 4];

        public static Matrix4x4 Identity()
        {
            Matrix4x4 mat = new Matrix4x4();
            mat.m[0, 0] = 1.0f; mat.m[1, 1] = 1.0f; mat.m[2, 2] = 1.0f; mat.m[3, 3] = 1.0f;
            return mat;
        }

        public static Matrix4x4 Projection(float fov, float aspect, float zNear, float zFar)
        {
            float fovRad = 1.0f / (float)Math.Tan(fov * 0.5f / 180.0f * Math.PI);
            Matrix4x4 mat = new Matrix4x4();
            mat.m[0, 0] = aspect * fovRad; mat.m[1, 1] = fovRad; mat.m[2, 2] = zFar / (zFar - zNear);
            mat.m[3, 2] = (-zFar * zNear) / (zFar - zNear); mat.m[2, 3] = 1.0f; mat.m[3, 3] = 0.0f;
            return mat;
        }

        public static Matrix4x4 RotationX(float angle)
        {
            Matrix4x4 mat = new Matrix4x4();
            mat.m[0, 0] = 1; mat.m[1, 1] = (float)Math.Cos(angle);
            mat.m[1, 2] = (float)Math.Sin(angle); mat.m[2, 1] = -(float)Math.Sin(angle);
            mat.m[2, 2] = (float)Math.Cos(angle); mat.m[3, 3] = 1;
            return mat;
        }

        public static Matrix4x4 RotationY(float angle)
        {
            Matrix4x4 mat = new Matrix4x4();
            mat.m[0, 0] = (float)Math.Cos(angle); mat.m[0, 2] = (float)Math.Sin(angle);
            mat.m[1, 1] = 1; mat.m[2, 0] = -(float)Math.Sin(angle); mat.m[2, 2] = (float)Math.Cos(angle); mat.m[3, 3] = 1;
            return mat;
        }

        public static Matrix4x4 RotationZ(float angle)
        {
            Matrix4x4 mat = new Matrix4x4();
            mat.m[0, 0] = (float)Math.Cos(angle); mat.m[0, 1] = (float)Math.Sin(angle);
            mat.m[1, 0] = -(float)Math.Sin(angle); mat.m[1, 1] = (float)Math.Cos(angle); mat.m[2, 2] = 1; mat.m[3, 3] = 1;
            return mat;
        }

        public static Vector3 MultiplyVector(Vector3 i, Matrix4x4 m)
        {
            Vector3 v = new Vector3();
            v.X = i.X * m.m[0, 0] + i.Y * m.m[1, 0] + i.Z * m.m[2, 0] + m.m[3, 0];
            v.Y = i.X * m.m[0, 1] + i.Y * m.m[1, 1] + i.Z * m.m[2, 1] + m.m[3, 1];
            v.Z = i.X * m.m[0, 2] + i.Y * m.m[1, 2] + i.Z * m.m[2, 2] + m.m[3, 2];
            float w = i.X * m.m[0, 3] + i.Y * m.m[1, 3] + i.Z * m.m[2, 3] + m.m[3, 3];
            if (w != 0.0f) { v.X /= w; v.Y /= w; v.Z /= w; }
            return v;
        }
    }

    public class Triangle
    {
        public Vector3[] p = new Vector3[3];
        public Color Color = Color.White;
    }

    public class Mesh3D
    {
        public string ID { get; set; }
        public List<Triangle> Tris { get; set; } = new List<Triangle>();
        public float RotX, RotY, RotZ, PosX, PosY, PosZ;
        public float ScaleX = 1.0f, ScaleY = 1.0f, ScaleZ = 1.0f;
        public bool Visible { get; set; } = true;

        public static Mesh3D CreateCube(string id, float size, Color c)
        {
            Mesh3D mesh = new Mesh3D { ID = id }; float s = size / 2;
            Vector3[] v = { new Vector3(-s,-s,-s), new Vector3(-s,s,-s), new Vector3(s,s,-s), new Vector3(s,-s,-s),
                            new Vector3(-s,-s,s), new Vector3(-s,s,s), new Vector3(s,s,s), new Vector3(s,-s,s) };
            int[][] indices = { new[]{0,1,2}, new[]{0,2,3}, new[]{4,0,3}, new[]{4,3,7}, new[]{5,4,7}, new[]{5,7,6},
                                new[]{1,5,6}, new[]{1,6,2}, new[]{4,5,1}, new[]{4,1,0}, new[]{3,2,6}, new[]{3,6,7} };
            foreach (var idx in indices) mesh.Tris.Add(new Triangle { p = new[] { v[idx[0]], v[idx[1]], v[idx[2]] }, Color = c });
            return mesh;
        }

        public static Mesh3D CreatePyramid(string id, float size, Color c)
        {
            Mesh3D mesh = new Mesh3D { ID = id }; float s = size / 2;
            Vector3[] v = { new Vector3(0, s, 0), new Vector3(-s, -s, -s), new Vector3(s, -s, -s), new Vector3(s, -s, s), new Vector3(-s, -s, s) };
            int[][] indices = { new[] { 1, 2, 4 }, new[] { 2, 3, 4 }, new[] { 0, 2, 1 }, new[] { 0, 3, 2 }, new[] { 0, 4, 3 }, new[] { 0, 1, 4 } };
            foreach (var idx in indices) mesh.Tris.Add(new Triangle { p = new[] { v[idx[0]], v[idx[1]], v[idx[2]] }, Color = c });
            return mesh;
        }

        public static Mesh3D CreatePlane(string id, float size, Color c)
        {
            Mesh3D mesh = new Mesh3D { ID = id }; float s = size / 2;
            Vector3[] v = { new Vector3(-s, 0, -s), new Vector3(s, 0, -s), new Vector3(-s, 0, s), new Vector3(s, 0, s) };
            foreach (var idx in new[] { new[] { 0, 1, 2 }, new[] { 1, 3, 2 } }) mesh.Tris.Add(new Triangle { p = new[] { v[idx[0]], v[idx[1]], v[idx[2]] }, Color = c });
            return mesh;
        }

        public static Mesh3D CreateSphere(string id, float radius, int rings, int sectors, Color c)
        {
            Mesh3D mesh = new Mesh3D { ID = id };
            float const_R = 1.0f / (float)(rings - 1);
            float const_S = 1.0f / (float)(sectors - 1);
            float M_PI = (float)Math.PI;
            float M_PI_2 = (float)(Math.PI / 2.0);

            List<Vector3> verts = new List<Vector3>();
            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < sectors; s++)
                {
                    float y = (float)Math.Sin(-M_PI_2 + M_PI * r * const_R);
                    float x = (float)Math.Cos(2 * M_PI * s * const_S) * (float)Math.Sin(M_PI * r * const_R);
                    float z = (float)Math.Sin(2 * M_PI * s * const_S) * (float)Math.Sin(M_PI * r * const_R);
                    verts.Add(new Vector3(x * radius, y * radius, z * radius));
                }
            }

            for (int r = 0; r < rings - 1; r++)
            {
                for (int s = 0; s < sectors - 1; s++)
                {
                    int i0 = r * sectors + s;
                    int i1 = r * sectors + (s + 1);
                    int i2 = (r + 1) * sectors + (s + 1);
                    int i3 = (r + 1) * sectors + s;
                    mesh.Tris.Add(new Triangle { p = new[] { verts[i0], verts[i1], verts[i2] }, Color = c });
                    mesh.Tris.Add(new Triangle { p = new[] { verts[i0], verts[i2], verts[i3] }, Color = c });
                }
            }
            return mesh;
        }

        public static Mesh3D LoadOBJ(string id, string filepath, Color color)
        {
            Mesh3D mesh = new Mesh3D { ID = id };
            List<Vector3> verts = new List<Vector3>();

            if (!File.Exists(filepath)) return mesh; // Return empty mesh if not found

            string[] lines = File.ReadAllLines(filepath);
            foreach (string line in lines)
            {
                string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) continue;

                if (parts[0] == "v" && parts.Length >= 4)
                {
                    verts.Add(new Vector3(
                        float.Parse(parts[1], CultureInfo.InvariantCulture),
                        float.Parse(parts[2], CultureInfo.InvariantCulture),
                        float.Parse(parts[3], CultureInfo.InvariantCulture)));
                }
                else if (parts[0] == "f" && parts.Length >= 4)
                {
                    try
                    {
                        int i1 = int.Parse(parts[1].Split('/')[0]) - 1;
                        int i2 = int.Parse(parts[2].Split('/')[0]) - 1;
                        int i3 = int.Parse(parts[3].Split('/')[0]) - 1;
                        mesh.Tris.Add(new Triangle { p = new[] { verts[i1], verts[i2], verts[i3] }, Color = color });

                        // Quad faces triangulation
                        if (parts.Length == 5)
                        {
                            int i4 = int.Parse(parts[4].Split('/')[0]) - 1;
                            mesh.Tris.Add(new Triangle { p = new[] { verts[i1], verts[i3], verts[i4] }, Color = color });
                        }
                    }
                    catch { } // Ignore complex polygons for now
                }
            }
            return mesh;
        }
    }

    public class WinterRenderer3D
    {
        private Matrix4x4 matProj;
        public List<Mesh3D> Meshes = new List<Mesh3D>();
        public Vector3 CameraPos = new Vector3(0, 0, 0);
        public Vector3 LightDir = new Vector3(0, -1, -1);
        public Color AmbientColor = Color.FromArgb(40, 40, 40);

        public void Init(int width, int height)
        {
            Meshes.Clear();
            matProj = Matrix4x4.Projection(90.0f, (float)height / width, 0.1f, 1000.0f);

            // Normalize LightDir
            float l = (float)Math.Sqrt(LightDir.X * LightDir.X + LightDir.Y * LightDir.Y + LightDir.Z * LightDir.Z);
            LightDir.X /= l; LightDir.Y /= l; LightDir.Z /= l;
        }

        public void Render(Graphics g, int width, int height)
        {
            List<Triangle> trianglesToRaster = new List<Triangle>();

            foreach (var mesh in Meshes)
            {
                if (!mesh.Visible) continue;

                Matrix4x4 matRotZ = Matrix4x4.RotationZ(mesh.RotZ);
                Matrix4x4 matRotX = Matrix4x4.RotationX(mesh.RotX);
                Matrix4x4 matRotY = Matrix4x4.RotationY(mesh.RotY);

                foreach (var tri in mesh.Tris)
                {
                    Triangle triTrans = new Triangle();
                    for (int i = 0; i < 3; i++)
                    {
                        // Scale
                        Vector3 scaled = new Vector3(tri.p[i].X * mesh.ScaleX, tri.p[i].Y * mesh.ScaleY, tri.p[i].Z * mesh.ScaleZ);

                        // Rotate
                        triTrans.p[i] = Matrix4x4.MultiplyVector(Matrix4x4.MultiplyVector(Matrix4x4.MultiplyVector(scaled, matRotZ), matRotX), matRotY);

                        // Translate
                        triTrans.p[i].X += mesh.PosX;
                        triTrans.p[i].Y += mesh.PosY;
                        triTrans.p[i].Z += mesh.PosZ;
                    }

                    // Compute Normal
                    Vector3 l1 = new Vector3(triTrans.p[1].X - triTrans.p[0].X, triTrans.p[1].Y - triTrans.p[0].Y, triTrans.p[1].Z - triTrans.p[0].Z);
                    Vector3 l2 = new Vector3(triTrans.p[2].X - triTrans.p[0].X, triTrans.p[2].Y - triTrans.p[0].Y, triTrans.p[2].Z - triTrans.p[0].Z);
                    Vector3 normal = new Vector3(l1.Y * l2.Z - l1.Z * l2.Y, l1.Z * l2.X - l1.X * l2.Z, l1.X * l2.Y - l1.Y * l2.X);

                    float normalLen = (float)Math.Sqrt(normal.X * normal.X + normal.Y * normal.Y + normal.Z * normal.Z);
                    if (normalLen == 0) continue;
                    normal.X /= normalLen; normal.Y /= normalLen; normal.Z /= normalLen;

                    // Backface culling
                    Vector3 camRay = new Vector3(triTrans.p[0].X - CameraPos.X, triTrans.p[0].Y - CameraPos.Y, triTrans.p[0].Z - CameraPos.Z);
                    if (normal.X * camRay.X + normal.Y * camRay.Y + normal.Z * camRay.Z < 0)
                    {
                        // Lighting calculation
                        float dp = Math.Max(0.1f, normal.X * LightDir.X + normal.Y * LightDir.Y + normal.Z * LightDir.Z);

                        int r = Math.Min(255, (int)(tri.Color.R * dp) + AmbientColor.R);
                        int green = Math.Min(255, (int)(tri.Color.G * dp) + AmbientColor.G);
                        int b = Math.Min(255, (int)(tri.Color.B * dp) + AmbientColor.B);
                        triTrans.Color = Color.FromArgb(255, r, green, b);

                        // Projection
                        Triangle triProj = new Triangle { Color = triTrans.Color };
                        for (int i = 0; i < 3; i++)
                        {
                            Vector3 viewPt = new Vector3(triTrans.p[i].X - CameraPos.X, triTrans.p[i].Y - CameraPos.Y, triTrans.p[i].Z - CameraPos.Z);
                            triProj.p[i] = Matrix4x4.MultiplyVector(viewPt, matProj);

                            // Scale to screen
                            triProj.p[i].X = (triProj.p[i].X + 1.0f) * 0.5f * width;
                            triProj.p[i].Y = (triProj.p[i].Y + 1.0f) * 0.5f * height;
                        }

                        trianglesToRaster.Add(triProj);
                    }
                }
            }

            // Painter's algorithm (Sort by Z depth)
            trianglesToRaster.Sort((t1, t2) => {
                float z1 = (t1.p[0].Z + t1.p[1].Z + t1.p[2].Z) / 3.0f;
                float z2 = (t2.p[0].Z + t2.p[1].Z + t2.p[2].Z) / 3.0f;
                return z2.CompareTo(z1);
            });

            // Rasterization
            using (Pen wirePen = new Pen(Color.FromArgb(20, 0, 0, 0), 1)) // Very faint wireframe for style
            {
                foreach (var tri in trianglesToRaster)
                {
                    PointF[] points = {
                        new PointF(tri.p[0].X, tri.p[0].Y),
                        new PointF(tri.p[1].X, tri.p[1].Y),
                        new PointF(tri.p[2].X, tri.p[2].Y)
                    };

                    using (SolidBrush brush = new SolidBrush(tri.Color))
                    {
                        g.FillPolygon(brush, points);
                    }
                    g.DrawPolygon(wirePen, points);
                }
            }
        }
    }

    public class WinterEngineWindow : Form
    {
        public PictureBox viewport;
        public Dictionary<Keys, bool> KeyStates = new Dictionary<Keys, bool>();
        public int MouseX = 0;
        public int MouseY = 0;
        public bool MousePressed = false;

        public WinterEngineWindow()
        {
            this.Text = "WinterCode Engine Window";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.Black;
            this.KeyPreview = true;
            this.Icon = SystemIcons.Application;
            this.DoubleBuffered = true; // Улучшение производительности формы

            viewport = new PictureBox();
            viewport.Dock = DockStyle.Fill;
            viewport.BackColor = Color.Black;
            this.Controls.Add(viewport);

            // Обработка мыши
            viewport.MouseMove += (s, e) => { MouseX = e.X; MouseY = e.Y; };
            viewport.MouseDown += (s, e) => { MousePressed = true; };
            viewport.MouseUp += (s, e) => { MousePressed = false; };

            // Для перехвата фокуса
            viewport.Click += (s, e) => this.Focus();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            KeyStates[e.KeyCode] = true;
            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            KeyStates[e.KeyCode] = false;
            base.OnKeyUp(e);
        }
    }

    public class WinterIDE : Form
    {
        private RichTextBox editor;
        private RichTextBox consoleOut;
        private Panel lineNumbersPanel;
        private SplitContainer splitMain;

        private WinterInterpreter interpreter;
        private Thread scriptThread;
        private WinterEngineWindow engineWindow;

        public WinterIDE()
        {
            InitializeComponent();
            interpreter = new WinterInterpreter(this, consoleOut);

            // Пример скрипта-демонстрации возможностей v2.0
            editor.Text =
@"# WinterCode (3D4s) Demo - Black & White aesthetic
message Initializing Winter Engine...
window title Space Exploration Demo
window size 800 600

3d init
3d cube player_cube 0 -1 5 1.0
3d pyramid enemy -3 0 10 1.5
3d sphere moon 5 5 15 2.0 16 16

set px = 0
set py = -1
set pz = 5
set rot = 0

array create bullets
sub shoot
    message Pew!
endsub

message Use W/A/S/D to move, Mouse Click to interact!
message Press Shift + R to Restart, ESC to Stop.

set isRunning = 1
while isRunning == 1
    draw clear 10 10 15
    
    input key W wDown
    input key S sDown
    input key A aDown
    input key D dDown
    
    if wDown == 1
        add pz 0.2
    endif
    if sDown == 1
        sub pz 0.2
    endif
    if aDown == 1
        sub px 0.2
    endif
    if dDown == 1
        add px 0.2
    endif

    3d camera px py pz
    
    add rot 0.03
    3d rotate player_cube rot rot 0
    3d rotate enemy 0 rot 0
    3d rotate moon rot 0 rot
    
    3d render

    input mouse mX mY mDown
    if mDown == 1
        draw circle mX mY 20 200 50 50
        call shoot
    endif
    
    draw text 10 10 ENG_V2.0_RUNNING 255 255 255
    draw text 10 30 POS_X: 200 200 200
    draw string 80 30 px 255 255 255
    
    draw render
    sleep 16
endwhile

message Program Ended.
end";
            HighlightSyntax();
            DrawLineNumbers();
        }

        private void InitializeComponent()
        {
            this.Text = "WinterCode v2.0 IDE";
            this.Size = new Size(900, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.Black;
            this.ForeColor = Color.White;
            this.KeyPreview = true;

            // Настройка меню (Стиль: Мелкий, строгий)
            MenuStrip menu = new MenuStrip();
            menu.BackColor = Color.Black;
            menu.ForeColor = Color.White;
            menu.Font = new Font("Consolas", 9);

            ToolStripMenuItem fileMenu = new ToolStripMenuItem("File");
            ToolStripMenuItem openMenu = new ToolStripMenuItem("Open...");
            ToolStripMenuItem saveMenu = new ToolStripMenuItem("Save...");
            fileMenu.DropDownItems.Add(openMenu);
            fileMenu.DropDownItems.Add(saveMenu);

            ToolStripMenuItem runMenu = new ToolStripMenuItem("Run Script (Shift+R)");
            ToolStripMenuItem stopMenu = new ToolStripMenuItem("Stop (ESC)");

            menu.Items.Add(fileMenu);
            menu.Items.Add(runMenu);
            menu.Items.Add(stopMenu);
            this.Controls.Add(menu);
            this.MainMenuStrip = menu;

            openMenu.Click += (s, e) => {
                using (OpenFileDialog ofd = new OpenFileDialog() { Filter = "WinterCode Files (*.wc)|*.wc|All Files (*.*)|*.*" })
                {
                    if (ofd.ShowDialog() == DialogResult.OK) { editor.Text = File.ReadAllText(ofd.FileName); HighlightSyntax(); }
                }
            };

            saveMenu.Click += (s, e) => {
                using (SaveFileDialog sfd = new SaveFileDialog() { Filter = "WinterCode Files (*.wc)|*.wc" })
                {
                    if (sfd.ShowDialog() == DialogResult.OK) File.WriteAllText(sfd.FileName, editor.Text);
                }
            };

            runMenu.Click += RunMenu_Click;
            stopMenu.Click += StopMenu_Click;

            // Вертикальный сплиттер
            splitMain = new SplitContainer();
            splitMain.Dock = DockStyle.Fill;
            splitMain.Orientation = Orientation.Horizontal;
            splitMain.SplitterDistance = 450;
            splitMain.BackColor = Color.FromArgb(40, 40, 40);

            // Настройка панели номеров строк
            lineNumbersPanel = new Panel();
            lineNumbersPanel.Dock = DockStyle.Left;
            lineNumbersPanel.Width = 35;
            lineNumbersPanel.BackColor = Color.FromArgb(20, 20, 20);
            lineNumbersPanel.Paint += LineNumbersPanel_Paint;

            // Настройка редактора кода
            editor = new RichTextBox();
            editor.Dock = DockStyle.Fill;
            editor.BackColor = Color.Black;
            editor.ForeColor = Color.White;
            editor.Font = new Font("Consolas", 10);
            editor.AcceptsTab = true;
            editor.BorderStyle = BorderStyle.None;
            editor.WordWrap = false;
            editor.TextChanged += (s, e) => { HighlightSyntax(); lineNumbersPanel.Invalidate(); };
            editor.VScroll += (s, e) => lineNumbersPanel.Invalidate();
            editor.FontChanged += (s, e) => lineNumbersPanel.Invalidate();

            // Настройка консоли
            consoleOut = new RichTextBox();
            consoleOut.Dock = DockStyle.Fill;
            consoleOut.BackColor = Color.Black;
            consoleOut.ForeColor = Color.LightGray;
            consoleOut.Font = new Font("Consolas", 9);
            consoleOut.ReadOnly = true;
            consoleOut.BorderStyle = BorderStyle.None;

            Panel editorContainer = new Panel { Dock = DockStyle.Fill };
            editorContainer.Controls.Add(editor);
            editorContainer.Controls.Add(lineNumbersPanel);

            splitMain.Panel1.Controls.Add(editorContainer);
            splitMain.Panel2.Controls.Add(consoleOut);
            this.Controls.Add(splitMain);
        }

        private void LineNumbersPanel_Paint(object sender, PaintEventArgs e)
        {
            DrawLineNumbers(e.Graphics);
        }

        private void DrawLineNumbers(Graphics g = null)
        {
            if (g == null) { lineNumbersPanel.Invalidate(); return; }
            g.Clear(lineNumbersPanel.BackColor);

            int firstIndex = editor.GetCharIndexFromPosition(new Point(0, 0));
            int firstLine = editor.GetLineFromCharIndex(firstIndex);
            Point firstPos = editor.GetPositionFromCharIndex(firstIndex);

            int lastIndex = editor.GetCharIndexFromPosition(new Point(0, editor.ClientSize.Height));
            int lastLine = editor.GetLineFromCharIndex(lastIndex);

            using (Brush b = new SolidBrush(Color.Gray))
            using (StringFormat format = new StringFormat() { Alignment = StringAlignment.Far })
            {
                for (int i = firstLine; i <= lastLine + 1; i++)
                {
                    if (i >= editor.Lines.Length) break;
                    int y = editor.GetPositionFromCharIndex(editor.GetFirstCharIndexFromLine(i)).Y;
                    g.DrawString((i + 1).ToString(), editor.Font, b, lineNumbersPanel.Width - 2, y, format);
                }
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Shift | Keys.R))
            {
                RunMenu_Click(null, null);
                return true;
            }
            if (keyData == Keys.Escape)
            {
                StopMenu_Click(null, null);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void HighlightSyntax()
        {
            IntPtr eventMask = NativeMethods.SendMessage(editor.Handle, NativeMethods.EM_GETEVENTMASK, IntPtr.Zero, IntPtr.Zero);
            NativeMethods.SendMessage(editor.Handle, NativeMethods.WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);

            int selStart = editor.SelectionStart;
            int selLength = editor.SelectionLength;

            editor.SelectAll();
            editor.SelectionColor = Color.White;

            string text = editor.Text;

            // Базовые ключевые слова
            string[] keywords = { "message", "window", "set", "add", "sub", "mul", "div", "if", "else", "endif", "while", "endwhile", "repeat", "endrepeat", "sleep", "input", "file", "draw", "3d", "audio", "end", "call", "array" };
            string keywordRegex = @"\b(" + string.Join("|", keywords) + @")\b";
            foreach (Match m in Regex.Matches(text, keywordRegex))
            {
                editor.Select(m.Index, m.Length); editor.SelectionColor = Color.FromArgb(86, 156, 214);
            }

            // Математика и числа
            foreach (Match m in Regex.Matches(text, @"\b(math|sin|cos|tan|sqrt|rnd)\b"))
            {
                editor.Select(m.Index, m.Length); editor.SelectionColor = Color.FromArgb(197, 134, 192);
            }

            foreach (Match m in Regex.Matches(text, @"\b-?\d+(\.\d+)?\b"))
            {
                editor.Select(m.Index, m.Length); editor.SelectionColor = Color.FromArgb(181, 206, 168);
            }

            // Комментарии
            foreach (Match m in Regex.Matches(text, @"#.*"))
            {
                editor.Select(m.Index, m.Length); editor.SelectionColor = Color.FromArgb(87, 166, 74);
            }

            editor.Select(selStart, selLength);
            NativeMethods.SendMessage(editor.Handle, NativeMethods.WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
            NativeMethods.SendMessage(editor.Handle, NativeMethods.EM_SETEVENTMASK, IntPtr.Zero, eventMask);
            editor.Invalidate();
        }

        private void RunMenu_Click(object sender, EventArgs e)
        {
            string code = editor.Text.Trim();

            if (!code.EndsWith("end"))
            {
                interpreter.Log("Error: Скрипт ОБЯЗАТЕЛЬНО должен заканчиваться командой 'end'.", Color.Red);
                return;
            }

            if (scriptThread != null && scriptThread.IsAlive)
            {
                interpreter.Stop();
                scriptThread.Join(500);
                if (engineWindow != null && !engineWindow.IsDisposed)
                    engineWindow.Invoke(new Action(() => engineWindow.Close()));
            }

            consoleOut.Clear();
            interpreter.Log(">>> Compilation Started...", Color.Cyan);

            engineWindow = new WinterEngineWindow();
            engineWindow.FormClosing += (s, ev) => { StopMenu_Click(null, null); };
            engineWindow.Show();

            interpreter.SetOutputWindow(engineWindow);

            scriptThread = new Thread(() => interpreter.Run(code));
            scriptThread.IsBackground = true;
            scriptThread.Start();
        }

        private void StopMenu_Click(object sender, EventArgs e)
        {
            if (scriptThread != null && scriptThread.IsAlive)
            {
                interpreter.Stop();
                interpreter.Log(">>> Engine forcefully stopped by user.", Color.Orange);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            interpreter.Stop();
            base.OnFormClosing(e);
        }
    }

    public class WinterInterpreter
    {
        private WinterIDE mainForm;
        private RichTextBox console;
        public WinterEngineWindow engineWindow;
        private Bitmap renderBuffer;
        private Graphics renderGraphics;

        // Хранилища данных
        private Dictionary<string, double> vars = new Dictionary<string, double>();
        private Dictionary<string, string> stringVars = new Dictionary<string, string>();
        private Dictionary<string, List<double>> arrayVars = new Dictionary<string, List<double>>();
        private Dictionary<string, int> subroutines = new Dictionary<string, int>(); // Для "sub name"

        private WinterAudio audioEngine = new WinterAudio();
        private WinterRenderer3D renderer3D = new WinterRenderer3D();

        private bool isRunning = false;
        private Random rnd = new Random();

        public WinterInterpreter(WinterIDE form, RichTextBox cons)
        {
            mainForm = form;
            console = cons;
        }

        public void SetOutputWindow(WinterEngineWindow win)
        {
            engineWindow = win;
            InitGraphics(win.viewport.Width, win.viewport.Height);
            win.viewport.Resize += (s, e) => {
                if (win.viewport.Width > 0 && win.viewport.Height > 0)
                    InitGraphics(win.viewport.Width, win.viewport.Height);
            };
        }

        private void InitGraphics(int w, int h)
        {
            if (w <= 0 || h <= 0) return;
            if (renderBuffer != null) renderBuffer.Dispose();
            if (renderGraphics != null) renderGraphics.Dispose();

            renderBuffer = new Bitmap(w, h);
            renderGraphics = Graphics.FromImage(renderBuffer);
            renderGraphics.CompositingQuality = CompositingQuality.HighSpeed;
            renderGraphics.InterpolationMode = InterpolationMode.Low;
            renderGraphics.Clear(Color.Black);
            renderer3D.Init(w, h);

            UpdateViewport();
        }

        public void Log(string msg, Color? color = null)
        {
            if (console.InvokeRequired)
            {
                console.Invoke(new Action(() => Log(msg, color)));
                return;
            }
            console.SelectionStart = console.TextLength;
            console.SelectionLength = 0;
            console.SelectionColor = color ?? Color.LightGray;
            console.AppendText(msg + Environment.NewLine);
            console.ScrollToCaret();
        }

        public void Stop()
        {
            isRunning = false;
            audioEngine.StopAll();
        }

        public void Run(string code)
        {
            isRunning = true;
            vars.Clear();
            stringVars.Clear();
            arrayVars.Clear();
            subroutines.Clear();
            renderer3D.Meshes.Clear();

            string[] lines = code.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int pc = 0;

            Stack<Tuple<int, int>> loopStack = new Stack<Tuple<int, int>>();
            Stack<int> whileStack = new Stack<int>();
            Stack<int> callStack = new Stack<int>();

            // Pre-pass: сбор подпрограмм (sub / endsub)
            for (int i = 0; i < lines.Length; i++)
            {
                string tLine = lines[i].Trim();
                if (tLine.StartsWith("sub "))
                {
                    string subName = tLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[1];
                    subroutines[subName] = i;
                }
            }

            Log(">>> Executing...", Color.Lime);

            while (pc < lines.Length && isRunning)
            {
                string line = lines[pc].Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith("//")) { pc++; continue; }

                string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string cmd = parts[0].ToLower();

                try
                {
                    switch (cmd)
                    {
                        case "end":
                            isRunning = false;
                            break;

                        case "message":
                        case "echo":
                            Log(string.Join(" ", parts, 1, parts.Length - 1));
                            break;

                        case "window":
                            if (engineWindow != null)
                            {
                                if (parts[1] == "title") engineWindow.Invoke(new Action(() => engineWindow.Text = string.Join(" ", parts, 2, parts.Length - 2)));
                                else if (parts[1] == "size") engineWindow.Invoke(new Action(() => engineWindow.Size = new Size((int)GetVal(parts[2]), (int)GetVal(parts[3]))));
                            }
                            break;

                        case "set":
                            if (parts.Length > 2 && parts[2] == "=") vars[parts[1]] = GetVal(parts[3]);
                            else vars[parts[1]] = GetVal(parts[2]);
                            break;

                        case "add": vars[parts[1]] += GetVal(parts[2]); break;
                        case "sub":
                            if (parts.Length == 2) { } // Это объявление 'sub Имя', пропускаем при выполнении
                            else vars[parts[1]] -= GetVal(parts[2]);
                            break;
                        case "mul": vars[parts[1]] *= GetVal(parts[2]); break;
                        case "div": vars[parts[1]] /= GetVal(parts[2]); break;

                        case "if":
                            double left = GetVal(parts[1]), right = GetVal(parts[3]);
                            string op = parts[2]; bool cond = false;
                            if (op == "==") cond = left == right;
                            else if (op == ">") cond = left > right;
                            else if (op == "<") cond = left < right;
                            else if (op == ">=") cond = left >= right;
                            else if (op == "<=") cond = left <= right; else if (op == "!=") cond = left != right;

                            if (!cond)
                            {
                                // Ищем endif или else
                                int nestCount = 0;
                                while (pc < lines.Length)
                                {
                                    pc++;
                                    if (pc >= lines.Length) break;
                                    string fw = lines[pc].Trim().Split(' ')[0].ToLower();
                                    if (fw == "if") nestCount++;
                                    else if (fw == "endif" && nestCount == 0) break;
                                    else if (fw == "else" && nestCount == 0) break;
                                    else if (fw == "endif" && nestCount > 0) nestCount--;
                                }
                            }
                            break;
                        case "else":
                            // Если дошли сюда, значит if был true, нужно пропустить до endif
                            int nestCountElse = 0;
                            while (pc < lines.Length)
                            {
                                pc++;
                                if (pc >= lines.Length) break;
                                string fw = lines[pc].Trim().Split(' ')[0].ToLower();
                                if (fw == "if") nestCountElse++;
                                else if (fw == "endif" && nestCountElse == 0) break;
                                else if (fw == "endif" && nestCountElse > 0) nestCountElse--;
                            }
                            break;
                        case "endif": break; // Просто маркер

                        case "while":
                            // Проверяем условие
                            double wL = GetVal(parts[1]), wR = GetVal(parts[3]);
                            string wOp = parts[2]; bool wCond = false;
                            if (wOp == "==") wCond = wL == wR;
                            else if (wOp == ">") wCond = wL > wR;
                            else if (wOp == "<") wCond = wL < wR;
                            else if (wOp == ">=") wCond = wL >= wR;
                            else if (wOp == "<=") wCond = wL <= wR; else if (wOp == "!=") wCond = wL != wR;

                            if (wCond)
                            {
                                if (!whileStack.Contains(pc)) whileStack.Push(pc);
                            }
                            else
                            {
                                // Прыгаем к endwhile
                                int wNestCount = 0;
                                while (pc < lines.Length)
                                {
                                    pc++;
                                    if (pc >= lines.Length) break;
                                    string fw = lines[pc].Trim().Split(' ')[0].ToLower();
                                    if (fw == "while") wNestCount++;
                                    else if (fw == "endwhile" && wNestCount == 0) break;
                                    else if (fw == "endwhile" && wNestCount > 0) wNestCount--;
                                }
                                if (whileStack.Count > 0 && whileStack.Peek() == pc) whileStack.Pop();
                            }
                            break;

                        case "endwhile":
                            if (whileStack.Count > 0) { pc = whileStack.Peek() - 1; }
                            break;

                        case "call":
                            if (subroutines.ContainsKey(parts[1]))
                            {
                                callStack.Push(pc);
                                pc = subroutines[parts[1]];
                            }
                            else Log($"Runtime Error: Subroutine {parts[1]} not found", Color.Red);
                            break;

                        case "endsub":
                            if (callStack.Count > 0) pc = callStack.Pop();
                            break;

                        case "sleep":
                            Thread.Sleep((int)GetVal(parts[1]));
                            break;

                        case "array":
                            if (parts[1] == "create") arrayVars[parts[2]] = new List<double>();
                            else if (parts[1] == "push") arrayVars[parts[2]].Add(GetVal(parts[3]));
                            else if (parts[1] == "get") vars[parts[2]] = arrayVars[parts[3]][(int)GetVal(parts[4])];
                            else if (parts[1] == "set") arrayVars[parts[3]][(int)GetVal(parts[4])] = GetVal(parts[2]);
                            else if (parts[1] == "length") vars[parts[2]] = arrayVars[parts[3]].Count;
                            break;

                        case "math":
                            if (parts[1] == "sin") vars[parts[2]] = Math.Sin(GetVal(parts[3]));
                            else if (parts[1] == "cos") vars[parts[2]] = Math.Cos(GetVal(parts[3]));
                            else if (parts[1] == "tan") vars[parts[2]] = Math.Tan(GetVal(parts[3]));
                            else if (parts[1] == "sqrt") vars[parts[2]] = Math.Sqrt(GetVal(parts[3]));
                            else if (parts[1] == "rnd") vars[parts[2]] = rnd.Next((int)GetVal(parts[3]), (int)GetVal(parts[4]));
                            else if (parts[1] == "abs") vars[parts[2]] = Math.Abs(GetVal(parts[3]));
                            break;

                        case "file":
                            if (parts[1] == "write") File.WriteAllText(parts[2], vars.ContainsKey(parts[3]) ? vars[parts[3]].ToString() : stringVars.ContainsKey(parts[3]) ? stringVars[parts[3]] : parts[3]);
                            else if (parts[1] == "append") File.AppendAllText(parts[2], vars.ContainsKey(parts[3]) ? vars[parts[3]].ToString() : stringVars.ContainsKey(parts[3]) ? stringVars[parts[3]] : parts[3]);
                            else if (parts[1] == "read") stringVars[parts[2]] = File.ReadAllText(parts[3]);
                            break;

                        case "input":
                            if (engineWindow != null)
                            {
                                if (parts[1] == "key" && Enum.TryParse(parts[2].ToUpper(), out Keys k))
                                    vars[parts[3]] = engineWindow.KeyStates.ContainsKey(k) && engineWindow.KeyStates[k] ? 1 : 0;
                                else if (parts[1] == "mouse")
                                {
                                    vars[parts[2]] = engineWindow.MouseX;
                                    vars[parts[3]] = engineWindow.MouseY;
                                    if (parts.Length > 4) vars[parts[4]] = engineWindow.MousePressed ? 1 : 0;
                                }
                            }
                            break;

                        case "3d":
                            if (parts[1] == "init") renderer3D.Init(renderBuffer.Width, renderBuffer.Height);
                            else if (parts[1] == "camera") { renderer3D.CameraPos.X = (float)GetVal(parts[2]); renderer3D.CameraPos.Y = (float)GetVal(parts[3]); renderer3D.CameraPos.Z = (float)GetVal(parts[4]); }
                            else if (parts[1] == "cube") { var c = Mesh3D.CreateCube(parts[2], (float)GetVal(parts[6]), GetRandomColor()); c.PosX = (float)GetVal(parts[3]); c.PosY = (float)GetVal(parts[4]); c.PosZ = (float)GetVal(parts[5]); renderer3D.Meshes.Add(c); }
                            else if (parts[1] == "pyramid") { var p = Mesh3D.CreatePyramid(parts[2], (float)GetVal(parts[6]), GetRandomColor()); p.PosX = (float)GetVal(parts[3]); p.PosY = (float)GetVal(parts[4]); p.PosZ = (float)GetVal(parts[5]); renderer3D.Meshes.Add(p); }
                            else if (parts[1] == "plane") { var p = Mesh3D.CreatePlane(parts[2], (float)GetVal(parts[6]), Color.DarkGray); p.PosX = (float)GetVal(parts[3]); p.PosY = (float)GetVal(parts[4]); p.PosZ = (float)GetVal(parts[5]); renderer3D.Meshes.Add(p); }
                            else if (parts[1] == "sphere") { var s = Mesh3D.CreateSphere(parts[2], (float)GetVal(parts[6]), (int)(parts.Length > 7 ? GetVal(parts[7]) : 10), (int)(parts.Length > 8 ? GetVal(parts[8]) : 10), GetRandomColor()); s.PosX = (float)GetVal(parts[3]); s.PosY = (float)GetVal(parts[4]); s.PosZ = (float)GetVal(parts[5]); renderer3D.Meshes.Add(s); }
                            else if (parts[1] == "loadobj") { var o = Mesh3D.LoadOBJ(parts[2], parts[6], GetRandomColor()); o.PosX = (float)GetVal(parts[3]); o.PosY = (float)GetVal(parts[4]); o.PosZ = (float)GetVal(parts[5]); renderer3D.Meshes.Add(o); }
                            else if (parts[1] == "rotate") { var m = renderer3D.Meshes.Find(x => x.ID == parts[2]); if (m != null) { m.RotX = (float)GetVal(parts[3]); m.RotY = (float)GetVal(parts[4]); m.RotZ = (float)GetVal(parts[5]); } }
                            else if (parts[1] == "scale") { var m = renderer3D.Meshes.Find(x => x.ID == parts[2]); if (m != null) { m.ScaleX = (float)GetVal(parts[3]); m.ScaleY = (float)GetVal(parts[4]); m.ScaleZ = (float)GetVal(parts[5]); } }
                            else if (parts[1] == "render") renderer3D.Render(renderGraphics, renderBuffer.Width, renderBuffer.Height);
                            break;

                        case "draw":
                            if (parts[1] == "clear") renderGraphics.Clear(Color.FromArgb((int)GetVal(parts[2]), (int)GetVal(parts[3]), (int)GetVal(parts[4])));
                            else if (parts[1] == "rect") { using (var br = new SolidBrush(Color.FromArgb((int)GetVal(parts[6]), (int)GetVal(parts[7]), (int)GetVal(parts[8])))) renderGraphics.FillRectangle(br, (int)GetVal(parts[2]), (int)GetVal(parts[3]), (int)GetVal(parts[4]), (int)GetVal(parts[5])); }
                            else if (parts[1] == "circle") { using (var br = new SolidBrush(Color.FromArgb((int)GetVal(parts[5]), (int)GetVal(parts[6]), (int)GetVal(parts[7])))) renderGraphics.FillEllipse(br, (int)GetVal(parts[2]) - (int)GetVal(parts[4]), (int)GetVal(parts[3]) - (int)GetVal(parts[4]), (int)GetVal(parts[4]) * 2, (int)GetVal(parts[4]) * 2); }
                            else if (parts[1] == "line") { using (var pen = new Pen(Color.FromArgb((int)GetVal(parts[6]), (int)GetVal(parts[7]), (int)GetVal(parts[8])), 2)) renderGraphics.DrawLine(pen, (int)GetVal(parts[2]), (int)GetVal(parts[3]), (int)GetVal(parts[4]), (int)GetVal(parts[5])); }
                            else if (parts[1] == "text") { using (var br = new SolidBrush(Color.FromArgb((int)GetVal(parts[5]), (int)GetVal(parts[6]), (int)GetVal(parts[7])))) renderGraphics.DrawString(parts[4].Replace("_", " "), new Font("Consolas", 12), br, (int)GetVal(parts[2]), (int)GetVal(parts[3])); }
                            else if (parts[1] == "string") { string textToDraw = stringVars.ContainsKey(parts[4]) ? stringVars[parts[4]] : (vars.ContainsKey(parts[4]) ? vars[parts[4]].ToString() : "null"); using (var br = new SolidBrush(Color.FromArgb((int)GetVal(parts[5]), (int)GetVal(parts[6]), (int)GetVal(parts[7])))) renderGraphics.DrawString(textToDraw, new Font("Consolas", 12), br, (int)GetVal(parts[2]), (int)GetVal(parts[3])); }
                            else if (parts[1] == "render") UpdateViewport();
                            break;

                        case "audio":
                            if (parts[1] == "load") audioEngine.LoadAudio(parts[2], string.Join(" ", parts, 3, parts.Length - 3));
                            else if (parts[1] == "play") audioEngine.PlayAudio(parts[2], parts.Length > 3 ? GetVal(parts[3]) : 0);
                            else if (parts[1] == "stop") audioEngine.StopAudio(parts[2]);
                            break;
                    }
                }
                catch (Exception ex) { Log($"Runtime Error line {pc + 1}: {ex.Message}", Color.Red); }
                pc++;
            }
            Log(">>> Execution Finished.", Color.Gray);
        }

        private double GetVal(string s)
        {
            return vars.ContainsKey(s) ? vars[s] : (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double val) ? val : 0);
        }

        private Color GetRandomColor()
        {
            return Color.FromArgb(rnd.Next(50, 255), rnd.Next(50, 255), rnd.Next(50, 255));
        }

        private void UpdateViewport()
        {
            if (engineWindow == null || engineWindow.IsDisposed || engineWindow.viewport.InvokeRequired)
            {
                if (engineWindow != null && !engineWindow.IsDisposed)
                    engineWindow.viewport.Invoke(new Action(UpdateViewport));
                return;
            }

            // Быстрое копирование буфера на экран
            Bitmap clone = (Bitmap)renderBuffer.Clone();
            if (engineWindow.viewport.Image != null) engineWindow.viewport.Image.Dispose();
            engineWindow.viewport.Image = clone;
            engineWindow.viewport.Refresh();
        }
    }
}
