using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tainicom.Aether.Physics2D.Collision;

namespace tainicom.Aether.Physics2D.Dynamics.Hibernation
{
    public class HibernationManager
    {
        private World ActiveWorld { get; set; }
        public World HibernatedWorld { get; private set; }
        public List<BaseActiveArea> ActiveAreas = new List<BaseActiveArea>();
        private List<Body> BodiesToHibernate = new List<Body>();
        private List<Body> BodiesToWake = new List<Body>();

        public HibernationManager(World world)
        {
            // store reference to active world
            this.ActiveWorld = world;

            // create a new world to store hibernated bodies
            this.HibernatedWorld = new World();
            // TODO: set properties to match active world? does it matter, since we're not stepping?


            // do an initial game-world pass to hibernate all bodies
            // going forward, all active bodies will be gauranteed in be in an AA, so this won't be neccessary.
            // OPTIMIZATION IDEA: allow constructor to accept a list of active areas and process them prior to 
            //                    this initial hibernate, to prevent hibernating bodies we're going to instantly unhibernate.
            foreach (var body in this.ActiveWorld.BodyList)
            {
                // for now, we're just going to mark everything as neededing hibernation.
                this.BodiesToHibernate.Add(body);
            }
        }

        public void Update()
        {
            // We only process this when the world is unlocked, so we may safely modify bodies, etc.
            if (this.ActiveWorld.IsLocked)
                throw new InvalidOperationException("The active World is locked.");
            // We only process this when the world is unlocked, so we may safely modify bodies, etc.
            if (this.HibernatedWorld.IsLocked)
                throw new InvalidOperationException("The hibernated World is locked.");

            #region Hibernate all flagged bodies. TODO: move to last part of update? want to avoid any world step() between flagging and removing.

            // hibernate all flagged bodies.
            foreach( var body in this.BodiesToHibernate )
            {
                // clone into the hibernated world
                body.DeepClone(this.HibernatedWorld);

                // remove from the active world
                this.ActiveWorld.Remove(body);
            }

            // clear the list
            this.BodiesToHibernate.Clear();

            #endregion

            // process all active areas
            foreach( var activeArea in this.ActiveAreas)
            {
                // update its bounding box
                activeArea.UpdateAABB();

                #region un-hibernate bodies ("wake")

                // get all hibernated bodies in its aabb
                var hibernatedBodiesInActiveArea = this.HibernatedWorld.FindBodiesInAABB(ref activeArea.AABB);

                // wake them
                foreach( var hibernatedBody in hibernatedBodiesInActiveArea)
                {
                    // clone into the active world
                    hibernatedBody.DeepClone(this.ActiveWorld);

                    // remove from the hibernated world
                    this.HibernatedWorld.Remove(hibernatedBody);
                }

                // NOTE: in this case, we don't actually store the bodies in the ActiveArea. anything which 
                //       collides is instantly woken, and there's no need to store a history.

                #endregion
            }

            #region Add new bodies and update position statuses for all bodies in active area.

            // process all active areas
            for (var i = 0; i < this.ActiveAreas.Count; i++)
            {
                // get current active area
                var activeArea = this.ActiveAreas[i];

                // Find all bodies which collide with active area AABB.
                var bodiesInActiveArea = this.ActiveWorld.FindBodiesInAABB(ref activeArea.AABB);

                // add all bodies which weren't already in AA
                var activeAreaBodies = activeArea.Bodies.Select(b => b.Body).ToList();
                for (var biaaIndex = bodiesInActiveArea.Count - 1; biaaIndex >= 0; biaaIndex--)
                {
                    // get body
                    var body = bodiesInActiveArea[biaaIndex];

                    // determine if this body is already in the AA
                    var isAlreadyInActiveArea = activeAreaBodies.Contains(body);

                    if (!isAlreadyInActiveArea)
                    {
                        // didn't find it. add it.
                        activeArea.Bodies.Add(new AreaBody(body));
                    }
                }

                // Loop over current ActiveArea bodies to update their statuses.
                for (var areaBodyIndex = activeArea.Bodies.Count - 1; areaBodyIndex >= 0; areaBodyIndex--)
                {
                    // get the body
                    var areaBody = activeArea.Bodies[areaBodyIndex];
                    var body = areaBody.Body;

                    // store the old status
                    areaBody.PriorStatus = areaBody.PositionStatus;

                    // determine whether this body is still touching the AA's AABB
                    var isTouchingActiveAreaAABB = bodiesInActiveArea.Contains(body);

                    if (!isTouchingActiveAreaAABB)
                    {
                        // it's not touching the AABB at all, so it's totally out.
                        areaBody.PositionStatus = AreaBodyStatus.TotallyOut;
                    }
                    else
                    {
                        // get body AABB
                        var bodyTransform = body.GetTransform();
                        AABB bodyAabb;
                        this.HibernatedWorld.ContactManager.BroadPhase.GetFatAABB(body.ProxyId, out bodyAabb);

                        // at this point, we know it's touching. so it's just a matter of whether it's totally inside or partially inside.
                        // contains() returns 'true' only if the AABB is entirely within
                        var isTotallyWithinActiveArea = activeArea.AABB.Contains(ref bodyAabb);

                        if (isTotallyWithinActiveArea)
                        {
                            // it's totally inside the ActiveArea AABB.
                            areaBody.PositionStatus = AreaBodyStatus.TotallyIn;
                        }
                        else
                        {
                            // at this point, we know it must be partially within.
                            areaBody.PositionStatus = AreaBodyStatus.TotallyIn;
                        }
                    }
                }
            }

            #endregion


            #region Add new bodies and update position statuses for all bodies in active area.

            // process all active areas
            for (var i = 0; i < this.ActiveAreas.Count; i++)
            {
                // get current active area
                var activeArea = this.ActiveAreas[i];

                // Loop over current ActiveArea bodies to update their statuses.
                for (var areaBodyIndex = activeArea.Bodies.Count - 1; areaBodyIndex >= 0; areaBodyIndex--)
                {
                    // get the body
                    var areaBody = activeArea.Bodies[areaBodyIndex];
                    var body = areaBody.Body;

                    if( areaBody.PositionStatus == AreaBodyStatus.Invalid )
                    {
                        throw new InvalidProgramException("All active area bodies should have their position status set by this point.");
                    }

                    var statusHasChanged = areaBody.PriorStatus != areaBody.PositionStatus;
                    if( statusHasChanged )
                    {
                        switch( areaBody.PositionStatus )
                        {
                            case AreaBodyStatus.TotallyOut:
                                // TODO: make sure no other AA has this within it... 
                                // TODO: usually create an AA for it...
                                // temp: just hibernate the body. 
                                activeArea.Bodies.Remove(areaBody);
                                this.BodiesToHibernate.Add(body);
                                break;
                        }
                    }
                }
            }

            #endregion
            //if match found in collide bodies, remove from collide bodies (basically ignore). update status to 'in' or 'partially in' depending on "contains" call. include ramification.

            // any bodies which are in the active area's list of bodies, but weren't fonud in the AABB query should be marked as 
            // "totally outside" and any which are new should be added to the collection.

            //var body = hibernatedBodyResult.Body;

            //// get body AABB
            //var bodyTransform = body.GetTransform();
            //AABB bodyAabb;
            //this.HibernatedWorld.ContactManager.BroadPhase.GetFatAABB(body.ProxyId, out bodyAabb);

            //// AABB is a box, but active areas are actually circles, so we do a radius check.
            //// NOTE: we use the active area's position and radius for the check rather than active area's aabb
            //var bodyDistance = Vector2.Distance(bodyAabb.Center, activeArea.Position);
            //var bodyInActiveAreaCircle = bodyDistance < (bodyAabb.Radius + activeArea.Radius); 

            //if( !bodyInActiveAreaCircle)
            //{
            //    // didn't quite make it. it's in the AABB, but not within the active area's circle. skip it.
            //    continue;
            //}

            // TODO: create AA for bodies only partilly within a AA
            // TODO: remove AA for bodies fully within this AA
            // TODO: hibernate bodies which are outside of AA and haven't collided...

        }
    }
}
