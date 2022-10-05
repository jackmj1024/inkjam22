using System;
using Godot;
using Godot.Collections;

public partial class Ship : RigidBody3D {
	[Export] private float maxForce = 50f; // Maximum (full input) force that can be added per physics frame.
	[Export] private float maxBrakePower = 2f; // Value ADDED to the RigidBody's linear movement damping when braking.
	
	private bool useMouseSteering; // If true, ship rotates towards the mouse position instead of joystick direction.
	private const ushort RAY_LENGTH = 100; // Distance to project raycasts.
	private const byte CONTROLLER_INDEX = 1; // Device index of the player's controller

	private float baseDamping; // Stores the linear damping value on the RigidBody and uses it for the braking mechanic.

	// Child nodes
	private Camera3D camera;
	private Node3D gfx;
	
	#region Godot Overrides

	public override void _Ready() {
		camera = GetNode<Camera3D>("Camera");
		gfx = GetNode<Node3D>("GFX");

		baseDamping = LinearDamp;
		base._Ready();
	}

	public override void _PhysicsProcess(double _delta) {
		Steer();
		Accelerate();
		base._PhysicsProcess(_delta);
	}

	public override void _Input(InputEvent _event) {
		if(_event is InputEventMouseButton) {
			useMouseSteering = true;
		} else if(_event is InputEventJoypadButton) {
			useMouseSteering = false;
		}
		base._Input(_event);
	}
	
	#endregion
	
	#region Movement
	
	// Handle moving and braking.
	private void Accelerate() {
		Vector3 _direction = -gfx.GlobalTransform.basis.z;
		float _magnitude = maxForce * Input.GetActionStrength("accelerate");
		
		ConstantForce = _direction * _magnitude;

		LinearDamp = baseDamping + maxBrakePower * Input.GetActionStrength("brake");
	}

	private void Brake() {
		
	}

	// Handle steering with either a joystick or mouse.
	private void Steer() {
		Vector2 _direction = Vector2.Zero;
		
		if(!useMouseSteering) { // Joystick steering
			// Get the direction of the left joystick
			_direction.x = Input.GetJoyAxis(CONTROLLER_INDEX, JoyAxis.LeftX);
			_direction.y = Input.GetJoyAxis(CONTROLLER_INDEX, JoyAxis.LeftY);

			// A manual dead zone implementation is required to prevent direction changing when letting go of the joystick.
			if(_direction.Abs().x > 0.5f || _direction.Abs().y > 0.5f) {
				Vector3 _lookTarget = new(gfx.GlobalPosition.x + _direction.x, gfx.GlobalPosition.y,
					gfx.GlobalPosition.z + _direction.y);
				gfx.LookAt(_lookTarget); // Rotate in direction of input axis
			}
		} else { // Mouse steering
			Vector2 _mousePosition = GetParent<SubViewport>().GetMousePosition(); // Get screen-space mouse coordinates
			
			// Cast a ray forwards from the camera-relative mouse coordinates.
			Vector3 _rayOrigin = camera.ProjectRayOrigin(_mousePosition);
			Vector3 _rayEnd = _rayOrigin + camera.ProjectRayNormal(_mousePosition) * 2000;
			
			// Get the position at which the ray intersects with the invisible hitbox plane.
			Vector3 _lookTarget = GetWorld3d().DirectSpaceState.IntersectRay(
				PhysicsRayQueryParameters3D.Create(_rayOrigin, _rayEnd))["position"].AsVector3();
			_lookTarget.y = gfx.Position.y;  // Reset the Y position to prevent rotating along an unwanted axis.
			
			gfx.LookAt(_lookTarget);
		}
	}
	
	#endregion
}
