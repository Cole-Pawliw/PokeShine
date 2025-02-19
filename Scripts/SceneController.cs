using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;

// GD.Print();

/*
KNOWN BUGS:
- Selecting a game in HuntCreator sometimes slowly moves up the list instead of just selecting -- Might be resolved?
- Gen 6 games show all models, including those from gen 7
- HuntData needs better constructors
*/

/*
Incomplete features
- Make new json files on first launch (check for existence then create)
- Save files
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
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		mainScreen = GetNode<MainMenu>("MainMenu");
		huntScreen = GetNode<ShinyHuntScreen>("ShinyHuntScreen");
		
		mainScreen.HuntButtonPressed += OpenHunt;
		mainScreen.NewHuntButtonPressed += CreateNewHunt;
		huntScreen.BackButtonPressed += CloseHunt;
		huntScreen.DeleteSignal += DeleteHunt;
	}
	
	private void OpenHunt(int selectedHuntID)
	{
		HuntData selectedHunt = mainScreen.GetHunt(selectedHuntID);
		huntScreen.InitializeHunt(selectedHunt);
		huntScreen.Visible = true;
		mainScreen.Visible = false;
	}
	
	private void CloseHunt()
	{
		HuntData data = huntScreen.data;
		mainScreen.UpdateHunt(data);
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
		
		HuntCreator startHuntScreen = GetNode<HuntCreator>("HuntCreator");
		mainScreen.Visible = true;
		startHuntScreen.Visible = false;
		startHuntScreen.Cleanup();
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
}
