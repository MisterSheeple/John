﻿// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
using System;
using OpenTK;

namespace ClassicalSharp.Entities.Mobs {
	
	public class MobEntity : Entity {
		
		LocalInterpComponent interp;
		CollisionsComponent collisions;
		PhysicsComponent physics;
		static HacksComponent hacks = new HacksComponent(null, null);
		
		public MobEntity(Game game, string model) : base(game) {
			StepSize = 0.5f;
			SetModel(model);
			interp = new LocalInterpComponent(game, this);
			
			collisions = new CollisionsComponent(game, this);
			physics = new PhysicsComponent(game, this);
			physics.hacks = hacks;
			physics.collisions = collisions;
		}
		
		public override void Despawn() { }
		public override void RenderName() { }
		
		// TODO: this is just so the entities do something, remove later
		static Random rand = new Random();
		public override void Tick(double delta) {
			if (game.World.IsNotLoaded) return;
			float xMoving = 0.98f, zMoving = 0.98f;
			interp.AdvanceState();
			bool wasOnGround = onGround;
			physics.UpdateVelocityState(xMoving, zMoving);
			physics.PhysicsTick(xMoving, zMoving);			
			interp.next.Pos = Position; Position = interp.prev.Pos;
			anim.UpdateAnimState(interp.prev.Pos, interp.next.Pos, delta);			
		}
		
		public override void SetLocation(LocationUpdate update, bool interpolate) {
			interp.SetLocation(update, interpolate);
		}
		
		public override void RenderModel(double deltaTime, float t) {
			Position = Vector3.Lerp(interp.prev.Pos, interp.next.Pos, t);
			interp.LerpAngles(t);
			anim.GetCurrentAnimState(t);
			Model.Render(this);
		}
	}
}
