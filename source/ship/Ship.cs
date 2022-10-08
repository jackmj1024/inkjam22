using Godot;

public partial class Ship : RigidBody3D {
	[Export] private float maxForce = 50f; // Maximum (full input) force that can be added per physics frame.
	[Export] private float maxBrakePower = 2f; // Value ADDED to the RigidBody's linear movement damping when braking.
	
	private const byte RAY_LENGTH = 100; // Distance to project raycasts.
	private ControlMode controlMode = ControlMode.Disabled; // Determines which device is used for input.
	private int controllerIndex; // Device index of the player's controller if using one.

	private float baseDamping; // Stores the linear damping value on the RigidBody and uses it for the braking mechanic.

	// Child nodes
	private Camera3D camera;
	private Node3D gfx;
	private SubViewport subview;
	private MultiplayerSynchronizer synchronizer;
	
	#region Godot Overrides

	public override void _EnterTree() {
		// Find child nodes
		camera = GetNode<Camera3D>("Camera");
		gfx = GetNode<Node3D>("GFX");
		subview = GetTree().Root.GetNode<SubViewport>("Main/SubViewportContainer/SubViewport");
		synchronizer = GetNode<MultiplayerSynchronizer>("MultiplayerSynchronizer");
		
		synchronizer.SetMultiplayerAuthority(Name.ToString().ToInt());
		camera.Current = synchronizer.IsMultiplayerAuthority();
		
		baseDamping = LinearDamp; // Sets baseDamping based on the RigidBody's linear damping setting in the inspector.
		base._Ready();
	}

	public override void _PhysicsProcess(double _delta) {
		if(synchronizer.IsMultiplayerAuthority() && controlMode != ControlMode.Disabled) {
			Steer();
			Accelerate();
		}

		base._PhysicsProcess(_delta);
	}

	// This callback is exclusively used to detect and switch between input modes.
	public override void _Input(InputEvent _event) {
		if(_event is InputEventMouseButton) {
			controlMode = ControlMode.Mouse;
		} else if(_event is InputEventJoypadButton) {
			controlMode = ControlMode.Joystick;
			controllerIndex = _event.Device;
		}
		base._Input(_event);
	}
	
	#endregion
	
	#region Movement
	
	// Handle moving and braking.
	private void Accelerate() {
		// Moving
		Vector3 _direction = -gfx.GlobalTransform.basis.z;
		float _magnitude = maxForce * Input.GetActionStrength("accelerate");
		ConstantForce = _direction * _magnitude;

		// Braking
		LinearDamp = baseDamping + maxBrakePower * Input.GetActionStrength("brake");
	}

	// Handle steering with either a joystick or mouse.
	private void Steer() {
		Vector2 _direction = Vector2.Zero;
		
		if(controlMode == ControlMode.Joystick) { // Joystick steering
			// Get the direction of the left joystick
			_direction.x = Input.GetJoyAxis(controllerIndex, JoyAxis.LeftX);
			_direction.y = Input.GetJoyAxis(controllerIndex, JoyAxis.LeftY);

			// A manual dead zone implementation is required to prevent direction changing when letting go of the joystick.
			if(_direction.Abs().x > 0.5f || _direction.Abs().y > 0.5f) {
				Vector3 _lookTarget = new(gfx.GlobalPosition.x + _direction.x, gfx.GlobalPosition.y,
					gfx.GlobalPosition.z + _direction.y);
				gfx.LookAt(_lookTarget); // Rotate in direction of input axis
			}
		} else if(controlMode == ControlMode.Mouse) { // Mouse steering
			Vector2 _mousePosition = subview.GetMousePosition(); // Get screen-space mouse coordinates
			
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

	private enum ControlMode {
		Disabled,
		Mouse,
		Joystick
	}
}
