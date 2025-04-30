using Godot;
using System;
using System.Collections.Generic;

public class HuntData
{
	public HuntData()
	{
		pokemon = new List<string>();
		huntID = ++instances;
	}
	public HuntData(string name, string game)
	{
		pokemon = new List<string>();
		pokemon.Add(name);
		huntGame = game;
		huntID = ++instances;
	}
	public HuntData(string name, string game, string method, bool shinyCharm, string time)
	{
		pokemon = new List<string>();
		pokemon.Add(name);
		huntGame = game;
		huntMethod = method;
		charm = shinyCharm;
		startDate = time;
		huntID = ++instances;
	}
	public HuntData(List<string> names, string game, string method, bool shinyCharm, string time)
	{
		pokemon = new List<string>();
		pokemon = names;
		huntGame = game;
		huntMethod = method;
		charm = shinyCharm;
		startDate = time;
		huntID = ++instances;
	}
	public HuntData(bool comp, string start, string name,
					string game, string method, bool shinyCharm, int c, int inc)
	{
		pokemon = new List<string>();
		isComplete = comp;
		startDate = start;
		pokemon.Add(name);
		huntGame = game;
		huntMethod = method;
		charm = shinyCharm;
		count = c;
		incrementValue = inc;
		huntID = ++instances;
	}
	public HuntData(HuntData src)
	{
		isComplete = src.isComplete;
		startDate = src.startDate;
		
		pokemon = new List<string>(src.pokemon);
		
		huntGame = src.huntGame;
		
		huntMethod = src.huntMethod;
		huntRoute = src.huntRoute;
		charm = src.charm;
		count = src.count;
		incrementValue = src.incrementValue;
		timeSpent = src.timeSpent;
		
		showShiny = src.showShiny;
		showRegular = src.showRegular;
		showOdds = src.showOdds;
		showFullTimer = src.showFullTimer;
		showMiniTimer = src.showMiniTimer;
		
		huntIndex = src.huntIndex;
		huntID = src.huntID;
	}
	
	public bool Equals(HuntData other)
	{
		return Equals(other, this);
	}
	
	public override bool Equals(object obj)
	{
		if (obj == null || GetType() != obj.GetType())
		{
			return false;
		}
		
		var other = (HuntData)obj;
		return (other.isComplete, other.startDate, other.pokemon, other.huntGame, other.count,
				other.incrementValue).Equals((isComplete, startDate, pokemon, huntGame, count, incrementValue));
	}
	
	public override int GetHashCode()
	{
		return (isComplete, startDate, pokemon, huntGame, count, incrementValue).GetHashCode();
	}
	
	public static bool operator ==(HuntData hunt1, HuntData hunt2)
	{
		return hunt1.Equals(hunt2);
	}
	
	public static bool operator !=(HuntData hunt1, HuntData hunt2)
	{
		return !hunt1.Equals(hunt2);
	}
	
	public bool isComplete { get; set; } = false; // Marked true if the hunt is completed
	public string startDate { get; set; } // Datetime formatted string storing when the hunt was created
	
	public List<string> pokemon { get; set; } // The name of the pokemon being hunted
	
	public string huntFolder { get; private set; } // The folder to be used to access the sprites for this game
	private string _huntGame; // The game the pokemon is being hunted in
	public string huntGame
	{
		get
		{
			return _huntGame;
		}
		set
		{
			_huntGame = value;
			
			// Set the containing folder based on the game being hunted in
			if (value == "Gold" || value == "Silver" || value == "Crystal")
			{
				huntFolder = "GS";
			}
			else if (value == "Ruby" || value == "Sapphire" || value == "Emerald")
			{
				huntFolder = "RS";
			}
			else if (value == "Fire Red" || value == "Leaf Green")
			{
				huntFolder = "FL";
			}
			else if (value == "Diamond" || value == "Pearl" || value == "Platinum")
			{
				huntFolder = "DP";
			}
			else if (value == "Heart Gold" || value == "Soul Silver")
			{
				huntFolder = "HS";
			}
			else if (value == "Black" || value == "White" || value == "Black 2" || value == "White 2")
			{
				huntFolder = "BW";
			}
			else if (value == "X" || value == "Y" || value == "Alpha Sapphire" || value == "Omega Ruby" || 
					value == "Sun" || value == "Moon" || value == "Ultra Sun" || value == "Ultra Moon")
			{
				huntFolder = "BankModels";
			}
			else
			{
				huntFolder = "HomeModels";
			}
		}
	}
	
	public string huntMethod { get; set; } // The method being performed for the hunt, used to determine odds
	public string huntRoute { get; set; } // Unused for now, will help to creat multi hunts
	public bool charm { get; set; } = false; // An item that increases shiny odds
	public int count { get; set; } = 0; // The current number of encounters/resets for the pokemon
	public int incrementValue { get; set; } = 1; // The number to increase the counter by
	public int timeSpent { get; set; } = 0; // The number of seconds spent hunting
	
	//Hunt settings
	public bool showShiny { get; set; } = true; // Toggles the visibility of the shiny sprite in hunt screen
	public bool showRegular { get; set; } = true; // Toggles the visibility of the regular sprite in hunt screen
	public bool showOdds { get; set; } = true; // Toggles the visibility of the hunt odds in hunt screen
	public bool showFullTimer { get; set; } = true; // Toggles the visibility of the hunt timer in hunt screen
	public bool showMiniTimer { get; set; } = true; // Toggles the visibility of the encounter timer in hunt screen
	
	public int huntIndex { get; set; } // An index used for sorting the hunts, indexing is separate for active and completed hunts
	public int huntID { get; private set; } // Unique identifier for each HuntData
	public static int instances;
}

public partial class ActiveHunt : Control
{
	public HuntData data;
	Label label, multiIndicator, timer;
	Button sortButton;
	
	double secondTimer = 0; // Tracks how much time has passed up to 1 second
	int resetTimer = 0; // Times how long each reset takes
	bool activeHunt = false; // True when this screen is being used by a hunt
	
	[Signal]
	public delegate void SelectButtonPressedEventHandler(int selectedHuntID);
	[Signal]
	public delegate void SortButtonDownEventHandler(int selectedHuntID);
	[Signal]
	public delegate void SortButtonUpEventHandler();
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		label = GetNode<Label>("Count");
		multiIndicator = GetNode<Label>("MultiIndicator");
		timer = GetNode<Label>("Timer");
		sortButton = GetNode<Button>("SortButton");
	}
	
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		// Only increase the time while the hunt is active
		if (activeHunt)
		{
			secondTimer += delta;
		}
		
		// Update the info label with new seconds
		while (secondTimer > 1.0)
		{
			TimerPlusOne();
			secondTimer -= 1.0;
		}
	}
	
	public void InitializeHunt(HuntData hunt)
	{
		data = hunt;
		Sprite2D sprite = GetNode<Sprite2D>("ShinySprite");
		sprite.Texture = (Texture2D)GD.Load($"res://Sprites/{data.huntFolder}/Shiny/{data.pokemon[0]}.png");
		
		// Scale the size of the image to fit the ActiveHunt scene
		float scaleFactor = Math.Min(90f / sprite.Texture.GetWidth(), 85f / sprite.Texture.GetHeight());
		sprite.Scale = new Vector2(scaleFactor, scaleFactor);
		
		UpdateLabels();
	}
	
	public void StartTimer()
	{
		activeHunt = true;
	}
	
	public void StopTimer()
	{
		activeHunt = false;
		resetTimer = 0;
		secondTimer = 0;
		UpdateTimer();
	}
	
	public void UpdateLabels()
	{
		UpdateCount();
		UpdateTimer();
		UpdateMulti();
	}
	
	public void UpdateCount()
	{
		label.Text = $"{data.count}";
	}
	
	private void UpdateTimer()
	{
		string finalString = "";
		int fullTime = data.timeSpent;
		int hours, minutes, seconds;
		
		if (data.showFullTimer)
		{
			hours = fullTime / 3600;
			fullTime %= 3600; // Remove the hours to count seconds and minutes
			minutes = fullTime / 60;
			seconds = fullTime % 60;
		
			finalString += $"{hours:00}:{minutes:00}:{seconds:00}"; // :00 pads 2 zeros to everything
		}
		if (data.showMiniTimer)
		{
			finalString += $"     {resetTimer}s";
		}
		timer.Text = finalString;
	}
	
	private void UpdateMulti()
	{
		if (data.pokemon.Count > 1)
		{
			multiIndicator.Text = $"+{data.pokemon.Count - 1}";
		}
		else
		{
			multiIndicator.Text = "";
		}
	}
	
	// Updates the label displaying the hunt odds and timers
	private void TimerPlusOne()
	{
		resetTimer++;
		UpdateTimer();
	}
	
	private void Increment()
	{
		data.count += data.incrementValue;
		data.timeSpent += resetTimer;
		resetTimer = 0;
		UpdateCount();
		UpdateTimer();
		StartTimer();
	}
	
	private void Decrement()
	{
		data.count = Math.Max(data.count - data.incrementValue, 0);
		UpdateCount();
	}
	
	public void ToggleSort()
	{
		sortButton.Visible = !sortButton.Visible;
	}
	
	private void SelectButton()
	{
		EmitSignal("SelectButtonPressed", data.huntID);
	}
	
	private void SortSelect()
	{
		EmitSignal("SortButtonDown", data.huntID);
	}
	
	private void SortDeselect()
	{
		EmitSignal("SortButtonUp");
	}
	
	// Destroy this UI element
	public void Cleanup()
	{
		QueueFree();
	}
}
