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
    private AcceptDialog errorDialog;
    private FileDialog openDialog;
    private FileDialog saveDialog;

    private Label tickNumberLabel;

    private Timer timer;

    private Control hud;
    private SpinBox iterations;
    private Button singleButton;
    private Button playButton;
    private Button stopButton;
    private Button openButton;
    private Button saveButton;

    private SpinBox tickPSec;
    private SpinBox iterationsBox;

    private TextEdit output;
    private TextEdit input;
    private string fileName = "";

    private const string HEADER = "#Life 1.06";
    private const byte ALIVE_MASK = 16;
    private const byte NUM_MASK = 15;

    private int playCount = 0;
    private int stepsToRun = 10;

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
        timer.Stop();
        hud = camera.GetNode<Control>("Control");
        openDialog = hud.GetNode<FileDialog>("OpenFile");
        saveDialog = hud.GetNode<FileDialog>("SaveFile");
        errorDialog = hud.GetNode<AcceptDialog>("ErrorDialog");
        playButton = hud.GetNode<Button>("PlayButton");
        saveButton = hud.GetNode<Button>("SaveOutput");
        output = hud.GetNode<TextEdit>("Output");
        input = hud.GetNode<TextEdit>("Input");
        tickNumberLabel = hud.GetNode<Label>("TickNumber");

        iterationsBox = hud.GetNode<SpinBox>("NumSteps");
        tickPSec = hud.GetNode<SpinBox>("StepsSec");
    }

    public override void _UnhandledInput(InputEvent @event)//likely move this to diff area
    {
        
        if (@event is InputEventMouseButton)
        {
            Vector2 mousePos = GetGlobalMousePosition();

            /*if (Input.IsActionPressed("zoom_in"))
            {
                zoom = Mathf.Clamp(zoom - ZOOM_FACTOR, 0.4f, 4.0f);
              
                camera.Zoom = new Vector2(zoom,zoom);
            }
            else if (Input.IsActionPressed("zoom_out"))
            {
                zoom = Mathf.Clamp(zoom + ZOOM_FACTOR, 0.4f, 4.0f);
                camera.Zoom = new Vector2(zoom, zoom);
            }
            else*/
            if (!timer.IsStopped())
            {

                return;
            }
            if (Input.IsActionPressed("click"))
            {

                Vector2I pos = (Vector2I)GetGridPos(mousePos);
                Cell cell;
                cell.X = pos.X;
                cell.Y = pos.Y;
                bool alive = cellData.ContainsKey(cell);
                if (!alive)
                {
                    cellData.Add(cell, ALIVE_MASK);
                }
                else
                {
                    cellData.Remove(cell);
                }
                EmitSignal("UpdateCell", cell.X, cell.Y, cellData.ContainsKey(cell));
            }
        }
            /*
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
        */
        base._UnhandledInput(@event);
    }

    private void CalculateCells(List<Cell> _cells)
    {
        foreach (Cell cell in _cells)
        {
            foreach(Vector2I dir in directions)
            {
                Cell c;
                c.X = IncrementByValue(cell.X, (Int64)dir.X);
                c.Y = IncrementByValue(cell.Y, (Int64)dir.Y);
                if (cellData.ContainsKey(c))
                {
                    byte count = (byte)(cellData[c] & NUM_MASK);
                    if(count < 4) {
                        cellData[c] = (byte)(cellData[c] + 1);
                        //GD.Print("incremented value");
                    }
                    
                }
                else
                {
                    //GD.Print("new value");
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
        var keys = cellData.Keys;
        foreach (Cell cell in keys)
        {
            bool wAlive = (cellData[cell] & ALIVE_MASK) != 0;
            byte count = (byte)(cellData[cell] & NUM_MASK);
            //GD.Print(cell +" data "+ cellData[cell] + " Alive " + wAlive + " count " + count+ "\n");
            if (count <= 1)
            {
                cellData.Remove(cell);
            }
            else if (count >= 4)
            {
                if (cellData.ContainsKey(cell))
                {
                    cellData.Remove(cell);
                }
            }
            else if (count == 2 )
            {
                if (wAlive)
                {
                    cellData[cell] = ALIVE_MASK;
                }
                else
                {
                    cellData.Remove(cell);
                }
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
        CalculateCells(cellData.Keys.ToList<Cell>());
        UpdateWorld();
    }

    public void _on_timer_timeout()//calls our needed funtions when we are processing 
	{
       // GD.Print("Timer tick: " + stepsToRun + " " + playCount);
        if(stepsToRun <= 0 || playCount < stepsToRun)
        {
           
            UpdateAllCells();
            ++playCount;
        }
        
        if (playCount >= stepsToRun && stepsToRun > 0)
        {
            timer.Paused = true;
            timer.Stop();
            playButton.Disabled = false;
            saveButton.Disabled = false;
            iterationsBox.Editable = true;
            tickPSec.Editable = true;
            stopButton.Disabled = true;
        }
        tickNumberLabel.Text = "" + playCount;
        output.Text = printAliveCells();

    }

    public void _on_file_dialog_file_selected(string fileDir)//calls our needed funtions when we are processing 
    {
        cellData.Clear();
        EmitSignal("ClearWorld");
        
        //update ui
        try
        {
            if (String.IsNullOrEmpty(fileDir))
            {
                throw new Exception("No File Selected");
            }
            string ext = fileDir.Split('.').Last();
            if(String.IsNullOrEmpty(ext) || !(ext.Match("txt",false) ||ext.Match("life") || ext.Match("lif"))){
                throw new Exception("invlaid file type");
            }
            fileName = fileDir;
            using (StreamReader file = new StreamReader(fileDir))
            {
                StringBuilder sb = new StringBuilder();
                
                string h =file.ReadLine();//reading the header
                sb.AppendLine(h);
                //GD.Print(h);
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
                    sb.AppendLine(c.ToString());
                    //GD.Print(c.ToString());
                    cellData.Add(c, ALIVE_MASK);
                    EmitSignal("UpdateCell", c.X, c.Y, cellData.ContainsKey(c));
                }
                file.Close();

                input.Text = sb.ToString();
               // GD.Print("Done Loading");
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
        try
        {
            using (StreamWriter file = new StreamWriter(fileDir))
            {
                file.Write(printAliveCells());
                file.Close();
            }
        } 
        catch (Exception e)
        {
            errorDialog.Title = e.Message;
            errorDialog.Show();
        }
        
    }

    public void _on_reset_pressed()
    {
        _on_file_dialog_file_selected(fileName);
    }

    public void _on_load_input_pressed()
    {
        openDialog.Show();
    }

    public void _on_save_output_pressed()
    {
        saveDialog.Show();
    }

    public void _on_play_pressed()
    {
        if (String.IsNullOrEmpty(fileName))
        {
            return;
        }
        playCount = 0;
        timer.Paused = false;
        playButton.Disabled = true;
        stopButton.Disabled = false;
        saveButton.Disabled = true;
        tickPSec.Editable = false;
        iterationsBox.Editable = false;
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
        stepsToRun = (int)numSteps;
    }

    public void _on_show_button_toggled(bool show)
    {
        hud.Visible = show;
    }

    public void _on_stop_button_pressed()
    {
        playButton.Disabled = false;
        saveButton.Disabled = false;
        tickPSec.Editable = true;
        iterationsBox.Editable = true;
        timer.Paused = true;
        stopButton.Disabled = true;
        timer.Stop();
        output.Text = printAliveCells();
    }

    private string printAliveCells()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(HEADER);
        foreach(Cell c in cellData.Keys)
        {
            if ((cellData[c]&ALIVE_MASK) != 0)
            {
                sb.AppendLine(c.ToString());
            }
            
        }
        return sb.ToString();
    }

}
