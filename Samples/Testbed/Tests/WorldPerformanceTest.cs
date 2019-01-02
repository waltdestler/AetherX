

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using tainicom.Aether.Physics2D.Collision;
using tainicom.Aether.Physics2D.Collision.Shapes;
using tainicom.Aether.Physics2D.Diagnostics;
using tainicom.Aether.Physics2D.Dynamics;
using tainicom.Aether.Physics2D.Dynamics.Hibernation;
using tainicom.Aether.Physics2D.Loaders.RUBE;
using tainicom.Aether.Physics2D.Samples.Testbed.Framework;
using tainicom.Aether.Physics2D.Utilities;

namespace tainicom.Aether.Physics2D.Samples.Testbed.Tests
{
    public class WorldPerformanceTest : Test
    {
        private Dictionary<Keys, int> WorldSideSizeOptions = new Dictionary<Keys, int>
        {
            { Keys.Q, 500 },
            { Keys.W, 1000 },
            { Keys.E, 2000 },
            { Keys.R, 5000 },
            { Keys.T, 10000 },
            { Keys.Y, 15000 }
        };

        private Dictionary<Keys, int> BodyTypeOptions = new Dictionary<Keys, int>
        {
            { Keys.U, 1 },  // single body, single fixture
            { Keys.I, 2 }, // single body, multiple fixture            
            { Keys.O, 3 }, // multiple body (joined by joints), multiple fixture 
            { Keys.P, 4 }, // random 33% mix of all 3!!!
        };

        private Dictionary<Keys, int> MetersPerBodyOptions = new Dictionary<Keys, int>
        {
            { Keys.Z, 25 },  
            { Keys.X, 50 },
            { Keys.C, 100 },
            { Keys.V, 300 }, 
        };

        private Dictionary<Keys, BodyStructureType> BodyStructureTypeOptions = new Dictionary<Keys, BodyStructureType>
        {
            { Keys.J, BodyStructureType.SingleFixtureBox },
            { Keys.K, BodyStructureType.TwelveFixtureStructure }
        };

        private IndependentActiveArea ViewActiveArea { get; set; }
        DebugView HibernatedWorldDebugView { get; set; }

        private float WorldSideSize { get; set; }
        private float WorldRadius { get { return this.WorldSideSize / 2f; } }
        private BodyStructureType BodyStructureType = BodyStructureType.TwelveFixtureStructure;

        // NOTE: This should always be greater than the biggest test body, otherwise things 
        //       could overlap, which is a huge perf reduction.
        //       Square this to get the square-meters-per-body.
        private float MetersPerBody;

        private WorldPerformanceTest()
        {
            // default to smallest world size
            this.WorldSideSize = this.WorldSideSizeOptions[Keys.Q];

            // set to 100m per body
            this.MetersPerBody = this.MetersPerBodyOptions[Keys.Z];
        }

        public override void Initialize()
        {
            this.World = new World();

            // enable multithreading
            this.World.ContactManager.VelocityConstraintsMultithreadThreshold = 256;
            this.World.ContactManager.PositionConstraintsMultithreadThreshold = 256;
            this.World.ContactManager.CollideMultithreadThreshold = 256;

            this.World.Gravity = Vector2.Zero;

            this.CreateBounds();

            this.CreateBodies();

            // sets up debug drawing
            base.Initialize();

            // automatically enable additional performance info
            this.DebugView.AppendFlags(Diagnostics.DebugViewFlags.PerformanceGraph);
            this.DebugView.AppendFlags(Diagnostics.DebugViewFlags.DebugPanel);
            this.DebugView.AppendFlags(Diagnostics.DebugViewFlags.AABB);
            this.DebugView.Enabled = true;

            // set zoom to show a meaningful part of the world
            //this.GameInstance.ViewZoom = 0.09f;
        }

        private void CreateBodies()
        {
            // load the game world
            var tempWorld = new World();
            RubeLoader.Load("data/testbodies.json", tempWorld);

            // settings
            const float MAX_LINEAR_VELOCITY = 20.0f;
            const float MAX_ANGULAR_VELOCITY = 1.5F;

            // add the specific body
            //const int COMPLEX_BODY_INDEX = 2;
            //const int C_BODY_INDEX = 1;
            //const int BOX_BODY_INDEX = 0;

            var selectedBodyIndex = (int)this.BodyStructureType; //BOX_BODY_INDEX;

            for (var x = MetersPerBody; x < WorldSideSize - MetersPerBody; x += MetersPerBody)
            {
                for (var y = MetersPerBody; y < WorldSideSize - MetersPerBody; y += MetersPerBody)
                {
                    var bodyDef = tempWorld.BodyList[selectedBodyIndex];
                    var body = bodyDef.DeepClone(this.World);

                    body.Position = new Vector2(x - WorldRadius, y - WorldRadius);
                    body.LinearVelocity = RandomGenerator.Vector2(MAX_LINEAR_VELOCITY);
                    body.AngularVelocity = RandomGenerator.Float(-MAX_ANGULAR_VELOCITY, MAX_ANGULAR_VELOCITY);
                }
            }
        }

        private void CreateBounds() { 
            // create ring of edges
            var edgeVertices = new List<Vector2>();
            const float EDGE_SIZE = 10f;
            var edgesPerSide = (int)Math.Ceiling(WorldSideSize / EDGE_SIZE);
            var edgeXOffset = (edgesPerSide * EDGE_SIZE) / -2.0f;
            var edgeYOffset = edgeXOffset;
            for ( var i = 0; i < edgesPerSide; i++)
            {
                var horz1 = new Vector2(i * EDGE_SIZE + edgeXOffset, edgeYOffset);
                var horz2 = new Vector2((i + 1) * EDGE_SIZE + edgeXOffset, edgeYOffset);

                // create a body for the TOP edge
                var topBody = this.World.CreateBody(Vector2.Zero, 0, BodyType.Static);
                topBody.CreateFixture(new EdgeShape(horz1, horz2));

                // create a body for the BOTTOM edge
                var bottomBody = this.World.CreateBody(Vector2.Zero, 0, BodyType.Static);
                var bottomOffset = new Vector2(0, WorldSideSize);
                bottomBody.CreateFixture(new EdgeShape(horz1 + bottomOffset, horz2 + bottomOffset));


                var vert1 = new Vector2(edgeXOffset, i * EDGE_SIZE + edgeYOffset);
                var vert2 = new Vector2(edgeXOffset, (i + 1) * EDGE_SIZE + edgeYOffset);

                // create a body for the LEFT edge
                var leftBody = this.World.CreateBody(Vector2.Zero, 0, BodyType.Static);
                leftBody.CreateFixture(new EdgeShape(vert1, vert2));

                // create a body for the RIGHT edge
                var rightBody = this.World.CreateBody(Vector2.Zero, 0, BodyType.Static);
                var rightOffset = new Vector2(WorldSideSize, 0);
                rightBody.CreateFixture(new EdgeShape(vert1 + rightOffset, vert2 + rightOffset));

            };
        }

        public override void Update(GameSettings settings, GameTime gameTime)
        {
            base.Update(settings, gameTime);

            var cMgr = World.ContactManager;

            DrawString("Press Space to switch between single-core / multi-core solvers.");
            DrawString("Press 1-3 to set VelocityConstraints Threshold. (1-(0 - Always ON), 2-(256), 3-(int.MaxValue - Always OFF))");
            DrawString("Press 4-6 to set PositionConstraints Threshold. (4-(0 - Always ON), 5-(256), 6-(int.MaxValue - Always OFF))");
            DrawString("Press 7-9 to set Collide Threshold.             (7-(0 - Always ON), 8-(256), 9-(int.MaxValue - Always OFF))");

            TextLine += 15 * 10;
            var threshold = cMgr.VelocityConstraintsMultithreadThreshold;
            if (threshold == 0) DrawString("VelocityConstraintsMultithreadThreshold: 0");
            else if (threshold == 256) DrawString("VelocityConstraintsMultithreadThreshold: 256");
            else if (threshold == int.MaxValue) DrawString("VelocityConstraintsMultithreadThreshold: int.MaxValue");
            else DrawString("VelocityConstraintsMultithreadThreshold: " + threshold);
            threshold = cMgr.PositionConstraintsMultithreadThreshold;
            if (threshold == 0) DrawString("PositionConstraintsMultithreadThreshold: 0");
            else if (threshold == 256) DrawString("PositionConstraintsMultithreadThreshold: 256");
            else if (threshold == int.MaxValue) DrawString("PositionConstraintsMultithreadThreshold: int.MaxValue");
            else DrawString("PositionConstraintsMultithreadThreshold is Currently: " + threshold);
            threshold = cMgr.CollideMultithreadThreshold;
            if (threshold == 0) DrawString("CollideMultithreadThreshold: 0");
            else if (threshold == 256) DrawString("CollideMultithreadThreshold:  256");
            else if (threshold == int.MaxValue) DrawString("CollideMultithreadThreshold: int.MaxValue");
            else DrawString("CollideMultithreadThreshold is Currently: " + threshold);

            DrawString("[IsRunningSlowly = "+ gameTime.IsRunningSlowly.ToString().ToUpper() + "]");
            DrawString("Zoom = " + Math.Round(this.GameInstance.ViewZoom, 2) + ", World Radius (m) = " + WorldRadius + ", Meters per Body: " + MetersPerBody);

            TextLine += 15;
            DrawString("Press Left Alt to toggle debug rendering of game world.");
            DrawString("Debug rendering enabled: " + this.DebugView.Enabled);

            TextLine += 15;
            DrawString("Press to set broadphase algorithm. (J = "+ DYNAMICTREE_BROADPHASE_NAME + ", K = "+ QUADTREE_BROADPHASE_NAME + ", L = "+ BODY_DYNAMICTREE_BROADPHASE_NAME + ")");
            DrawString("Current broadphase algorithm: " + currentBroadPhaseName);

            // World size options
            TextLine += 15;
            var worldSizeOptions = string.Empty;
            foreach( var key in this.WorldSideSizeOptions.Keys )
            {
                worldSizeOptions += string.Format("{0} = {1}m, ", key.ToString(), this.WorldSideSizeOptions[key]);
            }
            DrawString("Current world size (width & height): " + WorldSideSize.ToString() + "m");
            DrawString(string.Format("Press one of these keys to change it: ({0})", worldSizeOptions));

            // Meters-per-body options 
            TextLine += 15;
            var metersPerBodyOptions = string.Empty;
            foreach (var key in this.MetersPerBodyOptions.Keys)
            {
                metersPerBodyOptions += string.Format("{0} = {1}m, ", key.ToString(), this.MetersPerBodyOptions[key]);
            }
            DrawString("Current meters-per-body: " + MetersPerBody.ToString() + "m");
            DrawString(string.Format("Press one of these keys to change it: ({0})", metersPerBodyOptions));

            // Body structure type options 
            TextLine += 15;
            var bodyStructureTypeOptions = string.Empty;
            foreach (var key in this.BodyStructureTypeOptions.Keys)
            {
                bodyStructureTypeOptions += string.Format("{0} = {1}, ", key.ToString(), this.BodyStructureTypeOptions[key].ToString());
            }
            DrawString("Current body structure type: " + this.BodyStructureType.ToString());
            DrawString(string.Format("Press one of these keys to change it: ({0})", bodyStructureTypeOptions));

            // Hibernation toggling 
            TextLine += 15;
            DrawString("Hibernation enabled: " + this.World.HibernationEnabled.ToString().ToUpper() + ". Press 'h' to toggle. Right-click to create/position an 'active area.'");

            
        }

        public override void Mouse(MouseState state, MouseState oldState)
        {
            Vector2 position = GameInstance.ConvertScreenToWorld(state.X, state.Y);

            if (state.RightButton == ButtonState.Pressed)
            {
                // set view active area, if hibernation is enabled
                if (this.World.HibernationEnabled)
                {
                    if (this.ViewActiveArea == null)
                    {
                        // init and add
                        this.ViewActiveArea = new IndependentActiveArea();
                        this.World.HibernationManager.ActiveAreas.Add(this.ViewActiveArea);
                    }

                    // set it to match current view
                    this.ViewActiveArea.SetPosition(position);
                    this.ViewActiveArea.SetRadius(20f);
                }

            }
        }

        const string DYNAMICTREE_BROADPHASE_NAME = "DynamicTree";
        const string QUADTREE_BROADPHASE_NAME = "QuadTree";
        const string BODY_DYNAMICTREE_BROADPHASE_NAME = "BodyDynamicTree";
        private string currentBroadPhaseName = DYNAMICTREE_BROADPHASE_NAME;

        public override void DrawDebugView(GameTime gameTime, ref Matrix projection, ref Matrix view)
        {
            base.DrawDebugView(gameTime, ref projection, ref view);

            // render game center
            this.DebugView.BeginCustomDraw(projection, view);
            this.DebugView.DrawSegment(Vector2.Zero, new Vector2(1, 0), Color.Green);
            this.DebugView.DrawSegment(Vector2.Zero, new Vector2(0, 1), Color.Red);
            this.DebugView.EndCustomDraw();

            if (this.World.HibernationEnabled) {
                // render active areas
                Color activeAreaAABBcolor = new Color(0.9f, 0.3f, 0.3f);
                Color activeAreaCircleColor = new Color(0.9f, 0.3f, 0.3f, 0.25f);
                this.DebugView.BeginCustomDraw(projection, view);
                foreach (var activeArea in this.World.HibernationManager.ActiveAreas) {
                    this.DebugView.DrawAABB(ref activeArea.AABB, activeAreaAABBcolor);
                    //this.DebugView.DrawSolidCircle(activeArea.Position, activeArea.Radius, Vector2.Zero, activeAreaCircleColor);
                    //this.DebugView.DrawCircle(activeArea.AABB.Center, activeArea.AABB.Extents.Length(), Color.Gray);

                    // render number of bodies within
                    Vector2 position = new Vector2(activeArea.AABB.LowerBound.X, activeArea.AABB.UpperBound.Y);
                    position = GameInstance.ConvertWorldToScreen(position);
                    DebugView.DrawString((int)position.X, (int)position.Y - 5, "Contains " + activeArea.Bodies.Count.ToString());
                }
                this.DebugView.EndCustomDraw();
            }

            if (this.HibernatedWorldDebugView != null)
            {
                this.HibernatedWorldDebugView.RenderDebugData(ref projection, ref view);
            }
        }

        public override void Keyboard(KeyboardManager keyboardManager)
        {
            base.Keyboard(keyboardManager);

            var cMgr = World.ContactManager;         

            if (keyboardManager.IsNewKeyPress(Keys.D1))
                cMgr.VelocityConstraintsMultithreadThreshold = 0;
            if (keyboardManager.IsNewKeyPress(Keys.D2))
                cMgr.VelocityConstraintsMultithreadThreshold = 256;
            if (keyboardManager.IsNewKeyPress(Keys.D3))
                cMgr.VelocityConstraintsMultithreadThreshold = int.MaxValue;

            if (keyboardManager.IsNewKeyPress(Keys.D4))
                cMgr.PositionConstraintsMultithreadThreshold = 0;
            if (keyboardManager.IsNewKeyPress(Keys.D5))
                cMgr.PositionConstraintsMultithreadThreshold = 256;
            if (keyboardManager.IsNewKeyPress(Keys.D6))
                cMgr.PositionConstraintsMultithreadThreshold = int.MaxValue;
            
            if (keyboardManager.IsNewKeyPress(Keys.D7))
                cMgr.CollideMultithreadThreshold = 0;
            if (keyboardManager.IsNewKeyPress(Keys.D8))
                cMgr.CollideMultithreadThreshold = 256;
            if (keyboardManager.IsNewKeyPress(Keys.D9))
                cMgr.CollideMultithreadThreshold = int.MaxValue;

            if (keyboardManager.IsNewKeyPress(Keys.Space))
            {
                if (cMgr.VelocityConstraintsMultithreadThreshold == int.MaxValue)
                    cMgr.VelocityConstraintsMultithreadThreshold = 0;
                else
                    cMgr.VelocityConstraintsMultithreadThreshold = int.MaxValue;
                cMgr.PositionConstraintsMultithreadThreshold = cMgr.VelocityConstraintsMultithreadThreshold;
                cMgr.CollideMultithreadThreshold = cMgr.VelocityConstraintsMultithreadThreshold;

            }
            
            // World side size.
            foreach( var key in this.WorldSideSizeOptions.Keys)
            {
                if( keyboardManager.IsNewKeyPress(key) )
                {
                    var pressedSize = this.WorldSideSizeOptions[key];
                    if( pressedSize != this.WorldSideSize )
                    {
                        this.WorldSideSize = pressedSize;
                        this.Initialize();
                    }
                    break;
                }
            }

            // Meters per body.
            foreach (var key in this.MetersPerBodyOptions.Keys)
            {
                if (keyboardManager.IsNewKeyPress(key))
                {
                    var newMetersPerBody = this.MetersPerBodyOptions[key];
                    if (newMetersPerBody != this.MetersPerBody)
                    {
                        this.MetersPerBody = newMetersPerBody;
                        this.Initialize();
                    }
                    break;
                }
            }

            // Body structure type.
            foreach (var key in this.BodyStructureTypeOptions.Keys)
            {
                if (keyboardManager.IsNewKeyPress(key))
                {
                    var newBodyStructureType = this.BodyStructureTypeOptions[key];
                    if (newBodyStructureType != this.BodyStructureType)
                    {
                        this.BodyStructureType = newBodyStructureType;
                        this.Initialize();
                    }
                    break;
                }
            }

            if( keyboardManager.IsNewKeyPress(Keys.LeftAlt))
            {
                this.DebugView.Enabled = !this.DebugView.Enabled;
            }

            if (keyboardManager.IsNewKeyPress(Keys.H))
            {
                if (this.World.HibernationEnabled)
                {
                    // it is enabled, and wer'e about to disable it, so clear the view active area
                    this.ViewActiveArea = null;

                    // also, destroy the debug view
                    this.HibernatedWorldDebugView.Dispose();
                    this.HibernatedWorldDebugView = null;
                } 

                // toggle hibernation
                this.World.HibernationEnabled = !this.World.HibernationEnabled;

                if( this.World.HibernationEnabled)
                {
                    // add second debug view for hibernated world
                    this.HibernatedWorldDebugView = new DebugView(this.World.HibernationManager.HibernatedWorld);
                    this.HibernatedWorldDebugView.LoadContent(GameInstance.GraphicsDevice, GameInstance.Content);

                    // draw everything in the hibernated world as grey
                    this.HibernatedWorldDebugView.AppendFlags(Diagnostics.DebugViewFlags.AABB);
                    this.HibernatedWorldDebugView.BodyAabbColor
                        = this.HibernatedWorldDebugView.BodyAabbRadiusColor
                        = this.HibernatedWorldDebugView.FixtureAabbColor
                        = this.HibernatedWorldDebugView.DefaultShapeColor 
                        = this.HibernatedWorldDebugView.InactiveShapeColor
                        = this.HibernatedWorldDebugView.KinematicShapeColor
                        = this.HibernatedWorldDebugView.SleepingShapeColor
                        = this.HibernatedWorldDebugView.StaticShapeColor
                        = this.HibernatedWorldDebugView.PolygonVertexColor
                        = this.HibernatedWorldDebugView.JointSegmentColor
                        = new Color(0.10f, 0.10f, 0.10f, 0.25f );
                }
            }
        }

        public static Test Create()
        {
            return new WorldPerformanceTest();
        }
    }

    public enum BodyStructureType
    {
        SingleFixtureBox = 0,
        TwelveFixtureStructure = 2
    }
}