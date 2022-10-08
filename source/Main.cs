using Godot;

public partial class Main : Node3D {
    [Export] private PackedScene playerObject;
    private const ushort PORT = 42069;
    
    private ENetMultiplayerPeer peer = new();
    private LineEdit ipField;

    public override void _Ready() {
        ipField = GetNode<LineEdit>("NetworkMenu/IPField");
        
        // Bind the window resize signal.
        GetTree().Root.Connect("size_changed", new Callable(this,nameof(ResizeViewport))); 
        ResizeViewport(); // Set the initial viewport size.
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
        peer.CreateServer(PORT);
        Multiplayer.MultiplayerPeer = peer;

        peer.PeerConnected += SpawnPlayer; // Connects PeerConnected callback to call the SpawnShip method.
        SpawnPlayer();
    }
    
    // Called automatically when the user presses the "Join" button.
    private void Join() {
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
