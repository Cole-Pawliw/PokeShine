using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;

// SAVE FILE LOCATION
// %appdata%\Godot\app_userdata\Shiny Hunt Tracker

/*
KNOWN BUGS:
- HuntData needs better constructors
- Make window stretch to mobile screen size
- Fix scrolling on mobile
*/

/*
Extra features
- Active hunt stats page (odds graph, other detailed info)
- Per-route pokemon availability (very complicated to make, might not get added)
- Mod support (custom sprites for ROM hacks)
*/

public partial class SceneController : Node
{
	MainMenu mainScreen;
	ShinyHuntScreen huntScreen;
	JsonManager json;
	string saveFileName = "savefile.save";
	
	double timer = 0;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		mainScreen = GetNode<MainMenu>("MainMenu");
		huntScreen = GetNode<ShinyHuntScreen>("ShinyHuntScreen");
		json = GetNode<JsonManager>("JsonManager");
		
		mainScreen.HuntButtonPressed += OpenHunt;
		mainScreen.CapturedButtonPressed += OpenStats;
		mainScreen.NewHuntButtonPressed += CreateNewHunt;
		mainScreen.InfoButtonPressed += OpenAppInfo;
		mainScreen.TreeExiting += AppClosing;
		huntScreen.BackButtonPressed += CloseHunt;
		huntScreen.DeleteSignal += DeleteHunt;
		huntScreen.HuntChanged += UpdateActiveSprite;
		huntScreen.FinishHunt += FinishHunt;
		
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
	
	private void OpenStats(int selectedHuntID)
	{
		CapturedData selectedHunt = mainScreen.GetCaptured(selectedHuntID);
		FinishedStats statsScreen = (FinishedStats)GD.Load<PackedScene>("res://Scenes/FinishedStats.tscn").Instantiate();
		AddChild(statsScreen);
		statsScreen.BackButtonPressed += CloseStats;
		statsScreen.HuntChanged += UpdateCaptured;
		statsScreen.DeleteHunt += DeleteCaptured;
		statsScreen.InitializeStats(new CapturedData(selectedHunt));
		
		statsScreen.Visible = true;
		mainScreen.Visible = false;
		huntScreen.Visible = false;
	}
	
	private void CloseHunt()
	{
		HuntData data = huntScreen.data;
		mainScreen.UpdateHunt(data);
		Save();
		mainScreen.Visible = true;
		huntScreen.Visible = false;
	}
	
	private void CloseStats()
	{
		FinishedStats statsScreen = GetNode<FinishedStats>("FinishedStats");
		mainScreen.Visible = true;
		statsScreen.Visible = false;
		statsScreen.Cleanup();
	}
	
	private void CreateNewHunt(int mode)
	{
		if (mode == 0)
		{
			HuntCreator startHuntScreen = (HuntCreator)GD.Load<PackedScene>("res://Scenes/HuntCreator.tscn").Instantiate();
			AddChild(startHuntScreen);
			
			startHuntScreen.Visible = true;
			mainScreen.Visible = false;
			huntScreen.Visible = false;
			
			startHuntScreen.StartHunt += StartHuntSignalReceiver;
			startHuntScreen.BackButtonPressed += CloseHuntCreator;
		}
		else
		{
			CapturedCreator startHuntScreen = (CapturedCreator)GD.Load<PackedScene>("res://Scenes/CapturedCreator.tscn").Instantiate();
			AddChild(startHuntScreen);
			
			startHuntScreen.Visible = true;
			mainScreen.Visible = false;
			huntScreen.Visible = false;
			
			startHuntScreen.AddHunt += AddCaptured;
			startHuntScreen.BackButtonPressed += CloseCapturedCreator;
		}
	}
	
	private void DeleteHunt()
	{
		HuntData data = huntScreen.data;
		mainScreen.RemoveHunt(data);
		Save();
		mainScreen.Visible = true;
		huntScreen.Visible = false;
	}
	
	private void DeleteCaptured()
	{
		FinishedStats screen = GetNode<FinishedStats>("FinishedStats");
		CapturedData data = screen.data;
		mainScreen.RemoveCaptured(data);
		Save();
		mainScreen.Visible = true;
		screen.Visible = false;
		screen.Cleanup();
	}
	
	private void UpdateActiveSprite()
	{
		HuntData data = huntScreen.data;
		mainScreen.UpdateHunt(data);
		mainScreen.UpdateHuntSprite(data.huntID);
		Save();
	}
	
	private void UpdateCaptured()
	{
		FinishedStats screen = GetNode<FinishedStats>("FinishedStats");
		CapturedData data = screen.data;
		mainScreen.UpdateCaptured(data);
		mainScreen.UpdateCapturedSprite(data.huntID);
		Save();
	}
	
	private void CloseHuntCreator()
	{
		HuntCreator startHuntScreen = GetNode<HuntCreator>("HuntCreator");
		mainScreen.Visible = true;
		startHuntScreen.Visible = false;
		startHuntScreen.Cleanup();
	}
	
	private void StartHuntSignalReceiver(string gameName, string method, bool charm)
	{
		string startDT = Time.GetDatetimeStringFromSystem().Split('T')[0];
		HuntCreator startHuntScreen = GetNode<HuntCreator>("HuntCreator");
		HuntData huntToAdd = new HuntData(startHuntScreen.pokemonSelected, gameName, method, charm, startDT);
		
		mainScreen.AddHunt(huntToAdd);
		Save(); // Update save file with newly added hunt
		
		CloseHuntCreator();
	}
	
	private void CloseCapturedCreator()
	{
		CapturedCreator startHuntScreen = GetNode<CapturedCreator>("CapturedCreator");
		mainScreen.Visible = true;
		startHuntScreen.Visible = false;
		startHuntScreen.Cleanup();
	}
	
	private void AddCaptured()
	{
		// s short for startHuntScreen so that initializing the CapturedData isn't 10 lines long
		CapturedCreator s = GetNode<CapturedCreator>("CapturedCreator");
		string startDate = $"{s.startYear.Value}-{s.startMon.Value}-{s.startDay.Value}";
		string endDate = $"{s.endYear.Value}-{s.endMon.Value}-{s.endDay.Value}";
		CapturedData huntToAdd = new CapturedData(startDate, endDate, s.selections[0], s.selections[1],
												s.selections[2], s.selections[3], s.selections[4], s.selections[5],
												s.nickname.Text, s.charmButton.ButtonPressed,
												(int)s.counter.Value, (int)s.timer.Value * 60);
		
		// Fill empty values
		string DT = Time.GetDatetimeStringFromSystem();
		if (huntToAdd.startDate == "")
		{
			huntToAdd.startDate = DT;
		}
		if (huntToAdd.endDate == "")
		{
			huntToAdd.endDate = DT;
		}
		
		mainScreen.AddCaptured(huntToAdd);
		Save(); // Update save file with newly added hunt
		
		CloseCapturedCreator();
	}
	
	private void FinishHunt(string nickname, string ball, string gender)
	{
		string endDT = Time.GetDatetimeStringFromSystem().Split('T')[0];
		CapturedData finishedHunt = new CapturedData(huntScreen.data, endDT, nickname, ball, gender);
		mainScreen.RemoveHunt(huntScreen.data);
		mainScreen.AddCaptured(finishedHunt);
		Save();
		mainScreen.Visible = true;
		huntScreen.Visible = false;
	}
	
	private void OpenAppInfo()
	{
		AppInfoScreen infoScreen = (AppInfoScreen)GD.Load<PackedScene>("res://Scenes/AppInfoScreen.tscn").Instantiate();
		AddChild(infoScreen);
		infoScreen.BackButtonPressed += CloseAppInfo;
		
		infoScreen.Visible = true;
		mainScreen.Visible = false;
		huntScreen.Visible = false;
	}
	
	private void CloseAppInfo()
	{
		AppInfoScreen infoScreen = GetNode<AppInfoScreen>("AppInfoScreen");
		mainScreen.Visible = true;
		infoScreen.Visible = false;
		infoScreen.Cleanup();
	}
	
	private void Save()
	{
		List<HuntData> allHunts = mainScreen.GetHunts();
		List<CapturedData> allCaptured = mainScreen.GetFinished();
		
		var options = new JsonSerializerOptions
		{
			IncludeFields = true,
		};
		
		string huntData = JsonSerializer.Serialize<List<HuntData>>(allHunts, options);
		string capturedData = JsonSerializer.Serialize<List<CapturedData>>(allCaptured, options);
		string fullSave = huntData + "\n" + capturedData;
		string path = ProjectSettings.GlobalizePath("user://");
		json.SaveJsonToFile(path, saveFileName, fullSave);
	}
	
	private void Load()
	{
		string path = ProjectSettings.GlobalizePath("user://");
		string fullLoad = json.LoadJsonFromFile(path, saveFileName);
		
		if (fullLoad == null)
		{
			return;
		}
		
		string[] datas = fullLoad.Split("\n");
		
		var options = new JsonSerializerOptions
		{
			IncludeFields = true,
		};
		List<HuntData> allHunts = JsonSerializer.Deserialize<List<HuntData>>(datas[0], options)!;
		List<CapturedData> allCaptures = JsonSerializer.Deserialize<List<CapturedData>>(datas[1], options)!;
		
		foreach (HuntData hunt in allHunts)
		{
			mainScreen.AddHunt(hunt);
		}
		
		foreach (CapturedData hunt in allCaptures)
		{
			mainScreen.AddCaptured(hunt);
		}
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
