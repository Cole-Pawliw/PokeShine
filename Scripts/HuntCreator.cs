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

public class GameHuntInformation
{
	// Stores the repeated GameInfo objects for re-use in gameInfoDict
	public static GameInfo[] infoStorage = {
		new GameInfo(0, "RS"), new GameInfo(1, "FL"), new GameInfo(2, "DP"), 
		new GameInfo(3, "HS"), new GameInfo(4, "BW"), new GameInfo(5, "BW"), new GameInfo(6, "Models"),
		new GameInfo(7, "Models"), new GameInfo(8, "Models"), new GameInfo(9, "Models") };
		
	// This dictionary maps the names of pokemon games to corresponding information
	// The int corresponds with an index to be used in methodAvailabilityDict
	// The string indicates the folder name used to access the sprites for this game
	public readonly static Dictionary<string, GameInfo> gameInfoDict = new Dictionary<string, GameInfo>()
	{
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
	
	// Stores the method name as the key and an array of booleans indicating if it's in a specific game
	public readonly static Dictionary<string, bool[]> methodAvailabilityDict = new Dictionary<string, bool[]>(){
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
	
	public readonly static Dictionary<string, bool[]> ballAvailabilityDict = new Dictionary<string, bool[]>(){
		{"Poke ball", new[]{true, true, true, true, true, true, true, true, true, true}},
		{"Great ball", new[]{true, true, true, true, true, true, true, true, true, true}},
		{"Ultra ball", new[]{true, true, true, true, true, true, true, true, true, true}},
		{"Master ball", new[]{true, true, true, true, true, true, true, true, true, true}},
		{"Safari ball", new[]{true, true, true, true, false, false, true, true, true, true}},
		{"Fast ball", new[]{false, false, false, true, false, false, true, true, true, true}},
		{"Level ball", new[]{false, false, false, true, false, false, true, true, true, true}},
		{"Lure ball", new[]{false, false, false, true, false, false, true, true, true, true}},
		{"Heavy ball", new[]{false, false, false, true, false, false, true, true, true, true}},
		{"Love ball", new[]{false, false, false, true, false, false, true, true, true, true}},
		{"Friend ball", new[]{false, false, false, true, false, false, true, true, true, true}},
		{"Moon ball", new[]{false, false, false, true, false, false, true, true, true, true}},
		{"Sport ball", new[]{false, false, false, true, false, false, true, true, true, true}},
		{"Net ball", new[]{true, true, true, true, true, true, true, true, true, true}},
		{"Dive ball", new[]{true, true, true, true, true, true, true, true, true, true}},
		{"Nest ball", new[]{true, true, true, true, true, true, true, true, true, true}},
		{"Repeat ball", new[]{true, true, true, true, true, true, true, true, true, true}},
		{"Timer ball", new[]{true, true, true, true, true, true, true, true, true, true}},
		{"Luxury ball", new[]{true, true, true, true, true, true, true, true, true, true}},
		{"Premier ball", new[]{true, true, true, true, true, true, true, true, true, true}},
		{"Dusk ball", new[]{false, false, true, true, true, true, true, true, true, true}},
		{"Heal ball", new[]{false, false, true, true, true, true, true, true, true, true}},
		{"Quick ball", new[]{false, false, true, true, true, true, true, true, true, true}},
		{"Dream ball", new[]{false, false, false, false, false, false, true, true, true, true}},
		{"Beast ball", new[]{false, false, false, false, false, false, false, false, true, true}} };
}

public partial class HuntCreator : Control
{
	Button gameSelect;
	Button pokemonSelect;
	Button methodSelect;
	CheckBox charmButton;
	
	string[] selections = {"", "", ""};
	int optionMode = 0;
	
	/*
	// This dictionary maps the names of pokemon games to corresponding information
	// The int corresponds with an index to be used in methodAvailabilityDict
	// The string indicates the folder name used to access the sprites for this game
	Dictionary<string, GameInfo> gameInfoDict;
	
	// Stores the method name as the key and an array of booleans indicating if it is in a specific game
	Dictionary<string, bool[]> methodAvailabilityDict;
	*/
	
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
		
		/*
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
		*/
		
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
			string folderName = info.spritesFolder; // Get the associated folder name for the current game
			
			// Open the sprites folder for the selected game
			using var spritesDir = DirAccess.Open($"res://Sprites/{folderName}/Regular/");
			spritesDir.ListDirBegin();
			string fileName = spritesDir.GetNext();
			
			// Add the name of each sprite to the pokemon list
			string pokemonName;
			while (fileName != "")
			{
				// Prevent double names due to import files in the folders
				if (fileName.Split('.').Last() != "import") {
					pokemonName = fileName.Split('.')[0];
					itemList.Add(pokemonName);
				}
				fileName = spritesDir.GetNext();
			}
		}
		else if (optionMode == 3) // Send list of methods
		{
			GameInfo info = GameHuntInformation.gameInfoDict[selections[0]]; // Get the code for the selected game
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
