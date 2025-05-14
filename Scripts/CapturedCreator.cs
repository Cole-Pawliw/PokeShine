using Godot;
using System;
using System.Collections.Generic;
using System.IO;

public partial class CapturedCreator : Control
{
	Button pokemonSelect, gameSelect, methodSelect, routeSelect, genderSelect, ballSelect;
	public CheckBox charmButton;
	public DateInputField startDate, endDate;
	public TimeInputField timer;
	public NumberInputField counter;
	public LineEdit nickname;
	Button addButton;
	
	AvailabilityInformation dicts;
	
	public string[] selections = {"", "", "", "", "", ""};
	int optionMode = 0;
	bool screenVisible = true; // True when this screen is the only one visible to the user
	
	[Signal]
	public delegate void AddHuntEventHandler();
	[Signal]
	public delegate void BackButtonPressedEventHandler();
	
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
		startDate = GetNode<DateInputField>("StartDate");
		endDate = GetNode<DateInputField>("EndDate");
		nickname = GetNode<LineEdit>("Nickname");
		counter = GetNode<NumberInputField>("CounterValue");
		timer = GetNode<TimeInputField>("TimerValue");
		addButton = GetNode<Button>("AddButton");
		
		dicts = GetNode<AvailabilityInformation>("AvailabilityInformation");
		
		// Set default start and end dates so the user knows what format they use
		// Current date is an easy default
		string date = Time.GetDatetimeStringFromSystem().Split('T')[0];
		startDate.UpdateDate(date);
		endDate.UpdateDate(date);
	}
	
	public override void _Notification(int what)
	{
		if (what == NotificationWMGoBackRequest && screenVisible)
		{
			BackToMenu();
		}
	}
	
	public void SetPreSelections(CapturedData data)
	{
		Button addButton = GetNode<Button>("AddButton");
		addButton.Text = "Edit Hunt";
		pokemonSelect.Visible = true;
		methodSelect.Visible = true;
		routeSelect.Visible = true;
		ballSelect.Visible = true;
		genderSelect.Visible = true;
		
		selections[0] = data.huntGame;
		selections[1] = data.pokemon;
		selections[2] = data.huntMethod;
		selections[3] = data.capturedGender;
		selections[4] = data.capturedBall;
		selections[5] = data.huntRoute;
		charmButton.ButtonPressed = data.charm;
		// startDate.Text = data.startDate;
		// endDate.Text = data.endDate;
		nickname.Text = data.nickname;
		counter.Text = $"{data.count}";
		timer.UpdateTime(data.timeSpent);
		
		startDate.UpdateDate(data.startDate);
		endDate.UpdateDate(data.endDate);
		
		GameInfo info = GameHuntInformation.gameInfoDict[selections[0]]; // Get the code for the selected game
		if (info.methodID >= 6) // Shiny charm introduced in Black2/White2
		{
			charmButton.Visible = true;
			charmButton.ButtonPressed = data.charm;
		}
		UpdateButtons();
		pokemonSelect.Disabled = false;
		methodSelect.Disabled = false;
		routeSelect.Disabled = false;
		addButton.Disabled = false;
		screenVisible = true;
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
			GameInfo info = GameHuntInformation.gameInfoDict[selections[0]]; // Get the code for the selected game
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
			GameInfo info = GameHuntInformation.gameInfoDict[selections[0]]; // Get the code for the selected game
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
			GameInfo info = GameHuntInformation.gameInfoDict[selections[0]]; // Get the code for the selected game
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
			dicts.SetRoutes(selections[0]); // Initialize the dictionary of routes
			itemList = new List<string>(dicts.pokemonRouteAvailabilityDict.Keys);
		}
		
		selectScreen.CreateList(itemList, false);
		selectScreen.CloseMenu += UpdateSelection;
		screenVisible = false;
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
		routeSelect.Visible = false;
		genderSelect.Visible = false;
		ballSelect.Visible = false;
		
		if (optionMode == 1 && selections[0] != selectedOption)
		{
			charmButton.Visible = false;
			addButton.Disabled = true;
			
			pokemonSelect.Disabled = false;
			methodSelect.Disabled = false;
			routeSelect.Disabled = false;
			
			// Reset other selections so non existent options can't be selected
			selections[1] = "";
			selections[2] = "";
			selections[5] = "";
			// Make sure user cannot enable shiny charm then change games
			charmButton.ButtonPressed = false;
			
			GameInfo gameInfo = GameHuntInformation.gameInfoDict[selectedOption]; // Get the code for the selected game
			if (gameInfo.methodID >= 6) // Shiny charm introduced in Black2/White2
			{
				charmButton.Visible = true;
			}
		}
		else if (optionMode == 2)
		{
			addButton.Disabled = false;
		}
		
		if (optionMode != 0)
		{
			selections[optionMode - 1] = selectedOption;
		}
		
		UpdateButtons();
		CloseSelector();
		optionMode = 0;
		
		// Make buttons visible again
		gameSelect.Visible = true;
		pokemonSelect.Visible = true;
		methodSelect.Visible = true;
		routeSelect.Visible = true;
		genderSelect.Visible = true;
		ballSelect.Visible = true;
		
		// Allow user to start the hunt
		if (selections[0] != "" && selections[1] != "")
		{
			addButton.Disabled = false;
		}
		else
		{
			addButton.Disabled = true;
		}
	}
	
	private void CloseSelector()
	{
		OptionSelect selector = GetNode<OptionSelect>("OptionSelect");
		selector.Visible = false;
		screenVisible = true;
		selector.Cleanup();
	}
	
	private void UpdateButtons()
	{
		gameSelect.Text = "Game:\n" + selections[0];
		pokemonSelect.Text = "Pokemon:\n" + selections[1];
		methodSelect.Text = "Method:\n" + selections[2];
		genderSelect.Text = "Gender:\n" + selections[3];
		ballSelect.Text = "Ball:\n" + selections[4];
		routeSelect.Text = "Route:\n" + selections[5];
	}
	
	private void EmitStartHunt()
	{
		screenVisible = false;
		// Only emit the signal if all selections have been made
		if (selections[0] != "" && selections[1] != "")
		{
			EmitSignal("AddHunt");
		}
	}
	
	public void BackToMenu()
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
