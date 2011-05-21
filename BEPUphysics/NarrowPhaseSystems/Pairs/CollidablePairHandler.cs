﻿using BEPUphysics.BroadPhaseSystems;
using BEPUphysics.Collidables;
using BEPUphysics.NarrowPhaseSystems.Factories;
using BEPUphysics.CollisionRuleManagement;
using BEPUphysics.Entities;
using BEPUphysics.Constraints.Collision;
using BEPUphysics.CollisionTests.Manifolds;
using BEPUphysics.CollisionTests;
using System;
using BEPUphysics.Constraints.SolverGroups;
using BEPUphysics.Materials;

namespace BEPUphysics.NarrowPhaseSystems.Pairs
{
    ///<summary>
    /// Superclass of pairs between collidables that generate contact points.
    ///</summary>
    public abstract class CollidablePairHandler : INarrowPhasePair
    {
        protected abstract Collidable CollidableA { get; }
        protected abstract Collidable CollidableB { get; }
        //Entities could be null!
        protected abstract Entity EntityA { get; }
        protected abstract Entity EntityB { get; }

        public abstract int ContactCount { get; }

        protected internal int previousContactCount;


        protected CollidablePairHandler()
        {
            Contacts = new ContactCollection(this);
        }

        ///<summary>
        /// Updates the pair handler.
        ///</summary>
        ///<param name="dt">Timestep duration.</param>
        public abstract void UpdateCollision(float dt);


        protected internal float timeOfImpact = 1;
        ///<summary>
        /// Gets the last computed time of impact of the pair handler.
        /// This is only computed when one of the members is a continuously
        /// updated object.
        ///</summary>
        public float TimeOfImpact
        {
            get
            {
                return timeOfImpact;
            }
        }

        ///<summary>
        /// Updates the time of impact for the pair.
        ///</summary>
        ///<param name="requester">Collidable requesting the update.</param>
        ///<param name="dt">Timestep duration.</param>
        public abstract void UpdateTimeOfImpact(Collidable requester, float dt);

        bool INarrowPhasePair.NeedsUpdate
        {
            get;
            set;
        }

        internal BroadPhaseOverlap broadPhaseOverlap;
        ///<summary>
        /// Gets the broad phase overlap associated with this pair handler.
        ///</summary>
        public BroadPhaseOverlap BroadPhaseOverlap
        {
            get
            {
                return broadPhaseOverlap;
            }
        }

        ///<summary>
        /// Gets or sets the collision rule governing this pair handler.
        ///</summary>
        public CollisionRule CollisionRule
        {
            get
            {
                return broadPhaseOverlap.collisionRule;
            }
            set
            {
                broadPhaseOverlap.collisionRule = value;
            }
        }

        BroadPhaseOverlap INarrowPhasePair.BroadPhaseOverlap
        {
            get
            {
                return broadPhaseOverlap;
            }
            set
            {
                broadPhaseOverlap = value;
                Initialize(value.entryA, value.entryB);
            }
        }

        NarrowPhasePairFactory INarrowPhasePair.Factory
        {
            get;
            set;
        }

        NarrowPhase narrowPhase;
        ///<summary>
        /// Gets the narrow phase that owns this pair handler.
        ///</summary>
        public NarrowPhase NarrowPhase
        {
            get
            {
                return narrowPhase;
            }
            set
            {
                narrowPhase = value;
            }
        }

        protected bool suppressEvents;
        ///<summary>
        /// Gets or sets whether or not to suppress events from this pair handler.
        ///</summary>
        public bool SuppressEvents
        {
            get
            {
                return suppressEvents;
            }
            set
            {
                suppressEvents = value;
            }
        }

        ///<summary>
        /// Gets or sets the parent of this pair handler.
        /// Pairs with parents report to their parents various
        /// changes in state.  This is mainly used to support
        /// hierarchies of pairs for compound collisions.
        ///</summary>
        public IPairHandlerParent Parent { get; set; }



        ///<summary>
        /// Initializes the pair handler.
        ///</summary>
        ///<param name="entryA">First entry in the pair.</param>
        ///<param name="entryB">Second entry in the pair.</param>
        public virtual void Initialize(BroadPhaseEntry entryA, BroadPhaseEntry entryB)
        {
            //Child initialization is responsible for setting up the entries.
            //Child initialization is responsible for setting up the manifold.
            //Child initialization is responsible for setting up the constraint.


            UpdateMaterialProperties();

            if (!suppressEvents)
            {
                CollidableA.EventTriggerer.OnPairCreated(CollidableB, this);
                CollidableB.EventTriggerer.OnPairCreated(CollidableA, this);
            }
        }

        ///<summary>
        /// Called when the pair handler is added to the narrow phase.
        ///</summary>
        public virtual void OnAddedToNarrowPhase()
        {
            CollidableA.pairs.Add(this);
            CollidableB.pairs.Add(this);
        }

        protected virtual void OnContactAdded(Contact contact)
        {
            //Children manage the addition of the contact to the constraint, if any.
            if (!suppressEvents)
            {
                CollidableA.EventTriggerer.OnContactCreated(CollidableB, this, contact);
                CollidableB.EventTriggerer.OnContactCreated(CollidableA, this, contact);
            }
            if (Parent != null)
                Parent.OnContactAdded(contact);

        }

        protected virtual void OnContactRemoved(Contact contact)
        {
            //Children manage the removal of the contact from the constraint, if any.
            if (!suppressEvents)
            {
                CollidableA.EventTriggerer.OnContactRemoved(CollidableB, this, contact);
                CollidableB.EventTriggerer.OnContactRemoved(CollidableA, this, contact);
            }
            if (Parent != null)
                Parent.OnContactRemoved(contact);

        }

        ///<summary>
        /// Cleans up the pair handler.
        ///</summary>
        public virtual void CleanUp()
        {

            //Child types remove contacts from the pair handler and call OnContactRemoved.
            //Child types manage the removal of the constraint from the space, if necessary.


            //If the contact manifold had any contacts in it on cleanup, then we still need to fire the 'ending' event.
            if (previousContactCount > 0 && !suppressEvents)
            {
                CollidableA.EventTriggerer.OnCollisionEnded(CollidableB, this);
                CollidableB.EventTriggerer.OnCollisionEnded(CollidableA, this);
            }

            //Remove this pair from each collidable.  This can be done safely because the CleanUp is called sequentially.
            CollidableA.pairs.Remove(this);
            CollidableB.pairs.Remove(this);

            //Notify the colliders that the pair went away.
            if (!suppressEvents)
            {
                CollidableA.EventTriggerer.OnPairRemoved(CollidableB);
                CollidableB.EventTriggerer.OnPairRemoved(CollidableA);
            }


            broadPhaseOverlap = new BroadPhaseOverlap();
            (this as INarrowPhasePair).NeedsUpdate = false;
            (this as INarrowPhasePair).NarrowPhase = null;
            suppressEvents = false;
            timeOfImpact = 1;
            Parent = null;

            previousContactCount = 0;

            //Child cleanup is responsible for cleaning up direct references to the involved collidables.
            //Child cleanup is responsible for cleaning up contact manifolds.
        }

        ///<summary>
        /// Forces an update of the pair's material properties.
        /// <param name="materialA">First material to use.</param>
        /// <param name="materialB">Second material to use.</param>
        ///</summary>
        public abstract void UpdateMaterialProperties(Material materialA, Material materialB);

        ///<summary>
        /// Forces an update of the pair's material properties.
        /// Uses default choices (such as the owning entities' materials).
        ///</summary>
        public void UpdateMaterialProperties()
        {
            UpdateMaterialProperties(null, null);
        }


        internal abstract void GetContactInformation(int index, out ContactInformation info);


        ///<summary>
        /// Gets a list of the contacts in the pair and their associated constraint information.
        ///</summary>
        public ContactCollection Contacts { get; private set; }
    }
}
