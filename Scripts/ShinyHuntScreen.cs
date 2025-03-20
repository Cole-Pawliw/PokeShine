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
		float oddsDenom = (float)Math.Round(CalculateHuntOdds(), 2);
		

		if (data.showOdds)
		{
			// Some hunt methods have too variable of odds to determine with accuracy
			// The actual odds will be hidden in these cases to prevent confusion
			if (oddsDenom != 0)
			{
				finalString += $"1/{oddsDenom}\n";
			}
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
	
	private float CalculateHuntOdds()
	{
		GameInfo game = GameHuntInformation.gameInfoDict[data.huntGame];
		float odds = (game.methodID < 7) ? 8192f : 4096f; // Base odds in different games
		int shinyRolls = (data.charm) ? 3 : 1; // Used to track multiple factors affecting odds
		int chain; // Used in some cases to calculate odds with a formula
		
		switch (data.huntMethod)
		{
			case "Shiny Family Breeding":
				odds = 128;
				break;
			case "Masuda Method":
				odds += (game.methodID < 5) ? 5 : 6; // Masuda breeding has different rolls starting in gen 5
				break;
			case "Poke Radar": // This case isn't fully accurate to what's going on but these odds are good enough
				shinyRolls = 1; // Shiny charm doesn't affect odds for this method
				chain = Math.Min(data.count, 40);
				float intermediary = 65535f / (8200 - chain * 200);
				odds = 65536f / (float)Math.Ceiling(intermediary);
				
				if (game.methodID > 6)
				{
					odds /= 2; // Double odds to account for increased shiny odds after gen 6
				}
				break;
			case "Friend Safari":
				shinyRolls += 4;
				break;
			case "Chain Fishing":
				chain = Math.Min(data.count, 20);
				shinyRolls += 2 * chain;
				break;
			case "Dex Nav":
				odds = 0; // No odds indicates there are too many variables to accurately track odds
				break;
			case "SOS Chain":
				// Shiny rolls increase by 4 after chains of 11, 21, and 31
				for (int i = 1; i < 4 && i * 10 + 1 < data.count; i++)
				{
					shinyRolls += 4;
				}
				break;
			case "Ultra Wormhole":
				odds = 0; // No odds indicates there are too many variables to accurately track odds
				break;
			case "Catch Combo":
				shinyRolls += 1; // This method assumes the player is always using a lure
				// If statements are required here instead of a loop because of varying increments
				if (data.count > 10)
				{
					shinyRolls += 3;
				}
				if (data.count > 20)
				{
					shinyRolls += 4;
				}
				if (data.count > 30)
				{
					shinyRolls += 4;
				}
				break;
			case "Dynamax Adventures":
				odds = 100;
				shinyRolls = 1; // Shiny charm doesn't affect these odds
				break;
			case "Mass Outbreak": // Mass outbreaks are in both PLA and SV, but have different functionality in each
				shinyRolls += 12; // Currently only considering PLA until a good solution is found for SV sandwiches
				break;
			case "Massive Mass Outbreak":
				shinyRolls += 25;
				break;
		}
		
		return odds / shinyRolls;
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
