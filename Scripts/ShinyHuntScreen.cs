using Godot;
using System;

public partial class ShinyHuntScreen : Control
{
	public HuntData data;
	Label counter, info;
	Sprite2D shiny, regular;
	double secondTimer = 0; // Tracks how much time has passed up to 1 second
	int resetTimer = 0; // Times how long each reset takes
	bool activeHunt = false; // True when this screen is being used by a hunt
	
	[Signal]
	public delegate void BackButtonPressedEventHandler();
	[Signal]
	public delegate void DeleteSignalEventHandler();
	[Signal]
	public delegate void FinishHuntEventHandler();
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		counter = GetNode<Label>("Count");
		info = GetNode<Label>("HuntInfo");
		shiny = GetNode<Sprite2D>("ShinySprite");
		regular = GetNode<Sprite2D>("RegularSprite");
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
		
		regular.Texture = (Texture2D)GD.Load($"res://Sprites/{hunt.huntFolder}/Regular/{hunt.pokemonName}.png");
		shiny.Texture = (Texture2D)GD.Load($"res://Sprites/{hunt.huntFolder}/Shiny/{hunt.pokemonName}.png");
		UpdateCounterLabel();
		
		float scaleFactor = 240f / regular.Texture.GetHeight();
		regular.Scale = new Vector2(scaleFactor, scaleFactor);
		scaleFactor = 240f / shiny.Texture.GetHeight();
		shiny.Scale = new Vector2(scaleFactor, scaleFactor);
		
		resetTimer = 0;
		secondTimer = 0;
		UpdateInfoLabel();
		
		activeHunt = true;
	}
	
	private void UpdateCounterLabel()
	{
		counter.Text = $"{data.count}";
	}
	
	private void UpdateInfoLabel()
	{
		string timerInHourFormat, finalString = "";
		int fullTime = data.timeSpent;
		int hours, minutes, seconds;
		int oddsDenom = 1365; // Temporary value until odds are dynamically calculated
		

		if (data.showOdds)
		{
			finalString += $"1/{oddsDenom}\n";
		}
		if (data.showFullTimer)
		{
			hours = fullTime / 3600;
			fullTime %= 3600; // Remove the hours to count seconds and minutes
			minutes = fullTime / 60;
			seconds = fullTime % 60;
		
			timerInHourFormat = $"{hours:00}:{minutes:00}:{seconds:00}\n"; // :00 pads 2 zeros to everything
			finalString += timerInHourFormat;
		}
		if (data.showMiniTimer)
		{
			finalString += $"{resetTimer}";
		}
		info.Text = finalString;
	}
	
	// Updates the label displaying the hunt odds and timers
	private void TimerPlusOne()
	{
		data.timeSpent++;
		resetTimer++;
		UpdateInfoLabel();
	}
	
	private void Increment()
	{
		data.count += data.incrementValue;
		resetTimer = 0;
		UpdateCounterLabel();
		UpdateInfoLabel();
	}
	
	private void Decrement()
	{
		data.count = Math.Max(data.count - data.incrementValue, 0);
		UpdateCounterLabel();
	}
	
	private void BackToMenu()
	{
		activeHunt = false;
		EmitSignal("BackButtonPressed");
	}
	
	private void OpenSettings()
	{
		activeHunt = false;
		HuntSettings settingsMenu = (HuntSettings)GD.Load<PackedScene>("res://Scenes/HuntSettings.tscn").Instantiate();
		AddChild(settingsMenu);
		settingsMenu.Name = "Settings";
		
		settingsMenu.CloseSettings += CloseSettings;
		settingsMenu.DeleteHunt += DeleteHunt;
		settingsMenu.SetInitialSettings(data);
	}
	
	private void CloseSettings()
	{
		HuntSettings settingsMenu = GetNode<HuntSettings>("Settings");
		data = settingsMenu.settings;
		
		UpdateCounterLabel();
		UpdateInfoLabel();
		shiny.Visible = data.showShiny;
		regular.Visible = data.showRegular;
		
		RemoveChild(settingsMenu);
		settingsMenu.Cleanup();
		activeHunt = true;
	}
	
	private void ShinyFound()
	{
		activeHunt = false;
		FinishHunt finishMenu = (FinishHunt)GD.Load<PackedScene>("res://Scenes/FinishHunt.tscn").Instantiate();
		AddChild(finishMenu);
		finishMenu.Name = "Finish";
		finishMenu.SetInitialSettings(new HuntData(data));
		
		finishMenu.BackButtonPressed += CloseFinishScreen;
		finishMenu.FinishButtonPressed += HuntCompleted;
	}
	
	private void CloseFinishScreen()
	{
		FinishHunt finishMenu = GetNode<FinishHunt>("Finish");
		RemoveChild(finishMenu);
		finishMenu.Cleanup();
		activeHunt = true;
	}
	
	private void HuntCompleted()
	{
		FinishHunt finishMenu = GetNode<FinishHunt>("Finish");
		data = finishMenu.data;
		RemoveChild(finishMenu);
		finishMenu.Cleanup();
		BackToMenu();
	}
	
	private void DeleteHunt()
	{
		HuntSettings settingsMenu = GetNode<HuntSettings>("Settings");
		RemoveChild(settingsMenu);
		settingsMenu.Cleanup();
		activeHunt = false;
		EmitSignal("DeleteSignal");
	}
	
	// Destroy this UI element
	public void Cleanup()
	{
		QueueFree();
	}
}
