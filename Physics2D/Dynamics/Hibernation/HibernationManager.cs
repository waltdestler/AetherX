using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace tainicom.Aether.Physics2D.Dynamics.Hibernation
{
    public class HibernationManager
    {
        private World ActiveWorld { get; set; }
        private World HibernatedWorld { get; set; }
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

                #endregion
            }

            // process all active areas
            for (var i = 0; i < this.ActiveAreas.Count; i++)
            {
                // get current active area
                var activeArea = this.ActiveAreas[i];

                // TODO: create AA for bodies only partilly within a AA
                // TODO: remove AA for bodies fully within this AA
                // TODO: hibernate bodies which are outside of AA and haven't collided...
            }
        }
    }
}
