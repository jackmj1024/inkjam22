using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using Array = System.Array;

public partial class TrackGenerator : Node3D {
    [Export] private float debugDelay;
    [Export] private int linePriority;
    [Export] private int tracKMinLength;
    [Export] private int tracKMaxLength;
    
    private GridMap map;
    private Rect2 area = new Rect2(new Vector2(0, 0), new Vector2(16, 9));
    
    private Vector3i currentPosition;
    private Array<Vector3i> track;

    private bool inProgress = false;
    private bool stopGeneration = false;

    public override void _Ready() {
        Generate();
    }

    private async Task<Array<Vector3i>> Generate() {
        if(GetNode("DebugMap") != null)
            map = GetNode<GridMap>("DebugMap");
        else
            map = new GridMap();

        track = new Array<Vector3i>();
        stopGeneration = false;
        inProgress = true;
        
        currentPosition = new Vector3i((int)(GD.Randi() % (int)(area.Size.x - 2) + 1), 0, 
            (int)(GD.Randi() % (int)(area.Size.y - 2) + 1));
        track.Add(currentPosition);
        map.SetCellItem(currentPosition, (int)TileTypes.Used);

        Vector3i _direction = GetRandomDirection(Vector3i.Zero);
        Vector3i _endPosition = currentPosition - _direction;

        currentPosition += _direction;
        track.Add(currentPosition);
        map.SetCellItem(currentPosition, (int)TileTypes.Used);

        while(currentPosition != _endPosition) {
            _direction = GetRandomDirection(_direction);
            currentPosition += _direction;
            track.Add(currentPosition);
            map.SetCellItem(currentPosition, (int)TileTypes.Used);
            
             await ToSignal(GetTree().CreateTimer(debugDelay), "timeout");
        }

        if(track.Count < tracKMaxLength || track.Count > tracKMaxLength) {
            //return Generate();
        }

        return track;
    }

    private Vector3i GetRandomDirection(Vector3i _previousDirection) {
        Vector3i _position = track[^1];
        Array<Vector3i> _directions = GetValidDirections(_position, _previousDirection);

        if(_directions.Count <= 0) {
            currentPosition = Rollback();
            return GetRandomDirection(_previousDirection);
        }

        return _directions[(int)(GD.Randi() % _directions.Count)];
    }

    private Array<Vector3i> GetValidDirections(Vector3i _position, Vector3i _previousDirection) {
        Array<Vector3i> _validDirections = new Array<Vector3i>();
        
        if(IsDirectionValid(_position, Vector3i.Forward))
            _validDirections.Add(Vector3i.Forward);
        if(IsDirectionValid(_position, Vector3i.Back))
            _validDirections.Add(Vector3i.Back);
        if(IsDirectionValid(_position, Vector3i.Left))
            _validDirections.Add(Vector3i.Left);
        if(IsDirectionValid(_position, Vector3i.Right))
            _validDirections.Add(Vector3i.Right);

        if(_previousDirection != Vector3i.Zero && linePriority != 0) {
            if(IsDirectionValid(_position, _previousDirection)) {
                for(int _i = 0; _i < linePriority; _i++) {
                    _validDirections.Add(_previousDirection);
                }
                
            }
        }
        
        return _validDirections;
    }

    private bool IsDirectionValid(Vector3i _position, Vector3i _direction) {
        Vector3i _testPosition = _position + _direction;
        return map.GetCellItem(_testPosition) == (int)TileTypes.Empty && 
               area.HasPoint(new Vector2(_testPosition.x, _testPosition.z));
    }

    private Vector3i Rollback() {
        track.RemoveAt(track.Count - 1);
        map.SetCellItem(currentPosition, (int)TileTypes.Locked);
        return track[^1];
    }

    private void Stop() {
        if(inProgress) {
            stopGeneration = true;
        }
    }

    private enum TileTypes {
        Empty = -1,
        Used = 0,
        Locked = 1
    }
}
