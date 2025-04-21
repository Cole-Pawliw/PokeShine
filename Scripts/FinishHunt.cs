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
	Button routeSelect;
	Button genderSelect;
	Button ballSelect;
	CheckBox charmButton;
	Label info;
	TextEdit nickname;
	TextureButton finishButton;
	
	AvailabilityInformation dicts;
	
	public HuntData data;
	int optionMode = 0;
	string[] altSelections = {"", ""};
	
	[Signal]
	public delegate void BackButtonPressedEventHandler();
	[Signal]
	public delegate void FinishButtonPressedEventHandler(string nickname, string ball, string gender);
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		pokemonSelect = GetNode<Button>("PokemonSelect");
		gameSelect = GetNode<Button>("GameSelect");
		methodSelect = GetNode<Button>("MethodSelect");
		routeSelect = GetNode<Button>("RouteSelect");
		genderSelect = GetNode<Button>("GenderSelect");
		ballSelect = GetNode<Button>("BallSelect");
		charmButton = GetNode<CheckBox>("CharmButton");
		info = GetNode<Label>("Info");
		nickname = GetNode<TextEdit>("Nickname");
		finishButton = GetNode<TextureButton>("FinishButton");
		
		dicts = GetNode<AvailabilityInformation>("AvailabilityInformation");
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
		string date = data.startDate.Split('T')[0];
		
		GameInfo gameInfo = GameHuntInformation.gameInfoDict[data.huntGame]; // Get the code for the selected game
		if (gameInfo.methodID >= 5) // Shiny charm introduced in Black2/White2
		{
			charmButton.Visible = true;
		}
		
		info.Text = $"{data.count}\n{timerInHourFormat}\n{date}";
		
		if (data.pokemon.Count > 1)
		{
			finishButton.Disabled = true;
		}
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
	
	private void RouteSelectPressed()
	{
		optionMode = 6;
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
			foreach(KeyValuePair<string, bool[]> pokemon in dicts.pokemonAvailabilityDict)
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
			foreach(KeyValuePair<string, bool[]> method in dicts.methodAvailabilityDict)
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
			foreach(KeyValuePair<string, bool[]> ball in dicts.ballAvailabilityDict)
			{
				if (ball.Value[gameCode] == true)
				{
					itemList.Add(ball.Key);
				}
			}
		}
		else if (optionMode == 6) // Send list of routes
		{
			dicts.SetRoutes(data.huntGame); // Initialize the dictionary of routes
			itemList = new List<string>(dicts.pokemonRouteAvailabilityDict.Keys);
		}
		
		selectScreen.CreateList(itemList, false);
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
			finishButton.Disabled = true;
			
			// Reset other selections so non existent options can't be selected
			data.pokemon.Clear();
			data.huntMethod = "";
			altSelections[0] = "";
			altSelections[1] = "";
			data.charm = false;
			// Make sure user cannot enable shiny charm then change games
			charmButton.ButtonPressed = false;
			
			data.huntGame = selectedOption;
			GameInfo gameInfo = GameHuntInformation.gameInfoDict[data.huntGame]; // Get the code for the selected game
			if (gameInfo.methodID >= 6) // Shiny charm introduced in Black2/White2
			{
				charmButton.Visible = true;
			}
		}
		else if (optionMode == 2)
		{
			data.pokemon.Clear();
			data.pokemon.Add(selectedOption);
			finishButton.Disabled = false;
		}
		else if (optionMode == 3)
		{
			data.huntMethod = selectedOption;
		}
		else if (optionMode == 4)
		{
			altSelections[1] = selectedOption;
		}
		else if (optionMode == 5)
		{
			altSelections[0] = selectedOption;
		}
		else if (optionMode == 6)
		{
			data.huntRoute = selectedOption;
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
		routeSelect.Text = "Route:\n" + data.huntRoute;
		genderSelect.Text = "Gender:\n" + altSelections[1];
		ballSelect.Text = "Ball:\n" + altSelections[0];
		charmButton.ButtonPressed = data.charm;
		
		Sprite2D shiny = GetNode<Sprite2D>("PokemonSelect/ShinySprite");
		if (data.pokemon.Count > 1 || data.pokemon[0] == "")
		{
			// If no pokemon is selected, remove the texture
			shiny.Texture = null;
			pokemonSelect.Text = "Found Pokemon:";
		}
		else
		{
			// Else set the appropriate texture
			shiny.Texture = (Texture2D)GD.Load($"res://Sprites/{data.huntFolder}/Shiny/{data.pokemon[0]}.png");
			pokemonSelect.Text = "";
		}
	}
	
	private void BackToHunt()
	{
		EmitSignal("BackButtonPressed");
	}
	
	private void ConfirmFinish()
	{
		// A pokemon name is needed to finish the hunt, everything else is optional
		if (data.pokemon.Count > 1 || data.pokemon[0] == "")
		{
			return;
		}
		
		data.isComplete = true;
		EmitSignal("FinishButtonPressed", nickname.Text, altSelections[1], altSelections[0]);
	}
	
	public void Cleanup()
	{
		QueueFree();
	}
}
