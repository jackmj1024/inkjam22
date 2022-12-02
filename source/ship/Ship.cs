using Godot;
using Godot.Collections;

public partial class Ship : RigidBody3D {
	[Export] private float maxForce = 50f; // Maximum (full input) force that can be added per physics frame.
	[Export] private float maxBrakePower = 2f; // Value ADDED to the RigidBody's linear movement damping when braking.
	
	private const byte RAY_LENGTH = 100; // Distance to project raycasts.
	private ControlMode controlMode = ControlMode.Disabled; // Determines which device is used for input.
	private int controllerIndex; // Device index of the player's controller if using one.

	private float baseDamping; // Stores the linear damping value on the RigidBody and uses it for the braking mechanic.

	[Export] private sbyte currentLap = 1;
	private bool finishedRace = false;

	private Main main;
	
	// Child nodes
	private Camera3D camera;
	private Node3D gfx;
	private SubViewport subview;
	private MultiplayerSynchronizer synchronizer;
    private Label lapLabel;

	#region Godot Overrides

	public override void _EnterTree() {
		// Find child nodes
		camera = GetNode<Camera3D>("Camera");
		gfx = GetNode<Node3D>("GFX");
		subview = GetTree().Root.GetNode<SubViewport>("Main/SubViewportContainer/SubViewport");
		synchronizer = GetNode<MultiplayerSynchronizer>("MultiplayerSynchronizer");
		lapLabel = GetNode<Label>("LapLabel");	
		synchronizer.SetMultiplayerAuthority(Name.ToString().ToInt());
		main = GetTree().Root.GetNode<Main>("Main");
		
		baseDamping = LinearDamp; // Sets baseDamping based on the RigidBody's linear damping setting in the inspector.
		GetTree().Root.GetNode<Area3D>("Main/FinishLine/FinishArea").BodyEntered += CrossFinish;
		ConstantForce += Vector3.Left * 0.1f; // Prevents from spawning within other ships
		base._Ready();
	}

	public override void _PhysicsProcess(double _delta) {
		camera.Current = synchronizer.IsMultiplayerAuthority();
		if(synchronizer.IsMultiplayerAuthority() && controlMode != ControlMode.Disabled && main.GameStarted) {
			Steer();
			Accelerate();
		}
		
		if(main.GameStarted && !lapLabel.Visible && synchronizer.IsMultiplayerAuthority()) {
			lapLabel.Show();
			lapLabel.Text = $"Lap 1/{Main.LapCount}";
		}

		if(finishedRace && Input.IsActionJustPressed("start")) {
			main.Peer.CloseConnection();
			GetTree().ReloadCurrentScene();
		}

		Position *= new Vector3(1, 0, 1); // Kinda weird way of locking the Y position	

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
			if(_direction.Abs().x > 0.75f || _direction.Abs().y > 0.75f) {
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
			Dictionary _collision = GetWorld3d().DirectSpaceState
				.IntersectRay(PhysicsRayQueryParameters3D.Create(_rayOrigin, _rayEnd));
			
			Vector3 _lookTarget = _collision["position"].AsVector3();
			_lookTarget.y = gfx.Position.y; // Reset the Y position to prevent rotating along an unwanted axis.
			gfx.LookAt(_lookTarget);
		}
	}
	
	#endregion

	private void CrossFinish(Node _body) {
		
		float _dist = Position.DistanceTo(GetTree().Root.GetNode<Node3D>("Main/FinishLine").Position);
		if(_dist > 12 || finishedRace)
			return;
		
		if(ConstantForce.z <= 0) {
			if(currentLap >= Main.LapCount) {
				main.numFinished++;
				lapLabel.Text = $"Finished in place #{main.numFinished}! Press START button or ENTER key to restart.";
				finishedRace = true;
			} else {
				currentLap++; ;
				lapLabel.Text = $"Lap {currentLap}/{Main.LapCount}";
			}
		} else {
			currentLap -= 1;
			if(currentLap <= 0) {
				currentLap = 0;
				lapLabel.Text = $"Lap 1/{Main.LapCount}";
			} else {
				lapLabel.Text = $"Lap {currentLap}/{Main.LapCount}";
			}
		}
		
		GD.Print($"Current Lap {currentLap}");
	}
	
	private enum ControlMode {
		Disabled,
		Mouse,
		Joystick
	}
}
