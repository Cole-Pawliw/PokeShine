using Godot;
using System;
using System.Collections.Generic;

public partial class ShinyHuntScreen : Control
{
	public HuntData data = new HuntData();
	AudioStreamPlayer tickPlayer;
	Label counter, info;
	TextureButton resetButton;
	List<Sprite2D> sprites;
	double secondTimer = 0; // Tracks how much time has passed up to 1 second
	int resetTimer = 0; // Times how long each reset takes
	bool activeHunt = false; // True when this screen is being used by a hunt
	bool muted = false; // Determines whether to play a tick sound or not
	
	float halfXAnchor = 0.5f, thirdXAnchor = 0.333f, quarterXAnchor = 0.25f, yAnchor = 0.833f; // Proportions for settings constants
	int halfX = 240, thirdX = 160, quarterX = 120, y = 600, yOffset = 120; // Constant values used for placing sprites in a grid
	
	[Signal]
	public delegate void BackButtonPressedEventHandler();
	[Signal]
	public delegate void DeleteSignalEventHandler();
	[Signal]
	public delegate void HuntChangedEventHandler();
	[Signal]
	public delegate void FinishHuntEventHandler(string nickname, string ball, string gender);
	[Signal]
	public delegate void RequestSaveEventHandler();
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		tickPlayer = GetNode<AudioStreamPlayer>("TickPlayer");
		counter = GetNode<Label>("Count");
		info = GetNode<Label>("HuntInfo");
		resetButton = GetNode<TextureButton>("ResetButton");
		
		sprites = new List<Sprite2D>(15); // Up to 15 sprites can be supported for multi-hunts
		for (int i = 1; i <= 15; i++)
		{
			sprites.Add(GetNode<Sprite2D>($"Sprite{i}"));
		}
		
		SetColors();
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
	
	public void SetColors()
	{
		TextureButton backButton, shinyButton, settingsButton, subButton, infoButton;
		backButton = GetNode<TextureButton>("BackButton");
		shinyButton = GetNode<TextureButton>("ShinyButton");
		settingsButton = GetNode<TextureButton>("SettingsButton");
		subButton = GetNode<TextureButton>("SubButton");
		infoButton = GetNode<TextureButton>("InfoButton");
		
		backButton.TextureNormal = (Texture2D)GD.Load($"res://Assets/Buttons/{GlobalSettings.colorMode}/back.png");
		shinyButton.TextureNormal = (Texture2D)GD.Load($"res://Assets/Buttons/{GlobalSettings.colorMode}/shine.png");
		settingsButton.TextureNormal = (Texture2D)GD.Load($"res://Assets/Buttons/{GlobalSettings.colorMode}/settings.png");
		subButton.TextureNormal = (Texture2D)GD.Load($"res://Assets/Buttons/{GlobalSettings.colorMode}/minus.png");
		resetButton.TextureNormal = (Texture2D)GD.Load($"res://Assets/Buttons/{GlobalSettings.colorMode}/reset.png");
		infoButton.TextureNormal = (Texture2D)GD.Load($"res://Assets/Buttons/{GlobalSettings.colorMode}/info.png");
		
		ColorRect bg = GetNode<ColorRect>("Background");
		bg.Color = new Color(GlobalSettings.backgrounds[GlobalSettings.colorMode - 1]);
	}
	
	public override void _Notification(int what)
	{
		if (what == NotificationWMGoBackRequest && activeHunt)
		{
			BackToMenu();
		}
	}
	
	public void InitializeHunt(HuntData hunt)
	{
		data = hunt;
		
		SetSprites();
		UpdateCounterLabel();
		SetResetButton();
		
		resetTimer = 0;
		secondTimer = 0;
		UpdateInfoLabel();
		
		activeHunt = true;
	}
	
	private void SetSprites()
	{
		ClearSprites(); // Wipe any previous sprites
		if (data.pokemon.Count == 1) // Single shiny hunt, show regular and shiny sprites
		{
			sprites[0].Texture = (Texture2D)GD.Load($"res://Sprites/{data.huntFolder}/Regular/{data.pokemon[0]}.png");
			sprites[0].Visible = data.showRegular;
			sprites[1].Texture = (Texture2D)GD.Load($"res://Sprites/{data.huntFolder}/Shiny/{data.pokemon[0]}.png");
			sprites[1].Visible = data.showShiny;
		}
		else // Multi-hunt
		{
			for (int i = 0; i < data.pokemon.Count; i++)
			{
				sprites[i].Texture = (Texture2D)GD.Load($"res://Sprites/{data.huntFolder}/Shiny/{data.pokemon[i]}.png");
				sprites[i].Visible = data.showShiny;
			}
		}
		PositionSprites(data.pokemon.Count);
		ScaleSprites(data.pokemon.Count);
	}
	
	private void PositionSprites(int amount)
	{
		int bufferCalcAmount = amount > 10 ? 10 : amount;
		int rows = (int)Math.Max(Math.Ceiling(bufferCalcAmount / 2.0), 2); // Minimum of 2 rows is needed
		int largeAmountRows = (int)Math.Max(Math.Ceiling(amount / 3.0), 4); // Only used when amount > 10
		int largeAmountOffset = yOffset + 50; // Increased offset to prevent overlap between sprites and counter
		int buffer = y / (rows + 1); // y = 600 will result in a whole number for any amount up to 10
		
		if (amount == 1 || amount == 2) // Unique case for 2 sprites, one of top of the other
		{
			sprites[0].Position = new Vector2(halfX, buffer + yOffset);
			sprites[1].Position = new Vector2(halfX, buffer * 2 + yOffset);
		}
		else if (amount <= 10) // More than 2 sprites uses 2 columns and up to 5 rows, with an odd number being centred
		{
			for (int i = 0; i < rows; i++)
			{
				for (int j = 0; j < 2; j++)
				{
					if (i == rows - 1 && amount % 2 == 1) // Last row, use halfX
					{
						sprites[i * 2 + j].Position = new Vector2(halfX, buffer * (i+1) + yOffset);
						break;
					}
					else
					{
						int xFactor = j == 0 ? 1 : 3;
						sprites[i * 2 + j].Position = new Vector2(quarterX * xFactor, buffer * (i+1) + yOffset);
					}
				}
			}
		}
		else // Any more than 10 sprites uses 3 columns
		{
			for (int i = 0; i < largeAmountRows; i++)
			{
				for (int j = 0; j < 3; j++)
				{
					if (i == largeAmountRows - 1 && amount % 3 == 1) // Use halfX
					{
						sprites[i * 3 + j].Position = new Vector2(halfX, buffer * (i+1) + largeAmountOffset);
						break;
					}
					else if (i == largeAmountRows - 1 && amount % 3 == 2) // Use thirdX
					{
						sprites[i * 3 + j].Position = new Vector2(thirdX * (j + 1), buffer * (i+1) + largeAmountOffset);
						if (j == 1)
						{
							break;
						}
					}
					else
					{
						sprites[i * 3 + j].Position = new Vector2(quarterX * (j + 1), buffer * (i+1) + largeAmountOffset);
					}
				}
			}
		}
	}
	
	private void ScaleSprites(int amount)
	{
		if (amount == 1)
		{
			amount = 2;
		}
		if (amount > 10)
		{
			amount = 10; // Always use smallest scaling for sprites after 10
		}
		int rows = (int)Math.Max(Math.Ceiling(amount / 2.0), 2); // Minimum of 2 rows is needed
		int xBuffer = halfX; // Buffer of pixels in the x dimension available for the sprite to fill
		int yBuffer = y / (rows + 1); // Buffer of pixels in the y dimension available for the sprite to fill
		float scaleFactor;
		
		foreach (Sprite2D sprite in sprites)
		{
			if (sprite.Texture != null)
			{
				scaleFactor = Math.Min((float)yBuffer / sprite.Texture.GetHeight(), (float)xBuffer / sprite.Texture.GetWidth());
				if (data.huntFolder == "BankModels")
				{
					scaleFactor *= 0.6f; // Bank sprites are blurry and consistently need scaling down
				}
				else if (data.huntFolder == "HomeModels")
				{
					scaleFactor *= 0.8f;
				}
				sprite.Scale = new Vector2(scaleFactor, scaleFactor);
			}
		}
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
		if (data.showCombo)
		{
			finalString += $"{data.combo} Combo\n";
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
			finalString += $"{resetTimer}s";
		}
		info.Text = finalString;
	}
	
	private float CalculateHuntOdds()
	{
		GameInfo game = GameHuntInformation.gameInfoDict[data.huntGame];
		float odds = (game.methodID < 7) ? 8192f : 4096f; // Base odds in different games
		int shinyRolls = (data.charm) ? 3 : 1; // Used to track multiple factors affecting odds
		int chain; // Used in some cases to calculate odds with a formula
		
		if (data.huntGame == "Legends Arceus" && data.charm)
		{
			shinyRolls++; // PLA gives one extra shiny roll
		}
		
		switch (data.huntMethod)
		{
			case "Shiny Family Breeding":
				odds = data.oddsBonus == 1 ? 64 : 128; // Double odds if breeding with a shiny egg group ditto
				break;
			case "Masuda Method":
				shinyRolls += (game.methodID < 5) ? 4 : 5; // Masuda breeding has different rolls starting in gen 5
				break;
			case "Poke Radar": // This case isn't fully accurate to what's going on but these odds are good enough
				shinyRolls = 1; // Shiny charm doesn't affect odds for this method
				chain = Math.Min(data.combo, 40);
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
				chain = Math.Min(data.combo, 20);
				shinyRolls += 2 * chain;
				break;
			case "Dex Nav":
				shinyRolls = 1; // Charm is already factored in with these calculations
				odds = 10000f / (float)CalculateDexNav(); // Dex nav uses a target value compared to 10000
				break;
			case "SOS Chain":
				// Shiny rolls increase by 4 after chains of 11, 21, and 31
				for (int i = 1; i < 4 && i * 10 + 1 <= data.combo; i++)
				{
					shinyRolls += 4;
				}
				break;
			case "Ultra Wormhole":
				odds = 0; // No odds indicates there are too many variables to accurately track odds
				break;
			case "Catch Combo":
				// If statements are required here instead of a loop because of varying increments
				if (data.combo > 10)
				{
					shinyRolls += 3;
				}
				if (data.combo > 20)
				{
					shinyRolls += 4;
				}
				if (data.combo > 30)
				{
					shinyRolls += 4;
				}
				break;
			case "Dynamax Adventures":
				odds = 300; // Shiny charm will change this to 1/100
				break;
			case "Mass Outbreak": // Mass outbreaks are in both PLA and SV, but have different functionality in each
				if (data.huntGame == "Legends Arceus")
				{
					shinyRolls += 25; // PLA adds a flat 25 to shiny rolls
				}
				else // SV use combos for determining extra rolls
				{
					if (data.combo >= 30)
					{
						shinyRolls++;
					}
					if (data.combo >= 60)
					{
						shinyRolls++;
					}
				}
				
				break;
			case "Massive Mass Outbreak":
				shinyRolls += 12;
				break;
		}
		
		if (data.huntGame == "Lets Go Pikachu" || data.huntGame == "Lets Go Eevee" ||
			data.huntGame == "Scarlet" || data.huntGame == "Violet" || data.huntGame == "Legends Arceus")
		{
			shinyRolls += data.oddsBonus; // Add based on lure, research progress, or sandwich power
		}
		
		if (data.huntMethod != "Masuda Method" && data.huntMethod != "Breeding" && game.methodID == 13) // Shiny charm is weird in BDSP
		{
			shinyRolls = 1; // No shiny charm unless the player is doing masuda method
		}
		
		return odds / shinyRolls;
	}
	
	private int CalculateDexNav()
	{
		int forcedRate, multiplyRate;
		// Set the base values for different points in the combo
		if (data.combo == 100)
		{
			forcedRate = 15;
		}
		else if (data.combo == 50)
		{
			forcedRate = 10;
		}
		else if (data.combo % 5 == 0 && data.combo != 0)
		{
			forcedRate = 5;
		}
		else
		{
			forcedRate = 1;
		}
		// Shiny charm adds 2 to each of the base values
		if (data.charm)
		{
			forcedRate += 2;
		}
		
		// Multiply based on "target value" - depends on search level
		switch(data.oddsBonus)
		{
			case <= 16:
				multiplyRate = 1;
				break;
			case <= 33:
				multiplyRate = 2;
				break;
			case <= 50:
				multiplyRate = 3;
				break;
			case <= 66:
				multiplyRate = 4;
				break;
			case <= 83:
				multiplyRate = 5;
				break;
			case <= 100:
				multiplyRate = 6;
				break;
			case <= 150:
				multiplyRate = 7;
				break;
			case <= 200:
				multiplyRate = 8;
				break;
			case <= 300:
				multiplyRate = 9;
				break;
			case <= 400:
				multiplyRate = 10;
				break;
			case <= 500:
				multiplyRate = 11;
				break;
			case <= 600:
				multiplyRate = 12;
				break;
			case <= 700:
				multiplyRate = 13;
				break;
			case <= 800:
				multiplyRate = 14;
				break;
			case <= 900:
				multiplyRate = 15;
				break;
			default:
				multiplyRate = 16;
				break;
		}
		
		return forcedRate * multiplyRate;
	}
	
	private void SetResetButton()
	{
		// Gross if statement but this function is rarely called
		if (data.huntMethod == "Poke Radar" || data.huntMethod == "Chain Fishing" || data.huntMethod == "Dex Nav"
			|| data.huntMethod == "SOS Chain" || data.huntMethod == "Catch Combo" || (data.huntMethod == "Mass Outbreak"
			&& (data.huntGame == "Scarlet" || data.huntGame == "Violet")))
		{
			resetButton.Visible = true;
		}
		else
		{
			resetButton.Visible = false;
		}
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
		data.combo += data.incrementValue;
		if (data.huntMethod == "Dex Nav")
		{
			if (data.combo > 100)
			{
				data.combo -= 100; // Combo in this method loops after 100
			}
			data.oddsBonus += data.incrementValue; // oddsBonus increases for dex nav only
		}
		resetTimer = 0;
		PlayTick();
		UpdateCounterLabel();
		UpdateInfoLabel();
	}
	
	private void Decrement()
	{
		if (data.count > 0)
		{
			data.combo -= data.incrementValue;
			if (data.huntMethod == "Dex Nav")
			{
				if (data.combo < 0)
				{
					data.combo += 100; // Combo in this method loops, so decrementing must loop backwards
				}
				data.oddsBonus = Math.Max(data.oddsBonus - data.incrementValue, 0); // oddsBonus increases for dex nav only
			}
			else if (data.combo < 0)
			{
				data.combo = 0; // All other combos stop at 0
			}
			
			data.count -= data.incrementValue;
			PlayTick();
			UpdateCounterLabel();
			UpdateInfoLabel();
		}
	}
	
	private void ResetCombo()
	{
		data.combo = 0;
		UpdateInfoLabel();
	}
	
	private void PlayTick()
	{
		if (GlobalSettings.soundOn)
		{
			tickPlayer.Play();
		}
	}
	
	private void BackToMenu()
	{
		activeHunt = false;
		ClearSprites();
		EmitSignal("BackButtonPressed");
	}
	
	private void ClearSprites()
	{
		foreach (Sprite2D sprite in sprites)
		{
			sprite.Texture = null; // Reset textures so there aren't remaining sprites when opening the next hunt
		}
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
	
	private void CloseSettings(bool importantChange)
	{
		HuntSettings settingsMenu = GetNode<HuntSettings>("Settings");
		data = settingsMenu.settings;
		
		UpdateCounterLabel();
		UpdateInfoLabel();
		
		// Setting sprites is a long process, only do it if necessary
		if (importantChange)
		{
			SetSprites();
			SetResetButton();
			UpdateMainMenu();
		}
		
		if (data.pokemon.Count == 1)
		{
			sprites[0].Visible = data.showRegular;
			sprites[1].Visible = data.showShiny;
		}
		else
		{
			for (int i = 0; i < data.pokemon.Count; i++)
			{
				sprites[i].Visible = data.showShiny;
			}
		}
		
		RemoveChild(settingsMenu);
		settingsMenu.Cleanup();
		activeHunt = true;
	}
	
	private void OpenStats()
	{
		activeHunt = false;
		ActiveStats statsMenu = (ActiveStats)GD.Load<PackedScene>("res://Scenes/ActiveStats.tscn").Instantiate();
		AddChild(statsMenu);
		statsMenu.Name = "Stats";
		
		statsMenu.BackButtonPressed += CloseStats;
		statsMenu.InitializeStats(data);
	}
	
	private void CloseStats()
	{
		ActiveStats statsMenu = GetNode<ActiveStats>("Stats");
		RemoveChild(statsMenu);
		statsMenu.Cleanup();
		activeHunt = true;
	}
	
	private void ShinyFound()
	{
		activeHunt = false;
		FinishHunt finishMenu = (FinishHunt)GD.Load<PackedScene>("res://Scenes/FinishHunt.tscn").Instantiate();
		AddChild(finishMenu);
		finishMenu.SetInitialSettings(new HuntData(data));
		
		finishMenu.BackButtonPressed += CloseFinishScreen;
		finishMenu.FinishButtonPressed += HuntCompleted;
	}
	
	private void CloseFinishScreen()
	{
		FinishHunt finishMenu = GetNode<FinishHunt>("FinishHunt");
		RemoveChild(finishMenu);
		finishMenu.Cleanup();
		activeHunt = true;
	}
	
	private void HuntCompleted(string nickname, string ball, string gender)
	{
		FinishHunt finishMenu = GetNode<FinishHunt>("FinishHunt");
		data = finishMenu.data;
		RemoveChild(finishMenu);
		finishMenu.Cleanup();
		ClearSprites();
		activeHunt = false;
		EmitSignal("FinishHunt", nickname, ball, gender);
	}
	
	private void DeleteHunt()
	{
		HuntSettings settingsMenu = GetNode<HuntSettings>("Settings");
		RemoveChild(settingsMenu);
		settingsMenu.Cleanup();
		activeHunt = false;
		EmitSignal("DeleteSignal");
	}
	
	private void UpdateMainMenu()
	{
		EmitSignal("HuntChanged");
	}
	
	private void Save()
	{
		EmitSignal("RequestSave");
	}
	
	private void SizeChanged()
	{
		// Reset constants to fit the current size
		halfX = (int)(Size.X * halfXAnchor);
		quarterX = (int)(Size.X * quarterXAnchor);
		y = (int)(Size.Y * yAnchor);
		
		if (data.pokemon.Count > 0)
		{
			PositionSprites(data.pokemon.Count);
			ScaleSprites(data.pokemon.Count);
		}
	}
	
	// Destroy this UI element
	public void Cleanup()
	{
		QueueFree();
	}
}
