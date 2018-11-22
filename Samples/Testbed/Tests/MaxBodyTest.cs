
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using tainicom.Aether.Physics2D.Collision;
using tainicom.Aether.Physics2D.Collision.Shapes;
using tainicom.Aether.Physics2D.Dynamics;
using tainicom.Aether.Physics2D.Loaders.RUBE;
using tainicom.Aether.Physics2D.Samples.Testbed.Framework;
using tainicom.Aether.Physics2D.Utilities;

namespace tainicom.Aether.Physics2D.Samples.Testbed.Tests
{
    public class MaxBodyTest : Test
    {
        const float WORLD_SIDE_SIZE = 10000f;
        const float WORLD_RADIUS = WORLD_SIDE_SIZE / 2f;

        // NOTE: This should always be greater than the biggest test body, otherwise things 
        //       could overlap, which is a huge perf reduction.
        //       Square this to get the square-meters-per-body.
        const float METERS_PER_BODY = 100f;

        private MaxBodyTest()
        {
  
        }

        public override void Initialize()
        {
            IBroadPhase broadphaseSolver;

            switch (this.currentBroadPhaseName)
            {
                case DYNAMICTREE_BROADPHASE_NAME:
                    broadphaseSolver = new DynamicTreeBroadPhase();
                    break;

                case QUADTREE_BROADPHASE_NAME:
                    broadphaseSolver = new QuadTreeBroadPhase(new AABB(Vector2.Zero, WORLD_SIDE_SIZE, WORLD_SIDE_SIZE));
                    break;

                case BODY_DYNAMICTREE_BROADPHASE_NAME:
                    throw new NotImplementedException();
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            this.World = new World(broadphaseSolver);

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

            // set zoom to show a meaningful part of the world
            this.GameInstance.ViewZoom = 0.09f;
        }

        private void CreateBodies()
        {
            // load the game world
            var tempWorld = new World();
            RubeLoader.Load("data/testbodies.json", tempWorld);

            // settings
            const float MAX_LINEAR_VELOCITY = 10.0f;
            const float MAX_ANGULAR_VELOCITY = 1.0F;

            // add the specific body
            const int COMPLEX_BODY_INDEX = 2;
            const int C_BODY_INDEX = 1;
            const int BOX_BODY_INDEX = 0;

            var selectedBodyIndex = BOX_BODY_INDEX;

            for (var x = METERS_PER_BODY; x < WORLD_SIDE_SIZE - METERS_PER_BODY; x += METERS_PER_BODY)
            {
                for (var y = METERS_PER_BODY; y < WORLD_SIDE_SIZE - METERS_PER_BODY; y += METERS_PER_BODY)
                {
                    var bodyDef = tempWorld.BodyList[selectedBodyIndex];
                    var body = bodyDef.DeepClone(this.World);

                    body.Position = new Vector2(x - WORLD_RADIUS, y - WORLD_RADIUS);
                    body.LinearVelocity = RandomGenerator.Vector2(MAX_LINEAR_VELOCITY);
                    body.AngularVelocity = RandomGenerator.Float(-MAX_ANGULAR_VELOCITY, MAX_ANGULAR_VELOCITY);
                }
            }
        }

        private void CreateBounds() { 
            // create ring of edges
            var edgeVertices = new List<Vector2>();
            const float EDGE_SIZE = 10f;
            var edgesPerSide = (int)Math.Ceiling(WORLD_SIDE_SIZE / EDGE_SIZE);
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
                var bottomOffset = new Vector2(0, WORLD_SIDE_SIZE);
                bottomBody.CreateFixture(new EdgeShape(horz1 + bottomOffset, horz2 + bottomOffset));


                var vert1 = new Vector2(edgeXOffset, i * EDGE_SIZE + edgeYOffset);
                var vert2 = new Vector2(edgeXOffset, (i + 1) * EDGE_SIZE + edgeYOffset);

                // create a body for the LEFT edge
                var leftBody = this.World.CreateBody(Vector2.Zero, 0, BodyType.Static);
                leftBody.CreateFixture(new EdgeShape(vert1, vert2));

                // create a body for the RIGHT edge
                var rightBody = this.World.CreateBody(Vector2.Zero, 0, BodyType.Static);
                var rightOffset = new Vector2(WORLD_SIDE_SIZE, 0);
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
            DrawString("Zoom = " + Math.Round(this.GameInstance.ViewZoom, 2) + ", World Radius (m) = " + WORLD_RADIUS + ", Meters per Body: " + METERS_PER_BODY);


            TextLine += 15;
            DrawString("Press A,B or C to set broadphase algorithm.     (A = "+ DYNAMICTREE_BROADPHASE_NAME + ", B = "+ QUADTREE_BROADPHASE_NAME + ", C = "+ BODY_DYNAMICTREE_BROADPHASE_NAME + ")");
            DrawString("Current broadphase algorithm: " + currentBroadPhaseName);
        
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
            
            // Broadphase switching
            string newBroadPhaseName = this.currentBroadPhaseName; 
            if (keyboardManager.IsNewKeyPress(Keys.A))
                newBroadPhaseName = DYNAMICTREE_BROADPHASE_NAME;
            if (keyboardManager.IsNewKeyPress(Keys.B))
                newBroadPhaseName = QUADTREE_BROADPHASE_NAME;
            if (keyboardManager.IsNewKeyPress(Keys.C))
                newBroadPhaseName = BODY_DYNAMICTREE_BROADPHASE_NAME;
            if(newBroadPhaseName != this.currentBroadPhaseName)
            {
                // store it
                this.currentBroadPhaseName = newBroadPhaseName;

                // restart sim so it may be applied.
                this.Initialize();
            }

        }

        public static Test Create()
        {
            return new MaxBodyTest();
        }
    }
}