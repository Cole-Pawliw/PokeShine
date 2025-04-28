using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public class GameInfo
{
	public GameInfo()
	{
		methodID = 0;
		spritesFolder = "GS";
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
		new GameInfo(0, "GS"), new GameInfo(1, "RS"), new GameInfo(2, "FL"), new GameInfo(3, "DP"), 
		new GameInfo(4, "HS"), new GameInfo(5, "BW"), new GameInfo(6, "BW"), new GameInfo(7, "BankModels"),
		new GameInfo(8, "BankModels"), new GameInfo(9, "BankModels"), new GameInfo(10, "BankModels"),
		new GameInfo(11, "BankModels"), new GameInfo(12, "BankModels"), new GameInfo(13, "BankModels"),
		new GameInfo(14, "BankModels"), new GameInfo(15, "BankModels") };
		
	// This dictionary maps the names of pokemon games to corresponding information
	// The int corresponds with an index to be used in methodAvailabilityDict
	// The string indicates the folder name used to access the sprites for this game
	public readonly static Dictionary<string, GameInfo> gameInfoDict = new Dictionary<string, GameInfo>(){
		{"Gold", infoStorage[0]}, {"Silver", infoStorage[0]}, {"Crystal", infoStorage[0]},
		{"Ruby", infoStorage[1]}, {"Sapphire", infoStorage[1]}, {"Emerald", infoStorage[1]},
		{"Fire Red", infoStorage[2]}, {"Leaf Green", infoStorage[2]},
		{"Diamond", infoStorage[3]}, {"Pearl", infoStorage[3]}, {"Platinum", infoStorage[3]},
		{"Heart Gold", infoStorage[4]}, {"Soul Silver", infoStorage[4]},
		{"Black", infoStorage[5]}, {"White", infoStorage[5]},
		{"Black 2", infoStorage[6]}, {"White 2", infoStorage[6]},
		{"X", infoStorage[7]}, {"Y", infoStorage[7]},
		{"Alpha Sapphire", infoStorage[8]}, {"Omega Ruby", infoStorage[8]},
		{"Sun", infoStorage[9]}, {"Moon", infoStorage[9]},
		{"Ultra Sun", infoStorage[10]}, {"Ultra Moon", infoStorage[10]},
		{"Let's Go Pikachu", infoStorage[11]}, {"Let's Go Eevee", infoStorage[11]},
		{"Sword", infoStorage[12]}, {"Shield", infoStorage[12]},
		{"Brilliant Diamond", infoStorage[13]}, {"Shining Pearl", infoStorage[13]},
		{"Legends Arceus", infoStorage[14]},
		{"Scarlet", infoStorage[15]}, {"Violet", infoStorage[15]} };
}

public partial class AvailabilityInformation : Node
{
	JsonManager json;
	
	// Stores the method name as the key and an array of booleans indicating if it's in a specific game
	public Dictionary<string, bool[]> methodAvailabilityDict;
	
	// Stores every type of pokeball
	public Dictionary<string, bool[]> ballAvailabilityDict;
	
	// Stores every pokemon with an array of availability in different games
	public Dictionary<string, bool[]> pokemonAvailabilityDict;
	
	// Stores the routes for a specific game and all the pokemon that can be seen in that route
	// Currently it does not store any pokemon, that will be added in the future
	public Dictionary<string, string[]> pokemonRouteAvailabilityDict;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		json = GetNode<JsonManager>("JsonManager");
		string path = "res://Jsons/";
		
		string methods = json.LoadResourceFromFile(path, "methods.json");
		string balls = json.LoadResourceFromFile(path, "balls.json");
		string pokemon = json.LoadResourceFromFile(path, "pokemon.json");
		
		var options = new JsonSerializerOptions
		{
			IncludeFields = true,
		};
		methodAvailabilityDict = JsonSerializer.Deserialize<Dictionary<string, bool[]>>(methods, options)!;
		ballAvailabilityDict = JsonSerializer.Deserialize<Dictionary<string, bool[]>>(balls, options)!;
		pokemonAvailabilityDict = JsonSerializer.Deserialize<Dictionary<string, bool[]>>(pokemon, options)!;
	}
	
	public void SetRoutes(string game)
	{
		string path = "res://Jsons/Games/";
		
		// Load the json for the specified game
		string routes = json.LoadResourceFromFile(path, game + ".json");
		
		var options = new JsonSerializerOptions
		{
			IncludeFields = true,
		};
		pokemonRouteAvailabilityDict = JsonSerializer.Deserialize<Dictionary<string, string[]>>(routes, options)!;
	}
}
