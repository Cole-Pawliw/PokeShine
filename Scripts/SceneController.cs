using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;

// GD.Print();

// SAVE FILE LOCATION
// %appdata%\Godot\app_userdata\Shiny Hunt Tracker

/*
KNOWN BUGS:
- Gen 6 games show all models, including those from gen 7
- HuntData needs better constructors
*/

/*
Incomplete features
- Make new json files on first launch (check for existence then create)
- View completed hunt details
- Dynamic hunt odds on hunt screen

Extra features
- Stats page (odds graph, other detailed info)
- Multi hunt, for random encounters in a route
*/

public partial class SceneController : Node
{
	MainMenu mainScreen;
	ShinyHuntScreen huntScreen;
	string saveFileName = "savefile.save";
	
	double timer = 0;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		mainScreen = GetNode<MainMenu>("MainMenu");
		huntScreen = GetNode<ShinyHuntScreen>("ShinyHuntScreen");
		
		mainScreen.HuntButtonPressed += OpenHunt;
		mainScreen.NewHuntButtonPressed += CreateNewHunt;
		mainScreen.TreeExiting += AppClosing;
		huntScreen.BackButtonPressed += CloseHunt;
		huntScreen.DeleteSignal += DeleteHunt;
		
		Load();
	}
	
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		timer += delta;
		
		// Update the main menu and save everything every 5 minutes
		if (timer > 300.0)
		{
			// Only update the current hunt if the hunt screen is open
			if (huntScreen.Visible)
			{
				HuntData data = huntScreen.data;
				mainScreen.UpdateHunt(data);
			}
			Save();
			timer = 0.0;
		}
	}
	
	private void OpenHunt(int selectedHuntID)
	{
		HuntData selectedHunt = mainScreen.GetHunt(selectedHuntID);
		huntScreen.InitializeHunt(new HuntData(selectedHunt));
		huntScreen.Visible = true;
		mainScreen.Visible = false;
	}
	
	private void CloseHunt()
	{
		HuntData data = huntScreen.data;
		mainScreen.UpdateHunt(data);
		Save();
		mainScreen.Visible = true;
		huntScreen.Visible = false;
	}
	
	private void CreateNewHunt()
	{
		HuntCreator startHuntScreen = (HuntCreator)GD.Load<PackedScene>("res://Scenes/HuntCreator.tscn").Instantiate();
		AddChild(startHuntScreen);
		
		startHuntScreen.Visible = true;
		mainScreen.Visible = false;
		huntScreen.Visible = false;
		
		startHuntScreen.StartHunt += StartHuntSignalReceiver;
		startHuntScreen.BackButtonPressed += CloseCreator;
	}
	
	private void DeleteHunt()
	{
		HuntData data = huntScreen.data;
		mainScreen.RemoveHunt(data);
		Save();
		mainScreen.Visible = true;
		huntScreen.Visible = false;
	}
	
	private void CloseCreator()
	{
		HuntCreator startHuntScreen = GetNode<HuntCreator>("HuntCreator");
		mainScreen.Visible = true;
		startHuntScreen.Visible = false;
		startHuntScreen.Cleanup();
	}
	
	private void StartHuntSignalReceiver(string gameName, string pokemonName, string method, bool charm)
	{
		string startDT = Time.GetDatetimeStringFromSystem();
		HuntData huntToAdd = new HuntData(pokemonName, gameName, method, charm, startDT);
		mainScreen.AddHunt(huntToAdd);
		Save(); // Update save file with newly added hunt
		
		HuntCreator startHuntScreen = GetNode<HuntCreator>("HuntCreator");
		mainScreen.Visible = true;
		startHuntScreen.Visible = false;
		startHuntScreen.Cleanup();
	}
	
	private void Save()
	{
		List<HuntData> allHunts = mainScreen.GetHunts();
		
		var options = new JsonSerializerOptions
		{
			IncludeFields = true,
		};
		
		string huntData = JsonSerializer.Serialize<List<HuntData>>(allHunts, options);
		SaveJsonToFile(saveFileName, huntData);
	}
	
	private void Load()
	{
		string path = ProjectSettings.GlobalizePath("user://");
		string huntData = LoadJsonFromFile(path, saveFileName);
		
		if (huntData == null)
		{
			return;
		}
		
		var options = new JsonSerializerOptions
		{
			IncludeFields = true,
		};
		List<HuntData> allHunts = JsonSerializer.Deserialize<List<HuntData>>(huntData, options)!;
		
		foreach (HuntData hunt in allHunts)
		{
			mainScreen.AddHunt(hunt);
		}
	}
	
	private void SaveJsonToFile(string fileName, string data)
	{
		string path = ProjectSettings.GlobalizePath("user://");
		GD.Print(path);
		if (!Directory.Exists(path))
		{
			Directory.CreateDirectory(path);
		}
		
		path = path + fileName;
		try
		{
			File.WriteAllText(path, data);
		}
		catch (System.Exception e)
		{
			GD.Print(e);
		}
	}
	
	private string LoadJsonFromFile(string path, string fileName)
	{
		string data = null;
		path = Path.Join(path, fileName);
		
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
	
	private void AppClosing()
	{
		// Only update the current hunt if the hunt screen is open
		if (huntScreen.Visible)
		{
			HuntData data = huntScreen.data;
			mainScreen.UpdateHunt(data);
		}
		Save();
	}
}
