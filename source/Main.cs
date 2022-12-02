using Godot;

public partial class Main : Node3D {
    [Export] private bool gameStarted = false;
    internal bool GameStarted => gameStarted;
	private static byte lapCount = 3; 
    internal static byte LapCount => lapCount;
    [Export] internal int numFinished = 0; // Number of players that have finished the race. Used for place counting.
    
    [Export] private PackedScene playerObject;
    private const ushort PORT = 42069;
    
    private ENetMultiplayerPeer peer = new();
    internal ENetMultiplayerPeer Peer => peer;
    
    private Label statusLabel;
    private VBoxContainer netMenu;
    private LineEdit ipField;

    public override void _Ready() {
        netMenu = GetNode<VBoxContainer>("NetworkMenu");
        ipField = GetNode<LineEdit>("NetworkMenu/IPField");
        statusLabel = GetNode<Label>("StatusLabel");
        
        // Bind the window resize signal.
        GetTree().Root.Connect("size_changed", new Callable(this,nameof(ResizeViewport))); 
        ResizeViewport(); // Set the initial viewport size.
    }

    public override void _Process(double _delta) {
        if(!gameStarted && Input.IsActionJustPressed("start") && IsMultiplayerAuthority()) {
            StartGame();
        }

        base._Process(_delta);
    }
    
    private async void StartGame(int _countdown = 3) {
        for(int _i = 0; _i < _countdown; _i++) {
            statusLabel.Text = $"Race starting in {_countdown - _i}...";
            await ToSignal(GetTree().CreateTimer(1), "timeout");
        }
        
        statusLabel.Hide();
        gameStarted = true;

    }

    // Called automatically when the user resizes the window.
    private void ResizeViewport() {
        // There's a lot going on here but its basically trying to convert the window resolution to a size that is
        // usable by the viewport.
        Vector2 _size = GetViewport().GetVisibleRect().Size;
        GetNode<SubViewport>("SubViewportContainer/SubViewport").Size = new Vector2i((int)_size.x, (int)_size.y);
    }
    
    // Called automatically when the user presses the "Host" button.
    private void Host() {
        netMenu.Hide();
        statusLabel.Show();
        statusLabel.Text = "The host can press START button or ENTER key to start the race!";
        peer.CreateServer(PORT);
        Multiplayer.MultiplayerPeer = peer;

        peer.PeerConnected += SpawnPlayer; // Connects PeerConnected callback to call the SpawnShip method.
        SpawnPlayer();
    }
    
    // Called automatically when the user presses the "Join" button.
    private void Join() {
        netMenu.Hide();
        peer.CreateClient(ipField.Text, PORT);
        Multiplayer.MultiplayerPeer = peer;
    }

    // Called automatically when a new client connects.
    private void SpawnPlayer(long _id=1) {
        Ship _ship = playerObject.Instantiate<Ship>();
        _ship.Name = _id.ToString();
        GetNode<SubViewport>("SubViewportContainer/SubViewport").AddChild(_ship);
    }
}
