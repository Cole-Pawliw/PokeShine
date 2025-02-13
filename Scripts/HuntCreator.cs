using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;

public class GameInfo
{
	public GameInfo()
	{
		methodID = 0;
		spritesFolder = "RS";
	}
	public GameInfo(int m, string folder)
	{
		methodID = m;
		spritesFolder = folder;
	}
	
	public int methodID { get; set; } // Stores the index to be used in methodAvailabilityDict
	public string spritesFolder { get; set; } // Stores the name of a sprite folder
}

public partial class HuntCreator : Control
{
	ItemList gameList;
	ItemList pokemonList;
	ItemList methodList;
	
	Button gameSelect;
	Button pokemonSelect;
	Button methodSelect;
	CheckBox charmButton;
	
	string selectedGame = "";
	string selectedPokemon = "";
	string selectedMethod = "";
	int optionMode = 0;
	
	string[] games = {"Ruby", "Sapphire", "Emerald", "Fire Red", "Leaf Green"};
	
	// This dictionary maps the names of pokemon games to corresponding information
	// The int corresponds with an index to be used in methodAvailabilityDict
	// The string indicates the folder name used to access the sprites for this game
	Dictionary<string, GameInfo> gameInfoDict;
	
	// Stores the method name as the key and an array of booleans indicating if it is in a specific game
	Dictionary<string, bool[]> methodAvailabilityDict;
	
	[Signal]
	public delegate void StartHuntEventHandler(string gameName, string pokemonName, string method, bool charm);
	
	public override void _Ready()
	{
		gameList = GetNode<ItemList>("GameListContainer/GameList");
		pokemonList = GetNode<ItemList>("PokemonListContainer/PokemonList");
		methodList = GetNode<ItemList>("MethodListContainer/MethodList");
		charmButton = GetNode<CheckBox>("CharmButton");
		
		gameSelect = GetNode<Button>("GameSelect");
		pokemonSelect = GetNode<Button>("PokemonSelect");
		methodSelect = GetNode<Button>("MethodSelect");
		
		// Stores the repeated GameInfo objects for re-use in gameInfoDict
		GameInfo[] infoStorage = { new GameInfo(0, "RS"), new GameInfo(1, "FL"), new GameInfo(2, "DP"), 
		new GameInfo(3, "HS"), new GameInfo(4, "BW"), new GameInfo(5, "BW"), new GameInfo(6, "Models"),
		new GameInfo(7, "Models"), new GameInfo(8, "Models"), new GameInfo(9, "Models") };
		
		string path = ProjectSettings.GlobalizePath("user://");
		string infoJson = LoadJsonFromFile(path, "GameInfo.json");
		string methodJson = LoadJsonFromFile(path, "MethodAvailability.json");
		
		var options = new JsonSerializerOptions
		{
			IncludeFields = true,
		};
		
		gameInfoDict = JsonSerializer.Deserialize<Dictionary<string, GameInfo>>(infoJson, options)!;
		methodAvailabilityDict = JsonSerializer.Deserialize<Dictionary<string, bool[]>>(methodJson, options)!;
		
		/*
		// Initialize the gameInfoDict dictionary
		gameInfoDict = new Dictionary<string, GameInfo>(){
		{"Ruby", infoStorage[0]}, {"Sapphire", infoStorage[0]}, {"Emerald", infoStorage[0]},
		{"Fire Red", infoStorage[1]}, {"Leaf Green", infoStorage[1]},
		{"Diamond", infoStorage[2]}, {"Pearl", infoStorage[2]}, {"Platinum", infoStorage[2]},
		{"Heart Gold", infoStorage[3]}, {"Soul Silver", infoStorage[3]},
		{"Black", infoStorage[4]}, {"White", infoStorage[4]},
		{"Black 2", infoStorage[5]}, {"White 2", infoStorage[5]},
		{"X", infoStorage[6]}, {"Y", infoStorage[6]},
		{"Alpha Sapphire", infoStorage[7]}, {"Omega Ruby", infoStorage[7]},
		{"Sun", infoStorage[8]}, {"Moon", infoStorage[8]},
		{"Ultra Sun", infoStorage[9]}, {"Ultra Moon", infoStorage[9]} };
		
		// Initialize the methodAvailabilityDict dictionary
		methodAvailabilityDict = new Dictionary<string, bool[]>(){
		{"Soft Reset", new[]{true, true, true, true, true, true, true, true, true, true}},
		{"Random Encounter", new[]{true, true, true, true, true, true, true, true, true, true}},
		{"Breeding", new[]{true, true, true, true, true, true, true, true, true, true}},
		{"Masuda Method", new[]{false, false, true, true, true, true, true, true, true, true}},
		{"Purchase/Revive", new[]{true, true, true, true, true, true, true, true, true, true}},
		{"Poke Radar", new[]{false, false, true, false, false, false, true, false, false, false}},
		{"Friend Safari", new[]{false, false, false, false, false, false, true, false, false, false}},
		{"Chain Fishing", new[]{false, false, false, false, false, false, true, true, false, false}},
		{"Dex Nav", new[]{false, false, false, false, false, false, false, true, false, false}},
		{"Horde Encounters", new[]{false, false, false, false, false, false, true, true, false, false}},
		{"SOS Chain", new[]{false, false, false, false, false, false, false, false, true, true}},
		{"Poke Pelago", new[]{false, false, false, false, false, false, false, false, true, true}},
		{"Ultra Wormhole", new[]{false, false, false, false, false, false, false, false, false, true}} };
		*/
	}
	
	private void GameVersionSelected(long index)
	{
		// Set other lists invisible before changing
		pokemonList.Visible = false;
		methodList.Visible = false;
		charmButton.Visible = false;
		
		charmButton.ButtonPressed = false; // Make sure user cannot enable shiny charm then change games
		selectedGame = gameList.GetItemText((int)index); // Get the name of the selected game
		GameInfo info = gameInfoDict[selectedGame]; // Get the code for the selected game
		
		setPokemonList(info.spritesFolder); // Add all available pokemon to the list
		setMethodList(info.methodID); // Add all available methods to the list
		
		// Make changed lists visible again
		pokemonList.Visible = true;
		methodList.Visible = true;
		if (info.methodID >= 5) // Shiny charm introduced in Black2/White2
		{
			charmButton.Visible = true;
		}
	}
	
	private void setPokemonList(string folderName)
	{
		// Remove previous pokemon from the list
		pokemonList.Clear();
		
		// Open the sprites folder for the selected game
		using var spritesDir = DirAccess.Open($"res://Sprites/{folderName}/Regular/");
		spritesDir.ListDirBegin();
		string fileName = spritesDir.GetNext();
		
		// Add the name of each sprite to the pokemon ItemList
		string pokemonName;
		while (fileName != "")
		{
			// Prevent double names due to import files in the folders
			if (fileName.Split('.').Last() != "import") {
				pokemonName = fileName.Split('.')[0];
				pokemonList.AddItem(pokemonName);
			}
			fileName = spritesDir.GetNext();
		}
	}
	
	private void setMethodList(int gameCode)
	{
		// Clear the previous methods from the list
		methodList.Clear();
		
		// If the method is available in the selected game, add it to methodList
		foreach(KeyValuePair<string, bool[]> method in methodAvailabilityDict)
		{
			if (method.Value[gameCode] == true)
			{
				methodList.AddItem(method.Key);
			}
		}
	}


	private void PokemonSelected(long index)
	{
		selectedPokemon = pokemonList.GetItemText((int)index);
	}


	private void HuntMethodSelected(long index)
	{
		selectedMethod = methodList.GetItemText((int)index);
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
	
	private void OpenSelector()
	{
		OptionSelect selectScreen = (OptionSelect)GD.Load<PackedScene>("res://Scenes/OptionSelect.tscn").Instantiate();
		AddChild(selectScreen);
		
		if (optionMode == 1) // Send list of games
		{
			selectScreen.CreateList(optionMode, new List<string>(gameInfoDict.Keys));
		} 
		else if (optionMode == 2) // Send list of pokemon
		{
			// Open the sprites folder for the selected game
			using var spritesDir = DirAccess.Open($"res://Sprites/{folderName}/Regular/");
			spritesDir.ListDirBegin();
			string fileName = spritesDir.GetNext();
			
			List<string> names = new List<string>();
			
			// Add the name of each sprite to the pokemon ItemList
			string pokemonName;
			while (fileName != "")
			{
				// Prevent double names due to import files in the folders
				if (fileName.Split('.').Last() != "import") {
					pokemonName = fileName.Split('.')[0];
					names.Add(pokemonName);
				}
				fileName = spritesDir.GetNext();
			}
			
			selectScreen.CreateList(optionMode, names);
		}
		else if (optionMode == 3) // Send list of methods
		{
			selectScreen.CreateList(optionMode, new List<string>(methodAvailabilityDict.Keys));
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
		charmButton.Visible = false;
		
		if (optionMode == 1)
		{
			selectedGame = selectedOption;
			
			// Reset other selections so non existent options can't be selected
			selectedPokemon = "";
			selectedMethod = "";
			// Make sure user cannot enable shiny charm then change games
			charmButton.ButtonPressed = false;
			
			GameInfo info = gameInfoDict[selectedGame]; // Get the code for the selected game
			if (info.methodID >= 5) // Shiny charm introduced in Black2/White2
			{
				charmButton.Visible = true;
			}
		}
		else if (optionMode == 2)
		{
			selectedPokemon = selectedOption;
		}
		else if (optionMode == 3)
		{
			selectedMethod = selectedOption;
		}
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
		gameSelect.Text = "Game:\n" + selectedGame;
	}
	
	private void EmitStartHunt()
	{
		GD.Print(selectedPokemon);
		EmitSignal("StartHunt", selectedGame, selectedPokemon, selectedMethod, charmButton.ButtonPressed);
	}
	
	private string LoadJsonFromFile(string path, string fileName)
	{
		string data = null;
		path = path + fileName;
		
		if (!File.Exists(path))
		{	
			return null;
		}
		
		try
		{
			data = File.ReadAllText(path);
		}
		catch (System.Exception e)
		{
			GD.Print(e);
		}
		
		return data;
	}
	
	// Destroy this UI element
	public void Cleanup()
	{
		QueueFree();
	}
}
