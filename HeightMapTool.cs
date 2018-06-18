using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace HeightMapTool
{
    public partial class HeightMapTool : Form
    {
        #region Member Method
        public HeightMapTool()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque, true);

            // Window 초기화
            InitializeComponent();

            // Graphics 요소 초기화
            InitializeGraphics();
            // Keyboard, Mouse 초기화
            InitializeEventHandler();
        }
        //
        // Graphics 요소 초기화
        //
        private void InitializeGraphics()
        {
            // DirectX의 SwapChain이랑 비슷하다고 보면 된다.
            PresentParameters pp = new PresentParameters {
                Windowed = true,
                SwapEffect = SwapEffect.Discard,
                EnableAutoDepthStencil = true,
                AutoDepthStencilFormat = DepthFormat.D16 };

            // D3D 장치를 생성한다.
            device = new Device(0, DeviceType.Hardware, this, CreateFlags.HardwareVertexProcessing, pp);

            // Geometry Data 생성
            BuildBasicGeometry_Vertex();
            BuildGeometry_Index();
            BuildGeometry_Buffer();
        }
        //
        // Graphics 요소 초기화 → Geometry Data 생성
        //
        private void BuildBasicGeometry_Vertex()
        {
            int k = 0;
            for (int z = 0; z < HeightVertices; ++z) {
                for (int x = 0; x < WidthVertices; ++x) {
                    vertices[k].Position = new Vector3((float)x, 0.0f, (float)z);
                    vertices[k].Color = Color.White.ToArgb();

                    ++k;
                }
            }
        }
        private void BuildGeometry_Index()
        {
            int k = 0;
            // Clockwise로 감김 설정
            for (int z = 0; z < HeightVertices - 1; ++z) {
                for (int x = 0; x < WidthVertices - 1; ++x) {
                    indices[k] = z * WidthVertices + x;
                    indices[k + 1] = (z + 1) * WidthVertices + x;
                    indices[k + 2] = (z + 1) * WidthVertices + (x + 1);

                    indices[k + 3] = z * WidthVertices + x;
                    indices[k + 4] = (z + 1) * WidthVertices + (x + 1);
                    indices[k + 5] = z * WidthVertices + (x + 1);

                    k += 6;
                }
            }
        }
        private void BuildGeometry_Buffer()
        {
            vb = new VertexBuffer(typeof(CustomVertex.PositionColored), NumVertices, device, Usage.Dynamic | Usage.WriteOnly, CustomVertex.PositionColored.Format, Pool.Default);
            vb.SetData(vertices, 0, LockFlags.None);

            ib = new IndexBuffer(typeof(int), NumIndices, device, Usage.WriteOnly, Pool.Default);
            ib.SetData(indices, 0, LockFlags.None);
        }
        //
        // Keyboard, Mouse 초기화
        //
        private void InitializeEventHandler()
        {
            this.KeyDown += new KeyEventHandler(OnKeyDown);
            this.MouseWheel += new MouseEventHandler(OnMouseWheel);
            this.MouseMove += new MouseEventHandler(OnMouseMove);
            this.MouseDown += new MouseEventHandler(OnMouseDown);
            this.MouseUp += new MouseEventHandler(OnMouseUp);
        }
        //
        // 주 Rendering 구문
        //
        private void OnRender(object sender, PaintEventArgs e)
        {
            // BackBuffer 초기화
            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1, 0);

            // View 설정
            SetView();

            // Rendering 시작
            device.BeginScene();

            // RenderState 설정(Light off, WireFrame)
            device.RenderState.Lighting = false;
            device.RenderState.FillMode = FillMode.WireFrame;

            // Pipeline에 VB/IB Binding → Rendering
            device.VertexFormat = CustomVertex.PositionColored.Format;
            device.SetStreamSource(0, vb, 0);
            device.Indices = ib;
            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, NumVertices, 0, NumIndices / 3);

            // Rendering 종료 → BackBuffer 전환
            device.EndScene();
            device.Present();

            menuStrip.Update();

            this.Invalidate();
        }
        //
        // View 설정
        //
        private void SetView()
        {
            // Look Vector 설정(*항상 Eye의 z축을 바라보도록)
            eyeLook.X = eyePos.X;
            eyeLook.Y = (float)(eyePos.Y + Math.Sin(rotX));
            eyeLook.Z = (float)(eyePos.Z + 1.0f + Math.Sin(rotX));

            // 투영 행렬과 시야 행렬 설정
            device.Transform.Projection = Matrix.PerspectiveFovLH((float)Math.PI / 4, this.Width / this.Height, 1.0f, 1000.0f);
            device.Transform.View = Matrix.LookAtLH(eyePos, eyeLook, eyeUp);
        }
        //
        // Menu Event 설정
        //
        private void MenuItem_LoadHeightMap_Click(object sender, EventArgs e)
        {
            LoadHeightMap();
            vb.SetData(vertices, 0, LockFlags.None);
        }
        private void MenuItem_SaveHeightMap_Click(object sender, EventArgs e)
        {
            SaveHeightMap();
        }
        private void MenuItem_Reset_Click(object sender, EventArgs e)
        {
            BuildBasicGeometry_Vertex();
            vb.SetData(vertices, 0, LockFlags.None);
        }
        //
        // Load HeightMap File(.RAW)
        //
        private void LoadHeightMap()
        {
            using (OpenFileDialog ofd = new OpenFileDialog()) {
                ofd.Title = "Load HeightMap";
                ofd.Filter = "8Bit RAW file (*.raw)|*.raw";
                if (ofd.ShowDialog(this) == DialogResult.OK) {
                    // Raw File을 읽어와서 byte[]에 저장한다.
                    byte[] temp = File.ReadAllBytes(ofd.FileName);

                    // byte[]를 float[]로 변환하면서 Scale도 조정해준다.
                    for (int i = 0; i < NumVertices; ++i) {
                        HeightMap[i] = temp[i] / 255.0f * HeightScale;
                    }

                    // 절벽 현상을 방지하기 위해 평활화를 해준다. 실제 높이보다 매우 낮아질 수 있으므로 문제가 될 시 주석처리하면 된다.
                    Smooth();

                    // 얻은 높이 정보를 가지고 Vertices의 Position을 조정한다.
                    int k = 0;
                    for (int z = 0; z < HeightVertices; ++z) {
                        for (int x = 0; x < WidthVertices; ++x) {
                            vertices[k].Position = new Vector3(x, HeightMap[k], z);
                            vertices[k].Color = Color.White.ToArgb();
                            ++k;
                        }
                    }
                }
            }
        }
        //
        // 평활화
        //
        private void Smooth()
        {
            float[] temp = new float[NumVertices];

            for (int z = 0; z < WidthVertices; ++z) {
                for (int x = 0; x < HeightVertices; ++x) {
                    temp[z * WidthVertices + x] = Average(x, z);
                }
            }

            HeightMap = temp;
        }
        //
        // 평균 구하기
        //
        private float Average(int x, int z)
        {
            float avg = 0.0f;
            float num = 0.0f;

            // 주위 8개 점의 합산을 구하여 이를 평균낸 값을 높이로 다시 설정한다.
            for (int i = z - 1; i <= z + 1; ++i) {
                for (int j = x - 1; j <= x + 1; ++j) {
                    if (InBounds(i, j)) {
                        avg += HeightMap[i * WidthVertices + j];
                        num += 1.0f;
                    }
                }
            }

            return avg / num;
        }
        //
        // 유효성 검사
        //
        private bool InBounds(int i, int j)
        {
            return i >= 0 && i < HeightVertices && 
                   j >= 0 && j < WidthVertices;
        }
        //
        // Save HeightMap File(.RAW)
        //
        private void SaveHeightMap()
        {
            using (SaveFileDialog sfd = new SaveFileDialog()) {
                byte[] temp = new byte[NumVertices];

                // Grid의 정보 중 Position의 Y축 정보만 따로 떼어 byte[]로 저장한다.
                for (int i = 0; i < NumVertices; ++i) {
                    temp[i] = Convert.ToByte(vertices[i].Y);
                }

                // .RAW 확장자로 byte[]를 저장한다.
                sfd.Title = "Save HeightMap";
                sfd.Filter = "8Bit RAW file (*.raw)|*.raw";
                if (sfd.ShowDialog(this) == DialogResult.OK) {
                    File.WriteAllBytes(sfd.FileName, temp);
                }
            }
        }
        //
        // Keyboard 설정
        //
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode) {
                case (Keys.W):
                    {
                        eyePos.Z += 1.0f;
                        break;
                    }
                case (Keys.S):
                    {
                        eyePos.Z -= 1.0f;
                        break;
                    }
                case (Keys.D):
                    {
                        eyePos.X += 1.0f;
                        break;
                    }

                case (Keys.A):
                    {
                        eyePos.X -= 1.0f;
                        break;
                    }
                case (Keys.E):
                    {
                        if (rotX < Math.PI / 2) {
                            rotX += EyeTurnSpeed;
                        }
                        break;
                    }
                case (Keys.Q):
                    {
                        if (rotX > -Math.PI / 2) {
                            rotX -= EyeTurnSpeed;
                        }
                        break;
                    }
                case (Keys.Space):
                    {
                        if (isLeftMouseDown) {
                            PickingTriangle(mousePoint);
                        }
                        break;
                    }
            }
        }
        //
        // Mouse 설정
        //
        private void OnMouseWheel(object sender, MouseEventArgs e)
        {
            eyePos.Y -= e.Delta * EyeYSpeed;
        }
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (isLeftMouseDown) {
                mousePoint = new Point(e.X, e.Y);
            }
        }
        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            switch (e.Button) {
                case (MouseButtons.Left):
                    {
                        isLeftMouseDown = true;
                        mousePoint = new Point(e.X, e.Y);
                        break;
                    }
            }
        }
        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            switch (e.Button) {
                case (MouseButtons.Left):
                    {
                        isLeftMouseDown = false;
                        break;
                    }
            }
        }
        //
        // Picking
        //
        private void PickingTriangle(Point mousePoint)
        {
            // Screen 상의 Mouse 위치를 구한다.
            Vector3 near = new Vector3(mousePoint.X, mousePoint.Y, 0.0f);
            Vector3 far = new Vector3(mousePoint.X, mousePoint.Y, 1.0f);
            // 해당 위치를 세계 공간으로 변환한다.
            near.Unproject(device.Viewport, device.Transform.Projection, device.Transform.View, device.Transform.World);
            far.Unproject(device.Viewport, device.Transform.Projection, device.Transform.View, device.Transform.World);

            // Ray의 방향을 구한다. 
            Vector3 direction = far - near;

            // 모든 삼각형들에 대해...
            for (int i = 0; i < NumIndices; i += 3) {
                // 만약 Ray에 맞은 삼각형이 있다면,
                if (Geometry.IntersectTri(vertices[indices[i]].Position, vertices[indices[i + 1]].Position, vertices[indices[i + 2]].Position, near, direction, out IntersectInformation hitPoint)) {
                    // 색깔을 붉은 색으로 조정해 시각화하고
                    vertices[indices[i]].Color = Color.Red.ToArgb();
                    vertices[indices[i + 1]].Color = Color.Red.ToArgb();
                    vertices[indices[i + 2]].Color = Color.Red.ToArgb();
                    // 높이를 조정한다.
                    vertices[indices[i]].Position += new Vector3(0, fixValue, 0);
                    vertices[indices[i + 1]].Position += new Vector3(0, fixValue, 0);
                    vertices[indices[i + 2]].Position += new Vector3(0, fixValue, 0);
                    // 그리고 VB를 갱신한다.
                    vb.SetData(vertices, 0, LockFlags.None);
                }
            }
        }
        #endregion



        #region Member Variable
        private Device device = null;
        //
        // Geometry Data
        //
        private CustomVertex.PositionColored[] vertices = new CustomVertex.PositionColored[NumVertices];
        private int[] indices = new int[NumIndices];
        private VertexBuffer vb = null;
        private IndexBuffer ib = null;
        private const int WidthVertices = 64;
        private const int HeightVertices = 64;
        private const int NumVertices = WidthVertices * HeightVertices;
        private const int NumIndices = (WidthVertices - 1) * (HeightVertices - 1) * 6;
        //
        // Height Data
        //
        private float[] HeightMap = new float[NumVertices];
        private const float HeightScale = 255.0f;
        //
        // View Data
        //
        private Vector3 eyePos = new Vector3(WidthVertices / 2.0f, 4.5f, -3.5f);
        private Vector3 eyeUp = new Vector3(0.0f, 1.0f, 0.0f);
        private Vector3 eyeLook = new Vector3(0.0f, 0.0f, 0.0f);
        private float rotX = 0.0f;
        private const float EyeYSpeed = 0.01f;
        private const float EyeTurnSpeed = 0.1f;
        //
        // Picking Data
        //
        private Point mousePoint = new Point(0, 0);
        private bool isLeftMouseDown = false;
        private const float fixValue = 1.0f;
        #endregion
    }
}