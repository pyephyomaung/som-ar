//----------------------------------------------
// (c) 2007 by casey chesnut, brains-N-brawn LLC
//----------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Media3D;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Imaging;

namespace AndyPyeAugmentedRealitySystem
{
    class ParticleModel : ModelVisual3D, IModelCode
    {
        private MyModel mm;

        System.Windows.Threading.DispatcherTimer frameTimer;

        private Point3D spawnPoint;
        private double elapsed;
        private double totalElapsed;
        private int lastTick;
        private int currentTick;

        private int frameCount;
        private double frameCountTime;
        private int frameRate;

        private ParticleSystemManager pm;

        private Random rand;

        public void Init(MyModel mm)
        {
            this.mm = mm;

            // not used since dispatchertimer is stopped by compositerendering
            frameTimer = new System.Windows.Threading.DispatcherTimer();
            frameTimer.Tick += OnFrame;
            frameTimer.Interval = TimeSpan.FromSeconds(1.0 / 60.0);

            this.spawnPoint = new Point3D(0.0, 0.0, 0.0);
            this.lastTick = Environment.TickCount;

            pm = new ParticleSystemManager();

            Model3DGroup group = new Model3DGroup();
            group.Children.Add(pm.CreateParticleSystem(500, Colors.Gray));
            group.Children.Add(pm.CreateParticleSystem(500, Colors.Red));
            group.Children.Add(pm.CreateParticleSystem(500, Colors.Silver));
            group.Children.Add(pm.CreateParticleSystem(500, Colors.Orange));
            group.Children.Add(pm.CreateParticleSystem(500, Colors.Yellow));
            mm.effect_mv3d = new ModelVisual3D();
            mm.effect_mv3d.Content = group;

            rand = new Random(this.GetHashCode());
        }

        public void Update()
        {
            OnFrame(null, new EventArgs());
        }

        private void OnFrame(object sender, EventArgs e)
        {
            // Calculate frame time;
            this.currentTick = Environment.TickCount;
            this.elapsed = (double)(this.currentTick - this.lastTick) / 1000.0;
            this.totalElapsed += this.elapsed;
            this.lastTick = this.currentTick;

            frameCount++;
            frameCountTime += elapsed;
            if (frameCountTime >= 1.0)
            {
                frameCountTime -= 1.0;
                frameRate = frameCount;
                frameCount = 0;
                //this.FrameRateLabel.Content = "FPS: " + frameRate.ToString() + "  Particles: " + pm.ActiveParticleCount.ToString();
                //Console.WriteLine("FPS: " + frameRate.ToString() + "  Particles: " + pm.ActiveParticleCount.ToString());
            }

            pm.Update((float)elapsed);
            double speed = 10.0; //10.0
            double sizeMult = 10.0; //1.0
            pm.SpawnParticle(this.spawnPoint, speed, Colors.Red, rand.NextDouble() * sizeMult, 2.5 * rand.NextDouble());
            pm.SpawnParticle(this.spawnPoint, speed, Colors.Orange, rand.NextDouble() * sizeMult, 2.5 * rand.NextDouble());
            pm.SpawnParticle(this.spawnPoint, speed, Colors.Silver, rand.NextDouble() * sizeMult, 2.5 * rand.NextDouble());
            pm.SpawnParticle(this.spawnPoint, speed, Colors.Gray, rand.NextDouble() * sizeMult, 2.5 * rand.NextDouble());
            pm.SpawnParticle(this.spawnPoint, speed, Colors.Red, rand.NextDouble() * sizeMult, 2.5 * rand.NextDouble());
            pm.SpawnParticle(this.spawnPoint, speed, Colors.Orange, rand.NextDouble() * sizeMult, 2.5 * rand.NextDouble());
            pm.SpawnParticle(this.spawnPoint, speed, Colors.Silver, rand.NextDouble() * sizeMult, 2.5 * rand.NextDouble());
            pm.SpawnParticle(this.spawnPoint, speed, Colors.Gray, rand.NextDouble() * sizeMult, 2.5 * rand.NextDouble());
            pm.SpawnParticle(this.spawnPoint, speed, Colors.Red, rand.NextDouble() * sizeMult, 2.5 * rand.NextDouble());
            pm.SpawnParticle(this.spawnPoint, speed, Colors.Yellow, rand.NextDouble() * sizeMult, 2.5 * rand.NextDouble());
            pm.SpawnParticle(this.spawnPoint, speed, Colors.Silver, rand.NextDouble() * sizeMult, 2.5 * rand.NextDouble());
            pm.SpawnParticle(this.spawnPoint, speed, Colors.Yellow, rand.NextDouble() * sizeMult, 2.5 * rand.NextDouble());

            double c = Math.Cos(this.totalElapsed);
            double s = Math.Sin(this.totalElapsed);
            Point3D p3d = new Point3D(s * 32.0, c * 32.0, 0.0);
            //this.spawnPoint = p3d;
        }

        public void Start()
        {
            //frameTimer.Start();
        }

        public void Stop()
        {
            frameTimer.Stop();
        }
    }

    public class ParticleSystemManager
    {
        private Dictionary<Color, ParticleSystem> particleSystems;

        public ParticleSystemManager()
        {
            this.particleSystems = new Dictionary<Color, ParticleSystem>();
        }

        public void Update(float elapsed)
        {
            foreach (ParticleSystem ps in this.particleSystems.Values)
            {
                ps.Update(elapsed);
            }
        }

        public void SpawnParticle(Point3D position, double speed, Color color, double size, double life)
        {
            try
            {
                ParticleSystem ps = this.particleSystems[color];
                ps.SpawnParticle(position, speed, size, life);
            }
            catch { }
        }

        public Model3D CreateParticleSystem(int maxCount, Color color)
        {
            ParticleSystem ps = new ParticleSystem(maxCount, color);
            this.particleSystems.Add(color, ps);
            return ps.ParticleModel;
        }

        public int ActiveParticleCount
        {
            get
            {
                int count = 0;
                foreach (ParticleSystem ps in this.particleSystems.Values)
                    count += ps.Count;
                return count;
            }
        }
    }

    public class ParticleSystem
    {
        private List<Particle> particleList;
        private GeometryModel3D particleModel;
        private int maxParticleCount;
        private Random rand;

        public ParticleSystem(int maxCount, Color color)
        {
            this.maxParticleCount = maxCount;

            this.particleList = new List<Particle>();

            this.particleModel = new GeometryModel3D();
            this.particleModel.Geometry = new MeshGeometry3D();

            Ellipse e = new Ellipse();
            e.Width = 32.0;
            e.Height = 32.0;
            RadialGradientBrush b = new RadialGradientBrush();
            b.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, color.R, color.G, color.B), 0.25));
            b.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, color.R, color.G, color.B), 1.0));
            e.Fill = b;
            e.Measure(new Size(32, 32));
            e.Arrange(new Rect(0, 0, 32, 32));

            Brush brush = null;

#if USE_VISUALBRUSH
            brush = new VisualBrush(e);
#else
            RenderTargetBitmap renderTarget = new RenderTargetBitmap(32, 32, 96, 96, PixelFormats.Pbgra32);
            renderTarget.Render(e);
            renderTarget.Freeze();
            brush = new ImageBrush(renderTarget);
#endif

            DiffuseMaterial material = new DiffuseMaterial(brush);

            this.particleModel.Material = material;

            this.rand = new Random(brush.GetHashCode());
        }

        public void Update(double elapsed)
        {
            List<Particle> deadList = new List<Particle>();

            // Update all particles
            foreach (Particle p in this.particleList)
            {
                p.Position += p.Velocity * elapsed;
                p.Life -= p.Decay * elapsed;
                p.Size = p.StartSize * (p.Life / p.StartLife);
                if (p.Life <= 0.0)
                    deadList.Add(p);
            }

            foreach (Particle p in deadList)
                this.particleList.Remove(p);

            UpdateGeometry();
        }

        private void UpdateGeometry()
        {
            Point3DCollection positions = new Point3DCollection();
            Int32Collection indices = new Int32Collection();
            PointCollection texcoords = new PointCollection();

            for (int i = 0; i < this.particleList.Count; ++i)
            {
                int positionIndex = i * 4;
                int indexIndex = i * 6;
                Particle p = this.particleList[i];

                Point3D p1 = new Point3D(p.Position.X, p.Position.Y, p.Position.Z);
                Point3D p2 = new Point3D(p.Position.X, p.Position.Y + p.Size, p.Position.Z);
                Point3D p3 = new Point3D(p.Position.X + p.Size, p.Position.Y + p.Size, p.Position.Z);
                Point3D p4 = new Point3D(p.Position.X + p.Size, p.Position.Y, p.Position.Z);

                positions.Add(p1);
                positions.Add(p2);
                positions.Add(p3);
                positions.Add(p4);

                Point t1 = new Point(0.0, 0.0);
                Point t2 = new Point(0.0, 1.0);
                Point t3 = new Point(1.0, 1.0);
                Point t4 = new Point(1.0, 0.0);

                texcoords.Add(t1);
                texcoords.Add(t2);
                texcoords.Add(t3);
                texcoords.Add(t4);

                indices.Add(positionIndex);
                indices.Add(positionIndex + 2);
                indices.Add(positionIndex + 1);
                indices.Add(positionIndex);
                indices.Add(positionIndex + 3);
                indices.Add(positionIndex + 2);
            }

            ((MeshGeometry3D)this.particleModel.Geometry).Positions = positions;
            ((MeshGeometry3D)this.particleModel.Geometry).TriangleIndices = indices;
            ((MeshGeometry3D)this.particleModel.Geometry).TextureCoordinates = texcoords;

        }

        public void SpawnParticle(Point3D position, double speed, double size, double life)
        {
            if (this.particleList.Count > this.maxParticleCount)
                return;
            Particle p = new Particle();
            p.Position = position;
            p.StartLife = life;
            p.Life = life;
            p.StartSize = size;
            p.Size = size;

            float x = 1.0f - (float)rand.NextDouble() * 2.0f;
            float z = 1.0f - (float)rand.NextDouble() * 2.0f;
            float y = 1.0f - (float)rand.NextDouble() * 2.0f; //mod add

            Vector3D v = new Vector3D(x, z, 0.0);
            //Vector3D v = new Vector3D(x, z, y); //mod
            v.Normalize();
            v *= ((float)rand.NextDouble() + 0.25f) * (float)speed;

            p.Velocity = new Vector3D(v.X, v.Y, v.Z);

            p.Decay = 1.0f;// 0.5 + rand.NextDouble();
            //if (p.Decay > 1.0)
            //    p.Decay = 1.0;

            this.particleList.Add(p);
        }

        public int MaxParticleCount
        {
            get
            {
                return this.maxParticleCount;
            }
            set
            {
                this.maxParticleCount = value;
            }
        }

        public int Count
        {
            get
            {
                return this.particleList.Count;
            }
        }

        public Model3D ParticleModel
        {
            get
            {
                return this.particleModel;
            }
        }
    }


    public class Particle
    {
        public Point3D Position;
        public Vector3D Velocity;
        public double StartLife;
        public double Life;
        public double Decay;
        public double StartSize;
        public double Size;
    }
}
