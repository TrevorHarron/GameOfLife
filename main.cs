using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public partial class main : Control
{
    private Camera2D camera;
    private FileDialog errorDialog;
    private FileDialog openDialog;
    private FileDialog saveDialog;

    

    private Label iterationsLabel;

    private Timer timer;

    private Control hud;
    private SpinBox iterations;
    private Button singleButton;
    private Button playButton;
    private Button openButton;
    private Button saveButton;

    private const string HEADER = "#Life 1.06";

    private struct Cell
    {
        public Int64 X;
        public Int64 Y;

        public override int GetHashCode()
        {
            return (int)((int)X ^ Y);
        }
        public override string ToString()
        {
            return X+" "+Y;
        }
    }

    private System.Collections.Generic.Dictionary<Cell, int> cellCounts = new System.Collections.Generic.Dictionary<Cell, int>();
    private System.Collections.Generic.Dictionary<Cell, bool> aliveCells = new System.Collections.Generic.Dictionary<Cell, bool>();

    private const float ZOOM_FACTOR = 0.2f;
    private float zoom = 0.01f;
    private bool panning = false;
    private Array<Vector2I> directions =new Array<Vector2I> {Vector2I.Down,Vector2I.Up,Vector2I.Right, Vector2I.Left, new Vector2I(-1,1), new Vector2I(1, -1),  new Vector2I(-1,-1), new Vector2I(1, 1)};


    [Signal]
    public delegate void UpdateCellEventHandler(Int64 x, Int64 y, bool isAlive);
    [Signal]
    public delegate void ClearWorldEventHandler();



    public override void _Ready()
	{
        camera = GetNode<Camera2D>("Camera");
        //Get File Dialog stuff
        
        timer = GetNode<Timer>("Timer");
        hud = camera.GetNode<Control>("Control");
        openDialog = hud.GetNode<FileDialog>("OpenFile");
        saveDialog = hud.GetNode<FileDialog>("SaveFile");
        errorDialog = hud.GetNode<FileDialog>("ErrorDialog");
    }

    public override void _UnhandledInput(InputEvent @event)//likely move this to diff area
    {
        if (true)//replace with better error
        {
            return;
        }
        /*if(@event is InputEventMouseButton)
        {
            Vector2 mousePos = GetGlobalMousePosition();
            
            if (Input.IsActionPressed("zoom_in"))
            {
                zoom = Mathf.Clamp(zoom - ZOOM_FACTOR, 0.4f, 4.0f);
              
                camera.Zoom = new Vector2(zoom,zoom);
            }
            else if (Input.IsActionPressed("zoom_out"))
            {
                zoom = Mathf.Clamp(zoom + ZOOM_FACTOR, 0.4f, 4.0f);
                camera.Zoom = new Vector2(zoom, zoom);
            }
            else if (Input.IsActionPressed("click"))
            {
                /*
                Vector2I cell = (Vector2I) GetGridPos(mousePos);
                //GD.Print(GD.VarToStr(cell));
                bool alive = aliveCells.ContainsKey(cell);
                if (!alive)
                {
                    aliveCells.Add(cell, true);
                }
                else
                {
                    aliveCells.Remove(cell);
                }
                
            }
            else if (Input.IsActionPressed("pan"))
            {
                panning = true;
            }
            else if (Input.IsActionJustReleased("pan"))
            {
                panning = false;
            }
        }
        else if(@event is InputEventMouseMotion)
        {
            if(panning)
            {
                InputEventMouseMotion e = @event as InputEventMouseMotion;
                camera.Position -= e.Relative;
            }
            
        }
        //*/
        base._UnhandledInput(@event);
    }

    private void CalculateCells(ICollection<Cell> _cells)
    {
        ulong n = (ulong)_cells.Count;
        foreach (Cell cell in _cells)
        {
            foreach(Vector2I dir in directions)
            {
                Cell c;
                c.X = IncrementByValue(cell.X, (Int64)dir.X);
                c.Y = IncrementByValue(cell.Y, (Int64)dir.Y);
                if (cellCounts.ContainsKey(c))
                {
                    cellCounts[c]++;
                }
                else
                {
                    cellCounts.Add(c,1);
                }
            }
        }
    }

    private Int64 IncrementByValue(Int64 num,Int64 val)//expect value to be 1 or -1
    {
        if (val == 0) return num;
        if(num == Int64.MaxValue && val > 0)
        {
            return Int64.MinValue;
        }
        else if(num == Int64.MinValue && val < 0)
        {
            return Int64.MaxValue;
        }
        return num+ val;
    }

    private void UpdateWorld()
    {
        //use the cellCounts
        
        foreach(Cell cell in cellCounts.Keys)
        {
            if (cellCounts[cell] <= 1)
            {
                if (aliveCells.ContainsKey(cell))
                {
                    aliveCells.Remove(cell);
                }
            }
            else if (cellCounts[cell] >= 4)
            {
                if (aliveCells.ContainsKey(cell))
                {
                    aliveCells.Remove(cell);
                }
            }
            else if (cellCounts[cell] == 2 && aliveCells.ContainsKey(cell))
            {
                aliveCells.TryAdd(cell, true);
            }
            else if (cellCounts[cell] == 3)
            {
                aliveCells.TryAdd(cell, true);
               
            }
            EmitSignal("UpdateCell",cell.X,cell.Y, aliveCells.GetValueOrDefault(cell));
        }
    }

    private Vector2 GetGridPos(Vector2 pos)
    {
        float pixels = 16.0f; // camera.Zoom.X;
        return  (pos.Snapped(new Vector2(pixels,pixels))/ pixels);
    }

    private void UpdateAllCells()
    {
        cellCounts.Clear();
        foreach(KeyValuePair<Cell,bool> kv in aliveCells) {
            cellCounts.Add(kv.Key,0);
        }
        CalculateCells(aliveCells.Keys);
        UpdateWorld();
    }

    public void _on_timer_timeout()//calls our needed funtions when we are processing 
	{
        UpdateAllCells();
	}

    public void _on_file_dialog_file_selected(string fileDir)//calls our needed funtions when we are processing 
    {
        aliveCells.Clear();
        EmitSignal("ClearWorld");
        //update ui
        try
        {
            using (StreamReader file = new StreamReader(fileDir))
            {
                file.ReadLine();//reading the header
                string ln;
                
                while ((ln = file.ReadLine()) != null)
                {
                    string[] items = ln.Split(' ');
                    if (items.Length != 2) { throw new Exception("Parsing Error"); }
                    Cell c;
                    c.X = Convert.ToInt64(items[0]);
                    c.Y = Convert.ToInt64(items[1]);
                    aliveCells.Add(c, true);
                }
                file.Close();
            }
        } 
        catch (Exception e)
        {
            errorDialog.Title = e.Message;
            errorDialog.Show();
        }
    }

    public void _on_save_file_file_selected(string fileDir)
    {

    }

    public void _on_play_pressed()
    {

    }

    public void _on_step_pressed()
    {

    }

    private string printAliveCells()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(HEADER);
        foreach(Cell c in aliveCells.Keys)
        {
            sb.Append(c.ToString());
        }
        return sb.ToString();
    }

}
