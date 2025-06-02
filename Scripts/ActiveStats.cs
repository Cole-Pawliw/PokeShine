using Godot;
using System;

public partial class ActiveStats : Control
{
	Label inputLabel;
	NumberInputField encounterInput;
	HuntData data;
	bool screenVisible = false;
	string constantPortion = "";
	
	[Signal]
	public delegate void BackButtonPressedEventHandler();
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		inputLabel = GetNode<Label>("ScrollContainer/BoxContainer/InputLabel");
		encounterInput = GetNode<NumberInputField>("ScrollContainer/BoxContainer/NumberInputField");
		screenVisible = true;
		SetColors();
	}
	
	public void SetColors()
	{
		TextureButton backButton;
		backButton = GetNode<TextureButton>("BackButton");
		backButton.TextureNormal = (Texture2D)GD.Load($"res://Assets/Buttons/{GlobalSettings.colorMode}/back.png");
		
		ColorRect bg = GetNode<ColorRect>("Background");
		bg.Color = new Color(GlobalSettings.backgrounds[GlobalSettings.colorMode - 1]);
	}
	
	public override void _Notification(int what)
	{
		if (what == NotificationWMGoBackRequest && screenVisible)
		{
			EmitBackButtonPressed();
		}
	}
	
	public void InitializeStats(HuntData hunt)
	{
		data = hunt;
		SetStats();
		
		// Set Buffer size
		int sizeY = (int)Size.Y / 4;
		GetNode<Label>("ScrollContainer/BoxContainer/Buffer").CustomMinimumSize = new Vector2(0, sizeY);
	}
	
	private void SetStats()
	{
		Label label = GetNode<Label>("ScrollContainer/BoxContainer/Label");
		float baseOdds = CalculateBaseOdds(data.combo);
		int encounterTime = data.count > 0 ? data.timeSpent / data.count : data.timeSpent;
		int fullOdds = (int)Math.Ceiling(baseOdds);
		fullOdds += fullOdds > 0 ? fullOdds * (data.count / fullOdds) : 0; // Find the next multiple of fullOdds
		float luck = (float)Math.Round(CalculateVariableLuck(data.count) * 100, 2); // Round the luck percent to 2 decimal places
		
		string labelInfo = $"Pokemon seen: {data.count}\nTime Spent: {CalculateTimeString()}\nCurrent Odds: 1/{Math.Round(baseOdds, 2)}\n\n";
		labelInfo += $"You see a pokemon every {encounterTime}s on average. ";
		labelInfo += $"With {data.incrementValue} pokemon seen at a time, that means each increment takes {encounterTime * data.incrementValue}s.\n\n";
		labelInfo += $"At the current odds, it will take you {encounterTime * (fullOdds - data.count) / 60}mins to reach {fullOdds} pokemon seen.\n\n";
		labelInfo += $"The chances of you seeing a shiny by now are {Math.Round(100 - luck, 2)}%, so you are in the {luck}th percentile of unlucky hunts.\n";
		
		label.Text = labelInfo;
		EncounterInputUpdated();
	}
	
	private void EncounterInputUpdated()
	{
		string newLabelText = constantPortion;
		float luck = CalculateVariableLuck(encounterInput.Value) * 100;
		newLabelText += $"The odds of getting a shiny after {encounterInput.Value} encounters is: {Math.Round(100 - luck, 2)}%.";
		inputLabel.Text = newLabelText;
	}
	
	private string CalculateTimeString()
	{
		int hours, minutes, seconds;
		int fullTime = data.timeSpent;
		hours = fullTime / 3600;
		fullTime %= 3600; // Remove the hours to count seconds and minutes
		minutes = fullTime / 60;
		seconds = fullTime % 60;
	
		return $"{hours:00}:{minutes:00}:{seconds:00}"; // :00 pads 2 zeros to everything
	}
	
	private float CalculateVariableLuck(int encounters)
	{
		float variableOdds = 1;
		
		switch (data.huntMethod)
		{
			case "Poke Radar":
				for (int i = 0; i < 40 && i < encounters; i++) // Calculate new odds for each of the first 40 encounters
				{
					variableOdds *= CalculateBaseOdds(i); // Only one encounter per odds, no need for CalculateLuck()
				}
				if (encounters > 40)
				{
					variableOdds *= CalculateLuck(encounters - 40, CalculateBaseOdds(40)); // Calculate odds with the remaining seen
				}
				return variableOdds;
			case "Chain Fishing":
				for (int i = 0; i < 20 && i < encounters; i++) // Calculate new odds for each of the first 20 encounters
				{
					variableOdds *= CalculateBaseOdds(i); // Only one encounter per odds, no need for CalculateLuck()
				}
				if (encounters > 20)
				{
					variableOdds *= CalculateLuck(encounters - 20, CalculateBaseOdds(20)); // Calculate odds with the remaining seen
				}
				return variableOdds;
			case "Dex Nav": // Complicated odds, base odds are the most common so assume that for calculations
				return CalculateLuck(encounters, CalculateBaseOdds(1));
			case "SOS Chain":
			case "Catch Combo":
				// Shiny rolls increase by 4 after chains of 11, 21, and 31
				for (int i = 10; i <= 30; i += 10) // Find the odds for 10, 20, and 30 encounters
				{
					if (encounters < i) // encounters < 30, wrap up with the excess and return
					{
						return variableOdds * CalculateLuck(encounters + 10 - i, CalculateBaseOdds(i));
					}
					else // Multiply the next 10 encounters
					{
						variableOdds *= CalculateLuck(10, CalculateBaseOdds(i));
					}
				}
				// Subtract to prevent counting the first 30 twice
				return variableOdds * CalculateLuck(encounters - 30, CalculateBaseOdds(encounters));
			case "Mass Outbreak":
				if (data.huntGame == "Legends Arceus") // Static odds for PLA
				{
					return CalculateLuck(encounters, CalculateBaseOdds(encounters));
				}
				// Non static odds for SV
				// Shiny rolls increase by 1 after chains of 31 and 61
				for (int i = 30; i <= 60; i += 30) // Find the odds for 30 and 60 encounters
				{
					if (encounters < i) // encounters < 60, wrap up with the excess and return
					{
						return variableOdds * CalculateLuck(encounters + 30 - i, CalculateBaseOdds(i));
					}
					else // Multiply the next 10 encounters
					{
						variableOdds *= CalculateLuck(30, CalculateBaseOdds(i));
					}
				}
				// Subtract to prevent counting the first 60 twice
				return variableOdds * CalculateLuck(encounters - 60, CalculateBaseOdds(encounters));
			default: // When the shiny odds are static
				return CalculateLuck(encounters, CalculateBaseOdds(encounters));
		}
	}
	
	private float CalculateBaseOdds(int encounters)
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
				chain = Math.Min(encounters, 40);
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
				chain = Math.Min(encounters, 20);
				shinyRolls += 2 * chain;
				break;
			case "Dex Nav":
				shinyRolls = 1; // Charm is already factored in with these calculations
				odds = 10000f / (float)CalculateDexNav(encounters); // Dex nav uses a target value compared to 10000
				break;
			case "SOS Chain":
				// Shiny rolls increase by 4 after chains of 11, 21, and 31
				for (int i = 1; i < 4 && i * 10 + 1 <= encounters; i++)
				{
					shinyRolls += 4;
				}
				break;
			case "Ultra Wormhole":
				odds = 0; // No odds indicates there are too many variables to accurately track odds
				break;
			case "Catch Combo":
				// If statements are required here instead of a loop because of varying increments
				if (encounters > 10)
				{
					shinyRolls += 3;
				}
				if (encounters > 20)
				{
					shinyRolls += 4;
				}
				if (encounters > 30)
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
					if (encounters >= 30)
					{
						shinyRolls++;
					}
					if (encounters >= 60)
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
	
	private int CalculateDexNav(int encounters)
	{
		int forcedRate, multiplyRate;
		// Set the base values for different points in the combo
		if (encounters == 100)
		{
			forcedRate = 15;
		}
		else if (encounters == 50)
		{
			forcedRate = 10;
		}
		else if (encounters % 5 == 0 && encounters != 0)
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
	
	// This function simply calculates ((odds-1)/odds)^encounters
	// That is the odds of not finding a shiny when following the binomial distribution
	// 1 - this number is the odds of having a shiny in that many encounters, but could be more than 1 shiny
	private float CalculateLuck(int encounters, float odds)
	{
		if (odds == 0f)
		{
			return 0; // Some odds can't be calculated, so odds are always 0
		}
		float numerator = odds - 1;
		return (float)Math.Pow(numerator / odds, encounters);
	}
	
	private void EmitBackButtonPressed()
	{
		screenVisible = false;
		EmitSignal("BackButtonPressed");
	}
	
	// Destroy this UI element
	public void Cleanup()
	{
		QueueFree();
	}
}
