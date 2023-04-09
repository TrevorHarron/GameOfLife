using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public partial class main : Node2D
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

    private TextEdit output;

    private const string HEADER = "#Life 1.06";
    private const byte ALIVE_MASK = 16;
    private const byte NUM_MASK = 15;

    private int playCount = 0;
    private int stepsRun = 10;

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

    private System.Collections.Generic.Dictionary<Cell, byte> cellData = new System.Collections.Generic.Dictionary<Cell, byte>();

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
        playButton = hud.GetNode<Button>("Play");
        output = hud.GetNode<TextEdit>("Output");
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
        foreach (Cell cell in _cells)
        {
            foreach(Vector2I dir in directions)
            {
                Cell c;
                c.X = IncrementByValue(cell.X, (Int64)dir.X);
                c.Y = IncrementByValue(cell.Y, (Int64)dir.Y);
                if (cellData.ContainsKey(c) && (cellData[c] & NUM_MASK) < 4)
                {
                    cellData[c]++;
                }
                else
                {
                    cellData.Add(c,1);
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
        
        foreach(Cell cell in cellData.Keys)
        {
            bool wAlive = (cellData[cell] & ALIVE_MASK) != 0;
            byte count = (byte)(cellData[cell] & NUM_MASK);
            if (count <= 1)
            {
                cellData.Remove(cell);
            }
            else if (cellData[cell] >= 4)
            {
                if (cellData.ContainsKey(cell))
                {
                    cellData.Remove(cell);
                }
            }
            else if (count == 2 && wAlive)
            {
                cellData.TryAdd(cell, ALIVE_MASK);
            }
            else if (count == 3)
            {
                cellData[cell] =  ALIVE_MASK;
               
            }
            EmitSignal("UpdateCell",cell.X,cell.Y, cellData.ContainsKey(cell));
        }
    }

    private Vector2 GetGridPos(Vector2 pos)
    {
        float pixels = 16.0f; // camera.Zoom.X;
        return  (pos.Snapped(new Vector2(pixels,pixels))/ pixels);
    }

    private void UpdateAllCells()
    {
        //foreach(KeyValuePair<Cell,byte> kv in cellData) {
            //GD.Print(kv.Key, kv.Value);
            //cellData.TryAdd(kv.Key,ALIVE_MASK);
        //}
        CalculateCells(cellData.Keys);
        UpdateWorld();
    }

    public void _on_timer_timeout()//calls our needed funtions when we are processing 
	{
        if(stepsRun < 0 || playCount < stepsRun)
        {
            ++playCount;
            UpdateAllCells();
        }
        if(playCount >= stepsRun && stepsRun >= 0)
        {

            timer.Paused = true;
            timer.Stop();
            output.Text = printAliveCells();
            playButton.Disabled = false;

            saveButton.Disabled = false;
        }
        
	}

    public void _on_file_dialog_file_selected(string fileDir)//calls our needed funtions when we are processing 
    {
        cellData.Clear();
        EmitSignal("ClearWorld");
        //update ui
        try
        {
            using (StreamReader file = new StreamReader(fileDir))
            {
                string h =file.ReadLine();//reading the header
                GD.Print(h);
                if(!h.Equals(HEADER))
                {
                    throw new Exception("Not in Life 1.06 Format");
                }
                string ln;
                
                while ((ln = file.ReadLine()) != null)
                {
                    string[] items = ln.Split(' ');
                    if (items.Length != 2) { throw new Exception("Parsing Error"); }
                    Cell c;
                    c.X = Convert.ToInt64(items[0]);
                    c.Y = Convert.ToInt64(items[1]);
                    GD.Print(c.ToString());
                    cellData.Add(c, ALIVE_MASK);
                }
                file.Close();
                GD.Print("Done Loading");
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

    public void _on_load_input_pressed()
    {
        openDialog.Show();
    }

    public void _on_play_pressed()
    {
        playCount = 0;
        timer.Paused = false;
        playButton.Disabled = true;
        saveButton.Disabled = true;
        timer.Start();
    }

    public void _on_step_pressed()
    {
        playCount += 1;
        UpdateAllCells();
        output.Text = printAliveCells();
    }

    public void _on_steps_sec_value_changed(float stepPerSec)
    {
        if (stepPerSec > 0)
        {
            timer.WaitTime = 1.0f/stepPerSec;
        }
    }

    public void _on_num_steps_value_changed(float numSteps)
    {
        if (numSteps > 0)
        {
            stepsRun = (int)numSteps;
        }
    }

    private string printAliveCells()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(HEADER);
        foreach(Cell c in cellData.Keys)
        {
            if ((cellData[c]&ALIVE_MASK) != 0)
            {
                sb.Append(c.ToString());
            }
            
        }
        return sb.ToString();
    }

}
