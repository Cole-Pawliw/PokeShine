using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public partial class HuntCreator : Control
{
	Button gameSelect;
	Button pokemonSelect;
	Button methodSelect;
	CheckBox charmButton;
	
	AvailabilityInformation dicts;
	
	string[] selections = {"", "", ""};
	int optionMode = 0;
	
	[Signal]
	public delegate void StartHuntEventHandler(string gameName, string pokemonName, string method, bool charm);
	[Signal]
	public delegate void BackButtonPressedEventHandler();
	
	public override void _Ready()
	{
		gameSelect = GetNode<Button>("GameSelect");
		pokemonSelect = GetNode<Button>("PokemonSelect");
		methodSelect = GetNode<Button>("MethodSelect");
		charmButton = GetNode<CheckBox>("CharmButton");
		
		dicts = GetNode<AvailabilityInformation>("AvailabilityInformation");
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
		
		if (optionMode == 1 && selections[0] != selectedOption)
		{
			charmButton.Visible = false;
			
			// Reset other selections so non existent options can't be selected
			selections[1] = "";
			selections[2] = "";
			// Make sure user cannot enable shiny charm then change games
			charmButton.ButtonPressed = false;
			
			
			GameInfo info = GameHuntInformation.gameInfoDict[selectedOption]; // Get the code for the selected game
			if (info.methodID >= 5) // Shiny charm introduced in Black2/White2
			{
				charmButton.Visible = true;
			}
		}
		
		selections[optionMode - 1] = selectedOption;
		
		UpdateButtonText();
		CloseSelector();
		optionMode = 0;
		
		// Make buttons visible again
		gameSelect.Visible = true;
		pokemonSelect.Visible = true;
		methodSelect.Visible = true;
	}
	
	private void CloseSelector()
	{
		OptionSelect selector = GetNode<OptionSelect>("OptionSelect");
		selector.Visible = false;
		selector.Cleanup();
	}
	
	private void UpdateButtonText()
	{
		gameSelect.Text = "Game:\n" + selections[0];
		pokemonSelect.Text = "Pokemon:\n" + selections[1];
		methodSelect.Text = "Method:\n" + selections[2];
	}
	
	private void EmitStartHunt()
	{
		// Only emit the signal if all selections have been made
		if (selections[0] != "" && selections[1] != "" && selections[2] != "")
		{
			EmitSignal("StartHunt", selections[0], selections[1], selections[2], charmButton.ButtonPressed);
		}
	}
	
	public void BackToMenu()
	{
		EmitSignal("BackButtonPressed");
	}
	
	// Destroy this UI element
	public void Cleanup()
	{
		QueueFree();
	}
}
