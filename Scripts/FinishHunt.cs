using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public partial class FinishHunt : Control
{
	Button pokemonSelect;
	Button gameSelect;
	Button methodSelect;
	Button genderSelect;
	Button ballSelect;
	CheckBox charmButton;
	Label info;
	
	public HuntData data;
	int optionMode = 0;
	
	[Signal]
	public delegate void BackButtonPressedEventHandler();
	[Signal]
	public delegate void FinishButtonPressedEventHandler();
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		pokemonSelect = GetNode<Button>("PokemonSelect");
		gameSelect = GetNode<Button>("GameSelect");
		methodSelect = GetNode<Button>("MethodSelect");
		genderSelect = GetNode<Button>("GenderSelect");
		ballSelect = GetNode<Button>("BallSelect");
		charmButton = GetNode<CheckBox>("CharmButton");
		info = GetNode<Label>("Info");
	}
	
	public void SetInitialSettings(HuntData hunt)
	{
		data = hunt;
		data.isComplete = true;
		
		UpdateButtons();
		
		// Find the time spent in the hunt
		int fullTime = data.timeSpent;
		int hours = fullTime / 3600;
		fullTime %= 3600; // Remove the hours to count seconds and minutes
		int minutes = fullTime / 60;
		int seconds = fullTime % 60;
		string timerInHourFormat = $"{hours:00}:{minutes:00}:{seconds:00}"; // :00 pads 2 zeros to everything
		
		string endDT = Time.GetDatetimeStringFromSystem();
		string endDate = endDT.Split('T')[0];
		data.endDate = endDT;
		
		GameInfo gameInfo = GameHuntInformation.gameInfoDict[data.huntGame]; // Get the code for the selected game
		if (gameInfo.methodID >= 5) // Shiny charm introduced in Black2/White2
		{
			charmButton.Visible = true;
		}
		
		info.Text = $"{data.count}\n{timerInHourFormat}\n{endDate}";
	}
	
	private void GameSelectPressed()
	{
		optionMode = 1;
		OpenSelector();
	}
	
	private void PokemonSelectPressed()
	{
		optionMode = 2;
		OpenSelector();
	}
	
	private void MethodSelectPressed()
	{
		optionMode = 3;
		OpenSelector();
	}
	
	private void GenderSelectPressed()
	{
		optionMode = 4;
		OpenSelector();
	}
	
	private void BallSelectPressed()
	{
		optionMode = 5;
		OpenSelector();
	}
	
	private void CharmButtonToggled()
	{
		data.charm = charmButton.ButtonPressed;
	}
	
	private void OpenSelector()
	{
		OptionSelect selectScreen = (OptionSelect)GD.Load<PackedScene>("res://Scenes/OptionSelect.tscn").Instantiate();
		AddChild(selectScreen);
		List<string> itemList = new List<string>();
		
		if (optionMode == 1) // Send list of games
		{
			itemList = new List<string>(GameHuntInformation.gameInfoDict.Keys); // List of games
		} 
		else if (optionMode == 2) // Send list of pokemon
		{
			GameInfo info = GameHuntInformation.gameInfoDict[data.huntGame]; // Get the code for the selected game
			int gameCode = info.methodID; // Get the associated folder name for the current game
			
			// If the pokemon is available in the selected game, add it to itemList
			foreach(KeyValuePair<string, bool[]> pokemon in GameHuntInformation.pokemonAvailabilityDict)
			{
				if (pokemon.Value[gameCode] == true)
				{
					itemList.Add(pokemon.Key);
				}
			}
		}
		else if (optionMode == 3) // Send list of methods
		{
			GameInfo info = GameHuntInformation.gameInfoDict[data.huntGame]; // Get the code for the selected game
			int gameCode = info.methodID; // Get the associated folder name for the current game
			
			// If the method is available in the selected game, add it to itemList
			foreach(KeyValuePair<string, bool[]> method in GameHuntInformation.methodAvailabilityDict)
			{
				if (method.Value[gameCode] == true)
				{
					itemList.Add(method.Key);
				}
			}
		}
		else if (optionMode == 4) // Send list of genders
		{
			itemList = new List<string> {"Male", "Female", "Genderless"}; // List of genders
		}
		else if (optionMode == 5) // Send list of poke balls
		{
			GameInfo info = GameHuntInformation.gameInfoDict[data.huntGame]; // Get the code for the selected game
			int gameCode = info.methodID; // Get the associated folder name for the current game
			
			// If the ball is available in the selected game, add it to itemList
			foreach(KeyValuePair<string, bool[]> ball in GameHuntInformation.ballAvailabilityDict)
			{
				if (ball.Value[gameCode] == true)
				{
					itemList.Add(ball.Key);
				}
			}
		}
		
		selectScreen.CreateList(itemList);
		selectScreen.CloseMenu += UpdateSelection;
	}
	
	private void UpdateSelection(string selectedOption)
	{
		if (selectedOption == "")
		{
			// Back button pressed, close menu without changing anything
			CloseSelector();
			return;
		}
		
		// Set all options invisible before updating them
		gameSelect.Visible = false;
		pokemonSelect.Visible = false;
		methodSelect.Visible = false;
		genderSelect.Visible = false;
		ballSelect.Visible = false;
		
		if (optionMode == 1 && data.huntGame != selectedOption)
		{
			charmButton.Visible = false;
			
			// Reset other selections so non existent options can't be selected
			data.pokemonName = "";
			data.huntMethod = "";
			data.capturedGender = "";
			data.capturedBall = "";
			data.charm = false;
			// Make sure user cannot enable shiny charm then change games
			charmButton.ButtonPressed = false;
			
			data.huntGame = selectedOption;
			GameInfo gameInfo = GameHuntInformation.gameInfoDict[data.huntGame]; // Get the code for the selected game
			if (gameInfo.methodID >= 5) // Shiny charm introduced in Black2/White2
			{
				charmButton.Visible = true;
			}
			
		}
		else if (optionMode == 2)
		{
			data.pokemonName = selectedOption;
		}
		else if (optionMode == 3)
		{
			data.huntMethod = selectedOption;
		}
		else if (optionMode == 4)
		{
			data.capturedGender = selectedOption;
		}
		else if (optionMode == 5)
		{
			data.capturedBall = selectedOption;
		}
		
		UpdateButtons();
		CloseSelector();
		optionMode = 0;
		
		// Make buttons visible again
		gameSelect.Visible = true;
		pokemonSelect.Visible = true;
		methodSelect.Visible = true;
		genderSelect.Visible = true;
		ballSelect.Visible = true;
	}
	
	private void CloseSelector()
	{
		OptionSelect selector = GetNode<OptionSelect>("OptionSelect");
		selector.Visible = false;
		selector.Cleanup();
	}
	
	private void UpdateButtons()
	{
		gameSelect.Text = "Game:\n" + data.huntGame;
		methodSelect.Text = "Method:\n" + data.huntMethod;
		genderSelect.Text = "Gender:\n" + data.capturedGender;
		ballSelect.Text = "Ball:\n" + data.capturedBall;
		charmButton.ButtonPressed = data.charm;
		
		Sprite2D shiny = GetNode<Sprite2D>("PokemonSelect/ShinySprite");
		if (data.pokemonName == "")
		{
			// If no pokemon is selected, remove the texture
			shiny.Texture = null;
		}
		else
		{
			// Else set the appropriate texture
			shiny.Texture = (Texture2D)GD.Load($"res://Sprites/{data.huntFolder}/Shiny/{data.pokemonName}.png");
		}
	}
	
	private void BackToHunt()
	{
		EmitSignal("BackButtonPressed");
	}
	
	private void ConfirmFinish()
	{
		// A pokemon name is needed to finish the hunt, everything else is optional
		if (data.pokemonName == "") {
			return;
		}
		data.isComplete = true;
		EmitSignal("FinishButtonPressed");
	}
	
	public void Cleanup()
	{
		QueueFree();
	}
}
