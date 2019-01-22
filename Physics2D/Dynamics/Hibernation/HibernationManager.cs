using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tainicom.Aether.Physics2D.Collision;
using tainicom.Aether.Physics2D.Dynamics.Contacts;

namespace tainicom.Aether.Physics2D.Dynamics.Hibernation
{
    public class HibernationManager
    {
        private World ActiveWorld { get; set; }
        public World HibernatedWorld { get; private set; }
        public List<BaseActiveArea> ActiveAreas = new List<BaseActiveArea>();
        private List<Body> BodiesToHibernate = new List<Body>();

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

            // This should always come first. It updates AABBs, expiration timers, etc.
            this.UpdateActiveAreas();

            this.MergeBodyActiveAreas();

            // Handle expirations.
            this.RemoveExpiredActiveAreas();

            // un-hibernate bodies ("wake")
            this.WakeBodiesInActiveAreas();

            // Add new bodies and update positions.
            this.UpdateActiveAreaBodies();

            // Enact ramifications of status changes.
            this.ProcessActiveAreaBodyPositionChanges();

            // Hibernate all flagged bodies.
            this.HibernateBodies();
        }

        private void MergeBodyActiveAreas()
        {
            var bodyAAs = this.ActiveAreas.Where(aa => aa.AreaType == ActiveAreaType.BodyTracking).ToList();

            // get first in list
            var curBodyAA = bodyAAs.ElementAtOrDefault(0);
            while (curBodyAA != null)
            {
                // remove first from the list
                bodyAAs.RemoveAt(0);

                // find all other bodyAA which touch this one.
                var touchingAAs = bodyAAs.Where(aa => curBodyAA.AABB.Overlaps(ref aa.AABB)).ToList();

                for( var i = touchingAAs.Count - 1; i >= 0; i--)
                {
                    var touchingAA = touchingAAs[i];

                    // add all these AABBs to this AA
                    (curBodyAA as BodyActiveArea).AdditionalAABBs.Add(touchingAA.AABB);

                    var curBodyAABodies = curBodyAA.Bodies.Select(ab => ab.Body);
                    foreach (var touchingAABody in touchingAA.Bodies)
                    {
                        if (!curBodyAABodies.Contains(touchingAABody.Body))
                        {
                            // this body isn't in the current body AA, so let's add it.
                            curBodyAA.Bodies.Add(touchingAABody);
                        }
                    }

                    // ensure expiration time
                    //(curBodyAA as BodyActiveArea).EnsureExpirationNoLessThan(touchingAA as BodyActiveArea);

                    touchingAA.Bodies.Clear();

                    // remove them from the list of body AAs
                    bodyAAs.Remove(touchingAA);

                    // also from the global AAs
                    this.ActiveAreas.Remove(touchingAA);
                }

                // next in list
                curBodyAA = bodyAAs.ElementAtOrDefault(0);
            }
        }

        private void HibernateBodies()
        {
            // hibernate all flagged bodies.
            foreach (var body in this.BodiesToHibernate)
            {
                this.HibernateBody(body);
            }

            // clear the list
            this.BodiesToHibernate.Clear();
        }

        private void ProcessActiveAreaBodyPositionChanges()
        {

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

                    if (areaBody.PositionStatus == AreaBodyStatus.Invalid)
                    {
                        throw new InvalidProgramException("All active area bodies should have their position status set by this point.");
                    }

                    if (areaBody.PositionStatus == AreaBodyStatus.TotallyIn)
                    {

                        // this body is totally within this AA, so if there's a separate AA tracking this one specifically, 
                        // it's not needed, as that would be redundant.
                        // OPTIMIZATION IDEA: give body a ref to its own tracking AA. if !null when set, throw. super-easy look-up.
                        var bodyAAs = this.ActiveAreas.Where(aa =>
                            aa != activeArea // other AA is not this AA
                            && aa.AreaType == ActiveAreaType.BodyTracking // other AA is a body tracking AA...
                            && (aa as BodyActiveArea).TrackedBody == body // ...and it's tracking this body specifically
                            ); //&& aa.Bodies.Select( aab => aab.Body ).Any( b => b == body ) ); // 

                        switch (bodyAAs.Count())
                        {
                            case 0:
                                // no problem. no bodyAA to care about.
                                break;

                            case 1:
                                // destroy it! it is redundant.
                                this.ActiveAreas.Remove(bodyAAs.First());
                                break;

                            default:
                                // this really shouldn't happen. it means there is more than one bodyAA for this body.
                                throw new InvalidProgramException("There is more than one ActiveArea for this body. This should never happen. There is a bug elsewhere.");
                        }
                    }

                    var statusHasChanged = areaBody.PriorStatus != areaBody.PositionStatus;
                    if (statusHasChanged)
                    {
                        switch (areaBody.PositionStatus)
                        {


                            case AreaBodyStatus.PartiallyIn:
                            case AreaBodyStatus.TotallyOut:

                                // determine if needs to create own AA. (non-static and awake)
                                var warrantsBodyActiveArea = body.BodyType != BodyType.Static; //&& body.Awake; //body.AngularVelocity != 0 || body.LinearVelocity.Length() > 0;

                                if (warrantsBodyActiveArea)
                                {

                                    // determine if has own AA
                                    var bodyActiveArea = this.GetBodyActiveArea( body );

                                    if (bodyActiveArea == null)
                                    {
                                        // create body AA
                                        this.ActiveAreas.Add(new BodyActiveArea(body));
                                    } else {
                                        if (activeArea is IndependentActiveArea)
                                        {
                                            // renew the expiration timer on this AA
                                            (bodyActiveArea as BodyActiveArea).RenewExpiration();
                                        }
                                        else
                                        {
                                            // just ensure expiration isn't any shorter than this AA
                                            (bodyActiveArea as BodyActiveArea).EnsureExpirationNoLessThan(activeArea as BodyActiveArea);
                                        }
                                    } 
                                }

                                break;
                        }

                        if (areaBody.PositionStatus == AreaBodyStatus.TotallyOut)
                        {
                            this.RemoveAreaBody(activeArea, areaBody);
                        }
                    }


                }
            }
        }

        private BodyActiveArea GetBodyActiveArea(Body body)
        {
            return this.ActiveAreas.FirstOrDefault(aa => aa.AreaType == ActiveAreaType.BodyTracking && (aa as BodyActiveArea).TrackedBody == body) as BodyActiveArea;
        }

        /// <summary>
        /// Adds colliding bodies to active areas and updates position statuses for all bodies within the active area.
        /// </summary>
        private void UpdateActiveAreaBodies()
        {
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

                    //if (activeArea.AreaType == ActiveAreaType.BodyTracking && (activeArea as BodyActiveArea).IsExpired)
                    //{
                    //    // because the active area is expired, we consider all bodies totlly outside of it, as it is about to disappear.
                    //    areaBody.PositionStatus = AreaBodyStatus.Invalid; // we also set this to invalid, so we're sure to hit the "status changed" event.
                    //    areaBody.PositionStatus = AreaBodyStatus.TotallyOut;
                    //}
                    //else
                    //{
                    // determine whether this body is still touching the AA's AABB
                    var isTouchingActiveAreaAABB = bodiesInActiveArea.Contains(body);

                    if (!isTouchingActiveAreaAABB)
                    {
                        // it's not touching the AABB at all, so it's totally out.
                        areaBody.PositionStatus = AreaBodyStatus.TotallyOut;
                    }
                    else
                    {
                        // get body hibernation AABB
                        //var bodyTransform = body.GetTransform();
                        AABB bodyActiveAreaAabb = BaseActiveArea.CalculateBodyAABB(body);

                        // at this point, we know it's touching. so it's just a matter of whether it's totally inside or partially inside.
                        // contains() returns 'true' only if the AABB is entirely within
                        var isTotallyWithinActiveArea = activeArea.AABB.Contains(ref bodyActiveAreaAabb);

                        if (isTotallyWithinActiveArea)
                        {
                            // it's totally inside the ActiveArea AABB.
                            areaBody.PositionStatus = AreaBodyStatus.TotallyIn;
                        }
                        else
                        {
                            // at this point, we know it must be partially within.
                            areaBody.PositionStatus = AreaBodyStatus.PartiallyIn;
                        }
                    }
                    //}
                }
            }
        }

        private void WakeBodiesInActiveAreas()
        {
            // process all active areas
            foreach (var activeArea in this.ActiveAreas)
            {
                // get all hibernated bodies in its aabb
                var hibernatedBodiesInActiveArea = this.HibernatedWorld.FindBodiesInAABB(ref activeArea.AABB);

                // wake them
                foreach (var hibernatedBody in hibernatedBodiesInActiveArea)
                {
                    this.WakeBody(hibernatedBody);
                }

                // NOTE: in this case, we don't actually store the bodies in the ActiveArea. anything which 
                //       collides is instantly woken, and there's no need to store a history.
            }
        }

        private void RemoveExpiredActiveAreas()
        {
            for (var i = this.ActiveAreas.Count - 1; i >= 0; i--)
            {
                var activeArea = this.ActiveAreas[i];

                if (activeArea.IsExpired)
                {
                    this.ProcessExpiredActiveArea(activeArea);
                }
            }
        }

        private void ProcessExpiredActiveArea(BaseActiveArea activeArea)
        {
            // if this is a body tracking AA, only remove if not within any other AA
            if (activeArea.AreaType == ActiveAreaType.BodyTracking)
            {
                var bodyActiveArea = activeArea as BodyActiveArea;

                var isAnotherActiveAreaContainingBody = this.ActiveAreas.Any(aa =>
                    aa != activeArea // ...a different active area
                    && aa.IsExpired == false // ...isn't also an expired AA
                    && aa.Bodies.Select(aab => aab.Body).Contains(bodyActiveArea.TrackedBody)); // contains this body active area's tracked body

                if (isAnotherActiveAreaContainingBody)
                {
                    // renew the expiration, as it's clear it's still kicking around someone who cares about it.
                    // NOTE: if it's entirely within another AA then this body AA will be removed elsewhere. this condition really just ensures "partially in"
                    //bodyActiveArea.Renew();

                    // abort further expiration processing for this active area
                    return;
                }

                // TODO:
                // next... check if all bodies connected by joints are also expired...
                // if not... abort
                // bug... if joint is expired... but can't be removed due to above logic... would be hibernated and immediately awoken.
                // also... would have to recursively check all of that joined body's joined bodies... hmm.
                // add an IsAbleToBeRemoved which is set to true if no other AA are overlapping. one loop sets this.
                // add a second loop to compare both ISExpired and IsAbleToBeRemoved to see if removal may proceed.
            }

            // if this AA has any bodies we still consider "active" then we don't remove it.
            // NOTE: this fixes a bug where bodies being removed causes other bodies to shift incorrectly.
            //if (activeArea.Bodies.Select(ab => ab.Body)
            //    // we won't remove this AA if it contains any bodies which..
            //    .Any(b =>
            //    b.ContactList != null // is currently contacting another body
            //    //&& !b.Awake // and isn't asleep, i.e. if it's asleep we're okay removing it, even if it has contacts.
            //    //&& b.BodyType != BodyType.Static // and isn't static, i.e. if it's static we're okay removing it, even if it has contacts.
            //    ))
            //{
            //    // abort further expiration processing for this active area
            //    continue;
            //}

            // get all bodies
            var dynamicContactingBodies = activeArea.Bodies.Select(ab => ab.Body);

            // discard static bodies
            dynamicContactingBodies = dynamicContactingBodies.Where(b => b.BodyType != BodyType.Static);

            // discard bodies which are asleep
            //activeBodies = activeBodies.Where(b => b.Awake);

            // discard bodies which aren't contacting anything
            dynamicContactingBodies = dynamicContactingBodies.Where(b => b.HasContacts);

            if (dynamicContactingBodies.Any())
            {
                //(activeArea as BodyActiveArea).RenewExpiration(0.25f);
                //return;

                //if (activeArea is BodyActiveArea)
                //{
                //    // clear additional AABBs.
                //    (activeArea as BodyActiveArea).AdditionalAABBs.Clear();
                //}

                // for each body with contacts, check if all bodies touching are in this AA. If so, we can still expire this AA, otherwise we'll grow the size of the AA
                // in an attempt to scoop up those other bodies. the trick is that they all need to expire at the same time.
                var activeAreaBodies = activeArea.Bodies.Select(ab => ab.Body);
                foreach (var dynamicContactingBody in dynamicContactingBodies)
                {
                    bool isMissingContactingBodies = false;
                    ContactEdge ce = dynamicContactingBody.ContactList;
                    while (ce != null)
                    {
                        ContactEdge ce0 = ce;
                        ce = ce.Next;

                        var contactBodyA = ce0.Contact.FixtureA.Body;
                        var contactBodyB = ce0.Contact.FixtureB.Body;

                        // get the "other" body this one is touching
                        var otherBody = (contactBodyA == dynamicContactingBody) ? contactBodyB : contactBodyA;
                        
                        // if it's a static body, then we skip it.
                        if( otherBody.BodyType == BodyType.Static )
                        {
                            continue;
                        }

                        if (!activeAreaBodies.Contains(otherBody))
                        {
                            //if (activeArea is BodyActiveArea)
                            //{
                            //    // we increase the size of this AA in an attempt to swallow up all other nearby contacting bodies
                            //    //(activeArea as BodyActiveArea).BodyAABBMargin += Settings.BodyActiveAreaMargin;

                            //    const float AABB_MARGIN_MULTIPLIER = 1.1f;
                            //    var contactingBodyAAbb = new AABB();

                            //    // determine if has own AA
                            //    var bodyActiveArea = this.GetBodyActiveArea(otherBody);
                            //    if(bodyActiveArea != null)
                            //    {
                            //        // Add the BodyActiveArea's AABB
                            //        contactingBodyAAbb = bodyActiveArea.AABB * AABB_MARGIN_MULTIPLIER;
                            //    } else {
                            //        // Derive the body's AABB
                            //        contactingBodyAAbb = BaseActiveArea.CalculateBodyAABB(otherBody, Settings.BodyActiveAreaMargin * AABB_MARGIN_MULTIPLIER);
                            //    }

                            //    (activeArea as BodyActiveArea).AdditionalAABBs.Add(contactingBodyAAbb);
                                
                            //    //(activeArea as BodyActiveArea).RenewExpiration(1.0f);
                            //}
                            
                            isMissingContactingBodies = true;
                        }
                    }

                    if (isMissingContactingBodies)
                    {
                        // abort further expiration processing.
                        return;
                    }
                }

                // if we're reached this point then all contacting bodies are in this AA so it's safe to remove them all in one swoop and we'll continue processing expiration.
            }

            // remove all area bodies from this active area...
            for (var bodyIndex = activeArea.Bodies.Count - 1; bodyIndex >= 0; bodyIndex--)
            {
                var areaBody = activeArea.Bodies[bodyIndex];

                this.RemoveAreaBody(activeArea, areaBody);
            }

            // remove this active area...
            this.ActiveAreas.Remove(activeArea);
        }

        private void UpdateActiveAreas()
        {
            // process all active areas
            for (var i = this.ActiveAreas.Count - 1; i >= 0; i--)
            {
                var activeArea = this.ActiveAreas[i];

                // update its bounding box
                activeArea.Update();
            }
        }

        private void RemoveAreaBody(BaseActiveArea activeArea, AreaBody areaBody)
        {
            // remove it from this AA.
            activeArea.Bodies.Remove(areaBody);

            // if it's not in any other AA at this point, hibernate it.
            var activeAreasContainingBody = this.ActiveAreas.Where(aa => 
                aa != activeArea // is a different active area
                && aa.Bodies.Select(aab => aab.Body).Contains(areaBody.Body)); // contains the body
            
            if (!activeAreasContainingBody.Any())
            {
                // no other active area has this body in it, so go ahead and hibernate the body
                this.BodiesToHibernate.Add(areaBody.Body);
            }
        }

        private void HibernateBody(Body body)
        {
            // remove from active world 
            // NOTE: we don't call the World.Remove() method, as that actually destroys the body, fixtures, etc. 
            this.ActiveWorld.Remove(body);

            // add to hibernated world
            this.HibernatedWorld.Add(body);
        }

        internal void ReviveAll()
        {
            // wake them
            for (var i = this.HibernatedWorld.BodyList.Count - 1; i >= 0; i--)
            {
                var hibernatedBody = this.HibernatedWorld.BodyList[i];

                this.WakeBody(hibernatedBody);
            }
        }

        private void WakeBody( Body hibernatedBody )
        {
            // remove from the hibernated world
            this.HibernatedWorld.Remove(hibernatedBody);

            // add to active world
            this.ActiveWorld.Add(hibernatedBody);
        }
    }
}
