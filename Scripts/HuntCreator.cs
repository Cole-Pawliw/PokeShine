using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public partial class HuntCreator : Control
{
	Button gameSelect, pokemonSelect, methodSelect, routeSelect, startButton;
	CheckBox charmButton, bonusToggle;
	NumberInputField bonusAmount;
	Label bonusLabel;
	
	AvailabilityInformation dicts;
	
	string[] selections = {"", "", "", ""};
	public List<string> pokemonSelected;
	int optionMode = 0;
	bool screenVisible = true; // True when this screen is the only one visible to the user
	
	[Signal]
	public delegate void StartHuntEventHandler(string gameName, string method, string route, bool charm, int oddsBonus);
	[Signal]
	public delegate void BackButtonPressedEventHandler();
	
	public override void _Ready()
	{
		gameSelect = GetNode<Button>("GameSelect");
		pokemonSelect = GetNode<Button>("PokemonSelect");
		methodSelect = GetNode<Button>("MethodSelect");
		routeSelect = GetNode<Button>("RouteSelect");
		startButton = GetNode<Button>("StartButton");
		charmButton = GetNode<CheckBox>("CharmButton");
		
		bonusToggle = GetNode<CheckBox>("BonusToggle");
		bonusAmount = GetNode<NumberInputField>("BonusAmount");
		bonusLabel = GetNode<Label>("BonusLabel");
		
		dicts = GetNode<AvailabilityInformation>("AvailabilityInformation");
		pokemonSelected = new List<string>();
		
		SetColors();
	}
	
	public void SetColors()
	{
		TextureButton backButton;
		backButton = GetNode<TextureButton>("BackButton");
		backButton.TextureNormal = (Texture2D)GD.Load($"res://Assets/Buttons/{GameHuntInformation.colorMode}/back.png");
		
		ColorRect bg = GetNode<ColorRect>("Background");
		bg.Color = new Color(GameHuntInformation.backgrounds[GameHuntInformation.colorMode - 1]);
	}
	
	public override void _Notification(int what)
	{
		if (what == NotificationWMGoBackRequest && screenVisible)
		{
			BackToMenu();
		}
	}
	
	public void SetPreSelections(HuntData data)
	{
		Button startButton = GetNode<Button>("StartButton");
		startButton.Text = "Update Hunt";
		pokemonSelect.Disabled = false;
		methodSelect.Disabled = false;
		
		selections[0] = data.huntGame;
		selections[2] = data.huntMethod;
		
		if (selections[2] == "Random Encounter")
		{
			routeSelect.Disabled = false;
		}
		
		pokemonSelected = new List<string>(data.pokemon);
		dicts.SetRoutes(selections[0]); // Initialize the dictionary of routes for the selected game
		
		GameInfo info = GameHuntInformation.gameInfoDict[selections[0]]; // Get the code for the selected game
		if (info.methodID >= 6) // Shiny charm introduced in Black2/White2
		{
			charmButton.Disabled = false;
			charmButton.ButtonPressed = data.charm;
		}
		if (pokemonSelected.Count > 1)
		{
			selections[1] = "Various";
		}
		else
		{
			selections[1] = pokemonSelected[0];
		}
		UpdateButtonText();
		SetBonus(data.oddsBonus);
		startButton.Disabled = false;
		screenVisible = true;
	}
	
	private void GameSelectPressed()
	{
		optionMode = 1;
		OpenSelector();
	}
	
	private void PokemonSelectPressed()
	{
		if (selections[0] == "")
		{
			return;
		}
		optionMode = 2;
		OpenSelector();
	}
	
	private void MethodSelectPressed()
	{
		if (selections[0] == "")
		{
			return;
		}
		optionMode = 3;
		OpenSelector();
	}
	
	private void RouteSelectPressed()
	{
		if (selections[0] == "")
		{
			return;
		}
		optionMode = 4;
		OpenSelector();
	}
	
	private void OpenSelector()
	{
		OptionSelect selectScreen = (OptionSelect)GD.Load<PackedScene>("res://Scenes/OptionSelect.tscn").Instantiate();
		AddChild(selectScreen);
		List<string> itemList = new List<string>();
		bool multiSelect = false;
		screenVisible = false;
		
		if (optionMode == 1) // Send list of games
		{
			itemList = new List<string>(GameHuntInformation.gameInfoDict.Keys); // List of games
		} 
		else if (optionMode == 2) // Send list of pokemon
		{
			GameInfo info = GameHuntInformation.gameInfoDict[selections[0]]; // Get the code for the selected game
			int gameCode = info.methodID; // Get the associated folder name for the current game
			multiSelect = true;
			
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
		else if (optionMode == 4) // Send list of routes
		{
			foreach (KeyValuePair<string, string[]> location in dicts.pokemonRouteAvailabilityDict)
			{
				if (location.Value.Any())
				{
					itemList.Add(location.Key); // Only add the routes that have pokemon to random encounter
				}
			}
		}
		
		selectScreen.CreateList(itemList, multiSelect);
		if (optionMode == 2) // Send selected pokemon
		{
			selectScreen.SetPreSelections(pokemonSelected);
		}
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
		routeSelect.Visible = false;
		
		if (optionMode == 1 && selections[0] != selectedOption)
		{
			charmButton.Disabled = true;
			pokemonSelect.Disabled = false;
			methodSelect.Disabled = false;
			
			bonusToggle.Visible = false;
			bonusToggle.ButtonPressed = false;
			bonusAmount.Visible = false;
			bonusAmount.Text = "0";
			bonusLabel.Visible = false;
			
			dicts.SetRoutes(selectedOption); // Initialize the dictionary of routes for the selected game
			GameInfo info = GameHuntInformation.gameInfoDict[selectedOption]; // Get the code for the selected game
			
			if (selections[2] != "" && !dicts.methodAvailabilityDict[selections[2]][info.methodID])
			{
				selections[2] = "";
				if (pokemonSelected.Count > 1)
				{
					pokemonSelected.Clear();
					selections[1] = "";
				}
			}
			
			if (selections[3] != "" && !dicts.pokemonRouteAvailabilityDict.ContainsKey(selections[3]))
			{
				selections[3] = "";
			}
			else if (selections[3] != "")
			{
				pokemonSelected = new List<string>(dicts.pokemonRouteAvailabilityDict[selections[3]]);
			}
			
			for (int i = pokemonSelected.Count - 1; i >= 0; i--)
			{
				// Check if each pokemon is available in the new game
				if (!dicts.pokemonAvailabilityDict[pokemonSelected[i]][info.methodID])
				{
					// Remove the pokemon if it is not available
					pokemonSelected.Remove(pokemonSelected[i]);
				}
			}
			if (pokemonSelected.Count == 1)
			{
				selections[1] = pokemonSelected[0];
			}
			else if (pokemonSelected.Count == 0)
			{
				selections[1] = "";
			}
			
			if (info.methodID >= 6) // Shiny charm introduced in Black2/White2
			{
				charmButton.Disabled = false;
			}
			else
			{
				charmButton.ButtonPressed = false;
			}
		} 
		else if (optionMode == 2)
		{
			OptionSelect selectScreen = GetNode<OptionSelect>("OptionSelect");
			pokemonSelected = selectScreen.selectedValues;
		}
		else if (optionMode == 3)
		{
			if (selectedOption != "Random Encounter" && selectedOption != "Symbol Encounter" && pokemonSelected.Count > 1)
			{
				pokemonSelected.Clear();
				selections[1] = "";
			}
			
			if (selectedOption == "Random Encounter")
			{
				routeSelect.Disabled = false;
			}
			else
			{
				routeSelect.Disabled = true;
				selections[3] = "";
			}
		}
		else if (optionMode == 4)
		{
			pokemonSelected = new List<string>(dicts.pokemonRouteAvailabilityDict[selectedOption]);
			selections[1] = "Various";
		}
		
		selections[optionMode - 1] = selectedOption;
		
		if (optionMode == 3)
		{
			SetBonus();
		}
		
		UpdateButtonText();
		CloseSelector();
		optionMode = 0;
		
		// Make buttons visible again
		gameSelect.Visible = true;
		pokemonSelect.Visible = true;
		methodSelect.Visible = true;
		routeSelect.Visible = true;
		
		// Allow user to start the hunt
		if (selections[0] != "" && selections[1] != "" && selections[2] != "")
		{
			startButton.Disabled = false;
		}
		else
		{
			startButton.Disabled = true;
		}
	}
	
	private void CloseSelector()
	{
		OptionSelect selector = GetNode<OptionSelect>("OptionSelect");
		selector.Visible = false;
		screenVisible = true;
		RemoveChild(selector);
		selector.Cleanup();
	}
	
	private void UpdateButtonText()
	{
		gameSelect.Text = "Game:\n" + selections[0];
		pokemonSelect.Text = "Pokemon:\n" + selections[1];
		methodSelect.Text = "Method:\n" + selections[2];
		routeSelect.Text = "Route:\n" + selections[3];
	}
	
	private void SetBonus()
	{
		bonusToggle.Visible = false;
		bonusAmount.Visible = false;
		bonusLabel.Visible = false;
		
		bonusToggle.ButtonPressed = false;
		bonusAmount.Text = "0";
		
		if (selections[2] == "Shiny Family Breeding")
		{
			bonusToggle.Visible = true;
			bonusToggle.Text = "With Ditto";
		}
		else if ((selections[0] == "Lets Go Pikachu" || selections[0] == "Lets Go Eevee") &&
			(selections[2] == "Symbol Encounter" || selections[2] == "Catch Combo"))
		{
			bonusToggle.Visible = true;
			bonusToggle.Text = "Lure";
		}
		else if (selections[2] == "Dex Nav")
		{
			bonusAmount.Visible = true;
			bonusLabel.Visible = true;
			
			bonusAmount.MaxValue = 999;
			bonusLabel.Text = "Search Level";
		}
		else if ((selections[0] == "Scarlet" || selections[0] == "Violet") &&
				 (selections[2] == "Symbol Encounter" || selections[2] == "Mass Outbreak"))
		{
			bonusAmount.Visible = true;
			bonusLabel.Visible = true;
			
			bonusAmount.MaxValue = 3;
			bonusLabel.Text = "Sandwich Power";
		}
		else if (selections[0] == "Legends Arceus")
		{
			bonusAmount.Visible = true;
			bonusLabel.Visible = true;
			
			bonusAmount.MaxValue = 3;
			bonusLabel.Text = "Bonus Research Rolls";
		}
	}
	
	private void SetBonus(int bonusValue)
	{
		SetBonus();
		
		if (selections[2] == "Shiny Family Breeding" || selections[0] == "Lets Go Pikachu" || selections[0] == "Lets Go Eevee")
		{
			bonusToggle.ButtonPressed = bonusValue == 1 ? true : false;
		}
		else if (selections[2] == "Dex Nav" || selections[0] == "Scarlet" || selections[0] == "Violet" || selections[0] == "Legends Arceus")
		{
			bonusAmount.Text = $"{bonusValue}";
		}
	}
	
	private void EmitStartHunt()
	{
		// Only emit the signal if all selections have been made
		if (selections[0] != "" && selections[1] != "" && selections[2] != "")
		{
			screenVisible = false;
			int oddsBonus;
			if (bonusToggle.ButtonPressed)
			{
				oddsBonus = 1;
			}
			else
			{
				oddsBonus = bonusAmount.Value; // This value is set to 0 whenever not in use
			}
			EmitSignal("StartHunt", selections[0], selections[2], selections[3], charmButton.ButtonPressed, oddsBonus);
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
