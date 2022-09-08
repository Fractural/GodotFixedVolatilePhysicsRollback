﻿/*
 *  VolatilePhysics - A 2D Physics Library for Networked Games
 *  Copyright (c) 2015-2016 - Alexander Shoulson - http://ashoulson.com
 *
 *  This software is provided 'as-is', without any express or implied
 *  warranty. In no event will the authors be held liable for any damages
 *  arising from the use of this software.
 *  Permission is granted to anyone to use this software for any purpose,
 *  including commercial applications, and to alter it and redistribute it
 *  freely, subject to the following restrictions:
 *  
 *  1. The origin of this software must not be misrepresented; you must not
 *     claim that you wrote the original software. If you use this software
 *     in a product, an acknowledgment in the product documentation would be
 *     appreciated but is not required.
 *  2. Altered source versions must be plainly marked as such, and must not be
 *     misrepresented as being the original software.
 *  3. This notice may not be removed or altered from any source distribution.
*/

using FixMath.NET;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

#if UNITY
using UnityEngine;
#endif

namespace Volatile
{
    public partial class VoltWorld
    {
        #region Helper Filters
        public static bool FilterNone(VoltBody body)
        {
            return true;
        }

        public static bool FilterAll(VoltBody body)
        {
            return false;
        }

        public static bool FilterStatic(VoltBody body)
        {
            return (body.IsStatic == false);
        }

        public static bool FilterDynamic(VoltBody body)
        {
            return body.IsStatic;
        }

        public static VoltBodyFilter FilterExcept(VoltBody exception)
        {
            return ((body) => body != exception);
        }
        #endregion

        /// <summary>
        /// Fixed update delta time for body integration. 
        /// Defaults to Config.DEFAULT_DELTA_TIME.
        /// </summary>
        public Fix64 DeltaTime { get; set; }

        /// <summary>
        /// Number of iterations when updating the world.
        /// Defaults to Config.DEFAULT_ITERATION_COUNT.
        /// </summary>
        public int IterationCount { get; set; }

        /// <summary>
        /// How many frames of history this world is recording.
        /// </summary>
        public int HistoryLength { get; private set; }

        public IEnumerable<VoltBody> Bodies
        {
            get
            {
                for (int i = 0; i < this.bodies.Count; i++)
                    yield return this.bodies[i];
            }
        }

        internal Fix64 Elasticity { get; private set; }
        internal Fix64 Damping { get; private set; }

        private CheapList<VoltBody> bodies;
        private List<Manifold> manifolds;

        /// <summary>
        /// Includes dynamic, kinematic, and trigger bodies. It's fast
        /// to add and remove bodies from this phase.
        /// </summary>
        private IBroadPhase dynamicBroadphase;
        /// <summary>
        /// Includes only static bodies. It's costly to add and remove 
        /// bodies from this phase because it regenerates the tree.
        /// </summary>
        private IBroadPhase staticBroadphase;

        private VoltBuffer<VoltBody> reusableBuffer;
        private VoltBuffer<VoltBody> reusableOutput;

        // Each World instance should own its own object pools, in case
        // you want to run multiple World instances simultaneously.
        private IVoltPool<VoltBody> bodyPool;
        private IVoltPool<VoltShape> circlePool;
        private IVoltPool<VoltShape> polygonPool;

        private IVoltPool<Contact> contactPool;
        private IVoltPool<Manifold> manifoldPool;
        private IVoltPool<HistoryBuffer> historyPool;

        public VoltWorld(int historyLength, Fix64 damping)
        {
            this.HistoryLength = historyLength;
            this.Damping = damping;

            this.IterationCount = VoltConfig.DEFAULT_ITERATION_COUNT;
            this.DeltaTime = VoltConfig.DEFAULT_DELTA_TIME;

            this.bodies = new CheapList<VoltBody>();
            this.manifolds = new List<Manifold>();

            this.dynamicBroadphase = new NaiveBroadphase();
            this.staticBroadphase = new TreeBroadphase();

            this.reusableBuffer = new VoltBuffer<VoltBody>();
            this.reusableOutput = new VoltBuffer<VoltBody>();

            this.bodyPool = new VoltPool<VoltBody>();
            this.circlePool = new VoltPool<VoltShape, VoltCircle>();
            this.polygonPool = new VoltPool<VoltShape, VoltPolygon>();

            this.contactPool = new VoltPool<Contact>();
            this.manifoldPool = new VoltPool<Manifold>();
            this.historyPool = new VoltPool<HistoryBuffer>();
        }

        public VoltWorld(int historyLength = 0) : this(historyLength, VoltConfig.DEFAULT_DAMPING)
        {

        }

        /// <summary>
        /// Creates a new polygon shape from world-space vertices.
        /// </summary>
        public VoltPolygon CreatePolygonWorldSpace(
          VoltVector2[] worldVertices, Fix64 density, Fix64 friction, Fix64 restitution)
        {
            VoltPolygon polygon = (VoltPolygon)this.polygonPool.Allocate();
            polygon.InitializeFromWorldVertices(
              worldVertices,
              density,
              friction,
              restitution);
            return polygon;
        }

        public VoltPolygon CreatePolygonWorldSpace(VoltVector2[] worldVertices, Fix64 density)
        {
            return CreatePolygonWorldSpace(worldVertices, density, VoltConfig.DEFAULT_FRICTION, VoltConfig.DEFAULT_RESTITUTION);
        }

        public VoltPolygon CreatePolygonWorldSpace(VoltVector2[] worldVertices)
        {
            return CreatePolygonWorldSpace(worldVertices, VoltConfig.DEFAULT_DENSITY, VoltConfig.DEFAULT_FRICTION, VoltConfig.DEFAULT_RESTITUTION);
        }

        /// <summary>
        /// Creates a new polygon shape from body-space vertices.
        /// </summary>
        public VoltPolygon CreatePolygonBodySpace(
          VoltVector2[] bodyVertices, Fix64 density, Fix64 friction, Fix64 restitution)
        {
            VoltPolygon polygon = (VoltPolygon)this.polygonPool.Allocate();
            polygon.InitializeFromBodyVertices(
              bodyVertices,
              density,
              friction,
              restitution);
            return polygon;
        }

        public VoltPolygon CreatePolygonBodySpace(VoltVector2[] bodyVertices, Fix64 density)
        {
            return CreatePolygonBodySpace(bodyVertices, density, VoltConfig.DEFAULT_FRICTION, VoltConfig.DEFAULT_RESTITUTION);
        }

        public VoltPolygon CreatePolygonBodySpace(VoltVector2[] bodyVertices)
        {
            return CreatePolygonBodySpace(bodyVertices, VoltConfig.DEFAULT_DENSITY, VoltConfig.DEFAULT_FRICTION, VoltConfig.DEFAULT_RESTITUTION);
        }

        /// <summary>
        /// Creates a new circle shape from a world-space origin.
        /// </summary>
        public VoltCircle CreateCircleWorldSpace(
          VoltVector2 worldSpaceOrigin, Fix64 radius, Fix64 density, Fix64 friction, Fix64 restitution)
        {
            VoltCircle circle = (VoltCircle)this.circlePool.Allocate();
            circle.InitializeFromWorldSpace(
              worldSpaceOrigin,
              radius,
              density,
              friction,
              restitution);
            return circle;
        }

        public VoltCircle CreateCircleWorldSpace(VoltVector2 worldSpaceOrigin, Fix64 radius, Fix64 density)
        {
            return CreateCircleWorldSpace(worldSpaceOrigin, radius, density, VoltConfig.DEFAULT_FRICTION, VoltConfig.DEFAULT_RESTITUTION);
        }

        public VoltCircle CreateCircleWorldSpace(VoltVector2 worldSpaceOrigin, Fix64 radius)
        {
            return CreateCircleWorldSpace(worldSpaceOrigin, radius, VoltConfig.DEFAULT_DENSITY, VoltConfig.DEFAULT_FRICTION, VoltConfig.DEFAULT_RESTITUTION);
        }

        /// <summary>
        /// Creates a new static body and adds it to the world.
        /// </summary>
        public VoltBody CreateStaticBody(
          VoltVector2 position,
          Fix64 radians,
          VoltShape[] shapesToAdd,
          int layer = 1,
          int mask = 1)
        {
            VoltBody body = this.bodyPool.Allocate();
            body.InitializeStatic(position, radians, shapesToAdd, layer, mask);
            this.AddBodyInternal(body);
            return body;
        }

        /// <summary>
        /// Creates a new dynamic body and adds it to the world.
        /// </summary>
        public VoltBody CreateDynamicBody(
          VoltVector2 position,
          Fix64 radians,
          VoltShape[] shapesToAdd,
          int layer = 1,
          int mask = 1)
        {
            VoltBody body = this.bodyPool.Allocate();
            body.InitializeDynamic(position, radians, shapesToAdd, layer, mask);
            this.AddBodyInternal(body);
            return body;
        }

        /// <summary>
        /// Creates a new kinematic body and adds it to the world.
        /// </summary>
        public VoltBody CreateKinematicBody(
          VoltVector2 position,
          Fix64 radians,
          VoltShape[] shapesToAdd,
          int layer = 1,
          int mask = 1)
        {
            VoltBody body = this.bodyPool.Allocate();
            body.InitializeKinematic(position, radians, shapesToAdd, layer, mask);
            this.AddBodyInternal(body);
            return body;
        }

        /// <summary>
        /// Creates a new trigger body and adds it to the world.
        /// </summary>
        public VoltBody CreateTriggerBody(
          VoltVector2 position,
          Fix64 radians,
          VoltShape[] shapesToAdd,
          int layer = 1,
          int mask = 1)
        {
            VoltBody body = this.bodyPool.Allocate();
            body.InitializeTrigger(position, radians, shapesToAdd, layer, mask);
            this.AddBodyInternal(body);
            return body;
        }

        /// <summary>
        /// Adds a body to the world. Used for reintroducing bodies that 
        /// have been removed. For new bodies, use CreateBody.
        /// </summary>
        public void AddBody(
          VoltBody body,
          VoltVector2 position,
          Fix64 radians)
        {
#if DEBUG
            VoltDebug.Assert(body.IsInitialized);
#endif
            VoltDebug.Assert(body.World == null);
            this.AddBodyInternal(body);
            body.Set(position, radians);
        }

        /// <summary>
        /// Removes a body from the world. The body will be partially reset so it
        /// can be added later. The pointer is still valid and the body can be
        /// returned to the world using AddBody.
        /// </summary>
        public void RemoveBody(VoltBody body)
        {
            VoltDebug.Assert(body.World == this);

            body.PartialReset();

            this.RemoveBodyInternal(body);
        }

        /// <summary>
        /// Removes a body from the world and deallocates it. The pointer is
        /// invalid after this point.
        /// </summary>
        public void DestroyBody(VoltBody body)
        {
            VoltDebug.Assert(body.World == this);

            body.FreeShapes();

            this.RemoveBodyInternal(body);
            this.FreeBody(body);
        }

        /// <summary>
        /// Ticks the world, updating all dynamic bodies and resolving collisions.
        /// If a frame number is provided, all dynamic bodies will store their
        /// state for that frame for later testing.
        /// </summary>
        public void Update()
        {
            for (int i = 0; i < this.bodies.Count; i++)
            {
                VoltBody body = this.bodies[i];
                if (!body.IsStatic)
                {
                    body.Update();
                    this.dynamicBroadphase.UpdateBody(body);
                }
            }

            this.BroadPhase();
            this.UpdateCollision();
            this.FreeManifolds();
        }

        /// <summary>
        /// Updates a single body, resolving only collisions with that body.
        /// If a frame number is provided, all dynamic bodies will store their
        /// state for that frame for later testing.
        /// 
        /// Note: This function is best used with dynamic collisions disabled, 
        /// otherwise you might get symmetric duplicates on collisions.
        /// </summary>
        public void Update(VoltBody body, bool collideDynamic)
            => Update(body, collideDynamic, VoltCollisionFilters.DefaultWorldCollisionFilter);
        public void Update(VoltBody body, bool collideDynamic, VoltCollisionFilter filter)
        {
            if (body.IsStatic)
            {
                VoltDebug.LogWarning("Updating static body, doing nothing");
                return;
            }

            body.Update();
            this.dynamicBroadphase.UpdateBody(body);
            this.BroadPhase(body, collideDynamic, filter);

            this.UpdateCollision();
            this.FreeManifolds();
        }

        /// <summary>
        /// Finds all bodies containing a given point.
        /// 
        /// Subsequent calls to other Query functions (Point, Circle, Bounds) will
        /// invalidate the resulting enumeration from this function.
        /// </summary>
        public VoltBuffer<VoltBody> QueryPoint(
          VoltVector2 point,
          VoltBodyFilter filter = null,
          int ticksBehind = 0)
        {
            if (ticksBehind < 0)
                throw new ArgumentOutOfRangeException("ticksBehind");

            this.reusableBuffer.Clear();
            this.staticBroadphase.QueryPoint(point, this.reusableBuffer);
            this.dynamicBroadphase.QueryPoint(point, this.reusableBuffer);

            this.reusableOutput.Clear();
            for (int i = 0; i < this.reusableBuffer.Count; i++)
            {
                VoltBody body = this.reusableBuffer[i];
                if (VoltBody.Filter(body, filter))
                    if (body.QueryPoint(point, ticksBehind))
                        this.reusableOutput.Add(body);
            }
            return this.reusableOutput;
        }

        /// <summary>
        /// Finds all bodies intersecting with a given circle.
        /// 
        /// Subsequent calls to other Query functions (Point, Circle, Bounds) will
        /// invalidate the resulting enumeration from this function.
        /// </summary>
        public VoltBuffer<VoltBody> QueryCircle(
          VoltVector2 origin,
          Fix64 radius,
          VoltBodyFilter filter = null,
          int ticksBehind = 0)
        {
            if (ticksBehind < 0)
                throw new ArgumentOutOfRangeException("ticksBehind");

            this.reusableBuffer.Clear();
            this.staticBroadphase.QueryCircle(origin, radius, this.reusableBuffer);
            this.dynamicBroadphase.QueryCircle(origin, radius, this.reusableBuffer);

            this.reusableOutput.Clear();
            for (int i = 0; i < this.reusableBuffer.Count; i++)
            {
                VoltBody body = this.reusableBuffer[i];
                if (VoltBody.Filter(body, filter))
                    if (body.QueryCircle(origin, radius, ticksBehind))
                        this.reusableOutput.Add(body);
            }

            return this.reusableOutput;
        }

        /// <summary>
        /// Performs a raycast on all world bodies.
        /// </summary>
        public bool RayCast(
          ref VoltRayCast ray,
          ref VoltRayResult result,
          VoltBodyFilter filter = null,
          int ticksBehind = 0)
        {
            if (ticksBehind < 0)
                throw new ArgumentOutOfRangeException("ticksBehind");

            this.reusableBuffer.Clear();
            this.staticBroadphase.RayCast(ref ray, this.reusableBuffer);
            this.dynamicBroadphase.RayCast(ref ray, this.reusableBuffer);

            for (int i = 0; i < this.reusableBuffer.Count; i++)
            {
                VoltBody body = this.reusableBuffer[i];
                if (VoltBody.Filter(body, filter))
                {
                    body.RayCast(ref ray, ref result, ticksBehind);
                    if (result.IsContained)
                        return true;
                }
            }

            return result.IsValid;
        }

        /// <summary>
        /// Performs a circle cast on all world bodies.
        /// </summary>
        public bool CircleCast(
          ref VoltRayCast ray,
          Fix64 radius,
          ref VoltRayResult result,
          VoltBodyFilter filter = null,
          int ticksBehind = 0)
        {
            if (ticksBehind < 0)
                throw new ArgumentOutOfRangeException("ticksBehind");

            this.reusableBuffer.Clear();
            this.staticBroadphase.CircleCast(ref ray, radius, this.reusableBuffer);
            this.dynamicBroadphase.CircleCast(ref ray, radius, this.reusableBuffer);

            for (int i = 0; i < this.reusableBuffer.Count; i++)
            {
                VoltBody body = this.reusableBuffer[i];
                if (VoltBody.Filter(body, filter))
                {
                    body.CircleCast(ref ray, radius, ref result, ticksBehind);
                    if (result.IsContained)
                        return true;
                }
            }
            return result.IsValid;
        }

        #region Internals
        private void AddBodyInternal(VoltBody body)
        {
            this.bodies.Add(body);
            if (body.IsStatic)
                this.staticBroadphase.AddBody(body);
            else
                this.dynamicBroadphase.AddBody(body);

            body.AssignWorld(this);
            if ((this.HistoryLength > 0) && (body.IsStatic == false))
                body.AssignHistory(this.AllocateHistory());
        }

        private void RemoveBodyInternal(VoltBody body)
        {
            this.bodies.Remove(body);
            if (body.IsStatic)
                this.staticBroadphase.RemoveBody(body);
            else
                this.dynamicBroadphase.RemoveBody(body);

            body.FreeHistory();
            body.AssignWorld(null);
        }

        /// <summary>
        /// Returns whether or not this body is currently colliding
        /// </summary>
        /// <param name="query"></param>
        /// <param name="collideDynamic"></param>
        /// <returns></returns>
        public bool QueryColliding(VoltBody query, bool collideDynamic = false)
           => QueryColliding(query, collideDynamic, VoltCollisionFilters.DefaultWorldCollisionFilter);

        /// <summary>
        /// Returns whether or not this body is currently colliding
        /// </summary>
        /// <param name="query"></param>
        /// <param name="collideDynamic"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public bool QueryColliding(VoltBody query, bool collideDynamic, VoltCollisionFilter filter)
        {
            BroadPhase(query, collideDynamic, filter);

            var colliding = this.manifolds.Count != 0;
            FreeManifolds();
            return colliding;
        }

        /// <summary>
        /// Finds collisions for a body.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="collideDynamic"></param>
        /// <returns></returns>
        public VoltBodyCollisionResult QueryCollisions(VoltBody body, bool collideDynamic)
            => QueryCollisions(body, collideDynamic, VoltCollisionFilters.DefaultWorldCollisionFilter);

        /// <summary>
        /// Finds collisions for a body.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="collideDynamic"></param>
        /// <param name="filter">Filter for checking whether two bodies can collide</param>
        /// <returns></returns>
        public VoltBodyCollisionResult QueryCollisions(VoltBody query, bool collideDynamic, VoltCollisionFilter filter)
        {
            BroadPhase(query, collideDynamic, filter);

            if (this.manifolds.Count == 0)
                return new VoltBodyCollisionResult();

            var collisions = new VoltShapeCollision[this.manifolds.Count];
            for (int j = 0; j < this.manifolds.Count; j++)
            {
                // ShapeB is the collision
                var collisionContacts = new VoltCollisionContact[manifolds[j].UsedContacts];
                for (int i = 0; i < manifolds[j].UsedContacts; i++)
                    collisionContacts[i] = new VoltCollisionContact(manifolds[j].Contacts[i].position, manifolds[j].Contacts[i].normal, manifolds[j].Contacts[i].penetration);
                collisions[j] = new VoltShapeCollision(manifolds[j].ShapeA, manifolds[j].ShapeB, collisionContacts.ToArray());
            }
            FreeManifolds();
            return new VoltBodyCollisionResult(query, collisions);
        }

        /// <summary>
        /// Identifies collisions for all bodies, ignoring symmetrical duplicates.
        /// </summary>
        private void BroadPhase()
        {
            for (int i = 0; i < this.bodies.Count; i++)
            {
                VoltBody query = this.bodies[i];
                if (query.IsStatic)
                    continue;

                this.reusableBuffer.Clear();
                this.staticBroadphase.QueryOverlap(query.AABB, this.reusableBuffer);

                // HACK: Don't use dynamic broadphase for global updates for this.
                // It's faster if we do it manually because we can triangularize.
                for (int j = i + 1; j < this.bodies.Count; j++)
                    if (this.bodies[j].IsStatic == false)
                        this.reusableBuffer.Add(this.bodies[j]);

                this.TestBuffer(query);
            }
        }

        /// <summary>
        /// Identifies collisions for a single body. Does not keep track of 
        /// symmetrical duplicates (they could be counted twice).
        /// </summary>
        private void BroadPhase(VoltBody query, bool collideDynamic, VoltCollisionFilter filter)
        {
            VoltDebug.Assert(query.IsStatic == false);

            this.reusableBuffer.Clear();
            this.staticBroadphase.QueryOverlap(query.AABB, this.reusableBuffer);
            if (collideDynamic)
                this.dynamicBroadphase.QueryOverlap(query.AABB, this.reusableBuffer);

            this.TestBuffer(query, filter);
        }

        private void TestBuffer(VoltBody query)
        {
            TestBuffer(query, VoltCollisionFilters.DefaultWorldCollisionFilter);
        }

        /// <summary>
        /// Tests all bodies queued in the <see cref="reusableBuffer"/>.
        /// </summary>
        /// <param name="query">Body to test against</param>
        private void TestBuffer(VoltBody query, VoltCollisionFilter filter)
        {
            for (int i = 0; i < this.reusableBuffer.Count; i++)
            {
                VoltBody test = this.reusableBuffer[i];
                bool canCollide =
                  filter(query, test) &&
                  query.AABB.Intersect(test.AABB);

                if (canCollide)
                    for (int i_q = 0; i_q < query.shapeCount; i_q++)
                        for (int j_t = 0; j_t < test.shapeCount; j_t++)
                            this.NarrowPhase(query.shapes[i_q], test.shapes[j_t]);
            }
        }

        /// <summary>
        /// Creates a manifold for two shapes if they collide.
        /// </summary>
        private void NarrowPhase(
          VoltShape sa,
          VoltShape sb)
        {
            if (sa.AABB.Intersect(sb.AABB) == false)
                return;

            VoltShape.OrderShapes(ref sa, ref sb);
            Manifold manifold = Collision.Dispatch(this, sa, sb);
            if (manifold != null)
                this.manifolds.Add(manifold);
        }

        private void UpdateCollision()
        {
            for (int i = 0; i < this.manifolds.Count; i++)
                this.manifolds[i].PreStep();

            this.Elasticity = Fix64.One;
            for (int j = 0; j < this.IterationCount * 1 / 3; j++)
                for (int i = 0; i < this.manifolds.Count; i++)
                    this.manifolds[i].Solve();

            for (int i = 0; i < this.manifolds.Count; i++)
                this.manifolds[i].SolveCached();

            this.Elasticity = Fix64.Zero;
            for (int j = 0; j < this.IterationCount * 2 / 3; j++)
                for (int i = 0; i < this.manifolds.Count; i++)
                    this.manifolds[i].Solve();

            // Add collision events for each manifold in PostStep
            for (int i = 0; i < this.manifolds.Count; i++)
                this.manifolds[i].PostStep();

            // Process the queued collision events separately
            // (avoids duplicate events for the same body)
            for (int i = 0; i < this.bodies.Count; i++)
                this.bodies[i].ProcessCollisionEvents();
        }

        #region Pooling
        internal Contact AllocateContact()
        {
            return this.contactPool.Allocate();
        }

        internal Manifold AllocateManifold()
        {
            return this.manifoldPool.Allocate();
        }

        private HistoryBuffer AllocateHistory()
        {
            HistoryBuffer history = this.historyPool.Allocate();
            history.Initialize(this.HistoryLength);
            return history;
        }

        private void FreeBody(VoltBody body)
        {
            this.bodyPool.Deallocate(body);
        }

        private void FreeManifolds()
        {
            for (int i = 0; i < this.manifolds.Count; i++)
                this.manifoldPool.Deallocate(this.manifolds[i]);
            this.manifolds.Clear();
        }

        internal void FreeContacts(IList<Contact> contacts)
        {
            for (int i = 0; i < contacts.Count; i++)
                this.contactPool.Deallocate(contacts[i]);
        }

        internal void FreeHistory(HistoryBuffer history)
        {
            this.historyPool.Deallocate(history);
        }

        internal void FreeShape(VoltShape shape)
        {
            switch (shape.Type)
            {
                case VoltShape.ShapeType.Circle:
                    this.circlePool.Deallocate(shape);
                    break;

                case VoltShape.ShapeType.Polygon:
                    this.polygonPool.Deallocate(shape);
                    break;

                default:
                    VoltDebug.LogError("Unknown shape for deallocation");
                    break;
            }
        }

        private VoltCircle CreateCircle()
        {
            return new VoltCircle();
        }

        private VoltPolygon CreatePolygon()
        {
            return new VoltPolygon();
        }
        #endregion
        #endregion
    }
}