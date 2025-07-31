using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;

/*
Bugs and Changes
- Expand on routes
- Clean up code
- Add luck to FinishedStats
- Add clear all button to OptionSelect
- Different font
- Edit button design
*/

/*
Extra features
- Box sprites for ItemList
- GSC sprites?
*/

public partial class SceneController : Control
{
	MainMenu mainScreen;
	ShinyHuntScreen huntScreen;
	JsonManager json;
	string saveFileName = "savefile.save", activeFileName = "ActiveHunts.save", capturedFileName = "CapturedHunts.save";
	string path = "user://";
	string versionNumber = "1.0.3";
	
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
		mainScreen.SettingsButtonPressed += OpenSettings;
		mainScreen.RequestFullSave += Save;
		mainScreen.RequestSmallSave += SaveActiveHunts;
		mainScreen.TreeExiting += AppClosing;
		huntScreen.BackButtonPressed += CloseHunt;
		huntScreen.DeleteSignal += DeleteHunt;
		huntScreen.HuntChanged += UpdateActiveSprite;
		huntScreen.FinishHunt += FinishHunt;
		
		Load();
		
		SetColors();
	}
	
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		timer += delta;
		
		// Update the main menu and save everything every 5 minutes
		if (timer > 300.0)
		{
			Save();
			timer = 0.0;
		}
	}
	
	public override void _Notification(int what)
	{
		if (what == NotificationApplicationPaused)
		{
			Save();
		}
	}
	
	private void OpenHunt(int selectedHuntID)
	{
		HuntData selectedHunt = mainScreen.GetHunt(selectedHuntID);
		mainScreen.PauseHunts();
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
		mainScreen.PauseHunts();
	}
	
	private void CloseHunt()
	{
		HuntData data = huntScreen.data;
		mainScreen.UpdateHunt(data);
		mainScreen.Visible = true;
		huntScreen.Visible = false;
		Save();
	}
	
	private void CloseStats()
	{
		FinishedStats statsScreen = GetNode<FinishedStats>("FinishedStats");
		mainScreen.Visible = true;
		statsScreen.Visible = false;
		RemoveChild(statsScreen);
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
		mainScreen.PauseHunts();
	}
	
	private void DeleteHunt()
	{
		HuntData data = huntScreen.data;
		mainScreen.RemoveHunt(data);
		mainScreen.Visible = true;
		huntScreen.Visible = false;
		Save();
	}
	
	private void DeleteCaptured()
	{
		FinishedStats screen = GetNode<FinishedStats>("FinishedStats");
		CapturedData data = screen.data;
		mainScreen.RemoveCaptured(data);
		mainScreen.Visible = true;
		screen.Visible = false;
		Save();
		RemoveChild(screen);
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
		RemoveChild(startHuntScreen);
		startHuntScreen.Cleanup();
	}
	
	private void StartHuntSignalReceiver(string gameName, string method, string route, bool charm, int oddsBonus)
	{
		string startDT = Time.GetDatetimeStringFromSystem().Split('T')[0];
		HuntCreator startHuntScreen = GetNode<HuntCreator>("HuntCreator");
		HuntData huntToAdd = new HuntData(startHuntScreen.pokemonSelected, gameName, method, route, charm, oddsBonus, startDT);
		
		// Set common settings
		huntToAdd.showShiny = GlobalSettings.huntInfo[0];
		huntToAdd.showRegular = GlobalSettings.huntInfo[1];
		huntToAdd.showOdds = GlobalSettings.huntInfo[2];
		huntToAdd.showFullTimer = GlobalSettings.huntInfo[4];
		huntToAdd.showMiniTimer = GlobalSettings.huntInfo[5];
		// Gross if statement but this function is rarely called
		if (huntToAdd.huntMethod == "Poke Radar" || huntToAdd.huntMethod == "Chain Fishing" || huntToAdd.huntMethod == "Dex Nav"
			|| huntToAdd.huntMethod == "SOS Chain" || huntToAdd.huntMethod == "Catch Combo" || (huntToAdd.huntMethod == "Mass Outbreak"
			&& (huntToAdd.huntGame == "Scarlet" || huntToAdd.huntGame == "Violet")))
		{
			huntToAdd.showCombo = GlobalSettings.huntInfo[3];
		}
		
		huntToAdd.huntIndex = 0; // Set index to go at the top of the list
		mainScreen.AddHunt(huntToAdd);
		mainScreen.UpdateHuntIndices();
		Save(); // Update save file with newly added hunt
		
		CloseHuntCreator();
	}
	
	private void CloseCapturedCreator()
	{
		CapturedCreator startHuntScreen = GetNode<CapturedCreator>("CapturedCreator");
		mainScreen.Visible = true;
		startHuntScreen.Visible = false;
		RemoveChild(startHuntScreen);
		startHuntScreen.Cleanup();
	}
	
	private void AddCaptured()
	{
		// s short for startHuntScreen so that initializing the CapturedData isn't 10 lines long
		CapturedCreator s = GetNode<CapturedCreator>("CapturedCreator");
		CapturedData huntToAdd = new CapturedData(s.startDate.date, s.endDate.date, s.selections[0], s.selections[1],
												s.selections[2], s.selections[3], s.selections[4], s.selections[5],
												s.nickname.Text, s.charmButton.ButtonPressed,
												(int)s.counter.Value, s.timer.totalTime);
		
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
		
		huntToAdd.huntIndex = 0; // Set index to go at the top of the list
		mainScreen.AddCaptured(huntToAdd);
		mainScreen.UpdateHuntIndices();
		Save(); // Update save file with newly added hunt
		
		CloseCapturedCreator();
	}
	
	private void FinishHunt(string nickname, string ball, string gender)
	{
		string endDT = Time.GetDatetimeStringFromSystem().Split('T')[0];
		CapturedData finishedHunt = new CapturedData(huntScreen.data, endDT, nickname, ball, gender);
		mainScreen.RemoveHunt(huntScreen.data);
		mainScreen.AddCaptured(finishedHunt);
		mainScreen.Visible = true;
		huntScreen.Visible = false;
		Save();
	}
	
	private void OpenSettings()
	{
		UserSettings settingsScreen = (UserSettings)GD.Load<PackedScene>("res://Scenes/UserSettings.tscn").Instantiate();
		AddChild(settingsScreen);
		settingsScreen.BackButtonPressed += CloseSettings;
		settingsScreen.NewColors += SetColors;
		settingsScreen.SetSettings();
		
		settingsScreen.Visible = true;
		mainScreen.Visible = false;
		huntScreen.Visible = false;
		
		mainScreen.PauseHunts();
	}
	
	private void CloseSettings()
	{
		UserSettings settingsScreen = GetNode<UserSettings>("UserSettings");
		
		bool[] settings = {settingsScreen.shiny.ButtonPressed, settingsScreen.regular.ButtonPressed,
							settingsScreen.odds.ButtonPressed, settingsScreen.combo.ButtonPressed,
							settingsScreen.huntTimer.ButtonPressed, settingsScreen.encounterTimer.ButtonPressed};
		mainScreen.UpdateAllSettings(settings);
		
		mainScreen.Visible = true;
		settingsScreen.Visible = false;
		RemoveChild(settingsScreen);
		settingsScreen.Cleanup();
	}
	
	private void SetColors()
	{
		mainScreen.SetColors();
		huntScreen.SetColors();
		Theme = (Theme)GD.Load($"res://ColorTheme{GlobalSettings.colorMode}.tres");
	}
	
	private void Save()
	{
		var options = new JsonSerializerOptions
		{
			IncludeFields = true,
		};
		
		string fullSave = $"v{versionNumber}\nsortby:{GlobalSettings.sort}\n";
		fullSave += $"colors:{GlobalSettings.colorMode}\n";
		fullSave += $"volume:{GlobalSettings.soundOn}\n";
		fullSave += $"show shiny:{GlobalSettings.huntInfo[0]}\n";
		fullSave += $"show regular:{GlobalSettings.huntInfo[1]}\n";
		fullSave += $"show odds:{GlobalSettings.huntInfo[2]}\n";
		fullSave += $"show combo:{GlobalSettings.huntInfo[3]}\n";
		fullSave += $"show hunt timer:{GlobalSettings.huntInfo[4]}\n";
		fullSave += $"show encounter timer:{GlobalSettings.huntInfo[5]}\n";
		json.SaveJsonToFile(path, saveFileName, fullSave);
		SaveActiveHunts();
		SaveCaptured();
	}
	
	private void SaveActiveHunts()
	{
		// Only update the current hunt if the hunt screen is open
		if (huntScreen.Visible)
		{
			HuntData data = huntScreen.data;
			mainScreen.UpdateHunt(data);
		}
		
		List<HuntData> allHunts = mainScreen.GetHunts();
		string huntData = "";
		
		var options = new JsonSerializerOptions
		{
			IncludeFields = true,
		};
		
		foreach (HuntData hunt in allHunts)
		{
			huntData += JsonSerializer.Serialize<HuntData>(hunt, options);
			huntData += "\n";
		}
		if (huntData.Length > 0) // Check if any hunts have been aded
		{
			huntData = huntData.Remove(huntData.Length - 1); // Remove final \n
		}
		json.SaveJsonToFile(path, activeFileName, huntData);
	}
	
	private void SaveCaptured()
	{
		List<CapturedData> allCaptured = mainScreen.GetFinished();
		string capturedData = "";
		
		var options = new JsonSerializerOptions
		{
			IncludeFields = true,
		};
		
		foreach (CapturedData hunt in allCaptured)
		{
			capturedData += JsonSerializer.Serialize<CapturedData>(hunt, options);
			capturedData += "\n";
		}
		if (capturedData.Length > 0)
		{
			capturedData = capturedData.Remove(capturedData.Length - 1); // Remove final \n
		}
		json.SaveJsonToFile(path, capturedFileName, capturedData);
	}
	
	private void Load()
	{
		string fullLoad = json.LoadResourceFromFile(path, saveFileName);
		if (fullLoad == null || fullLoad == "")
		{
			return;
		}
		
		string[] datas = fullLoad.Split("\n");
		
		switch (datas[0])
		{
			case "v1.0.3":
				Load101(fullLoad);
				break;
			case "v1.0.1":
				Load101(fullLoad);
				break;
			case "v1.0":
				Load096(fullLoad);
				break;
			case "v0.9.6":
				Load096(fullLoad);
				break;
			case "v0.9.4":
				Load094(fullLoad);
				break;
			case "v0.9.3":
				Load093(fullLoad);
				break;
			default:
				LoadDefault(fullLoad);
				break;
		}
	}
	
	private void Load101(string fullLoad)
	{
		string[] datas = fullLoad.Split("\n");
		int size = datas.Length;
		GlobalSettings.sort = datas[1].Split(':')[1];
		GlobalSettings.colorMode = Int32.Parse(datas[2].Split(':')[1]);
		GlobalSettings.soundOn = bool.Parse(datas[3].Split(':')[1]);
		GlobalSettings.huntInfo = [ bool.Parse(datas[4].Split(':')[1]), bool.Parse(datas[5].Split(':')[1]),
									  bool.Parse(datas[6].Split(':')[1]), bool.Parse(datas[7].Split(':')[1]),
									  bool.Parse(datas[8].Split(':')[1]), bool.Parse(datas[9].Split(':')[1]) ];
		
		LoadActiveHunts();
		LoadCaptured();
	}
	
	private void Load096(string fullLoad)
	{
		string[] datas = fullLoad.Split("\n");
		int size = datas.Length;
		GlobalSettings.sort = datas[1].Split(':')[1];
		GlobalSettings.colorMode = Int32.Parse(datas[2].Split(':')[1]);
		GlobalSettings.huntInfo = [ bool.Parse(datas[3].Split(':')[1]), bool.Parse(datas[4].Split(':')[1]),
									  bool.Parse(datas[5].Split(':')[1]), bool.Parse(datas[6].Split(':')[1]),
									  bool.Parse(datas[7].Split(':')[1]), bool.Parse(datas[8].Split(':')[1]) ];
		
		LoadActiveHunts();
		LoadCaptured();
	}
	
	private void Load094(string fullLoad)
	{
		string[] datas = fullLoad.Split("\n");
		int size = datas.Length;
		GlobalSettings.sort = datas[1].Split(':')[1];
		
		LoadActiveHunts();
		LoadCaptured();
	}
	
	private void LoadActiveHunts()
	{
		string fullLoad = json.LoadResourceFromFile(path, activeFileName);
		if (fullLoad == null || fullLoad == "")
		{
			return;
		}
		
		string[] hunts = fullLoad.Split("\n");
		var options = new JsonSerializerOptions
		{
			IncludeFields = true,
		};
		
		foreach (string hunt in hunts)
		{
			try
			{
				mainScreen.AddHunt(JsonSerializer.Deserialize<HuntData>(hunt, options)!);
			}
			catch (Exception e)
			{
				string path = ProjectSettings.GlobalizePath("user://");
				string backupFile = "activebackup.save";
				json.SaveJsonToFile(path, backupFile, fullLoad);
				GD.Print(e);
			}
		}
		mainScreen.UpdateHuntIndices();
	}
	
	private void LoadCaptured()
	{
		string fullLoad = json.LoadResourceFromFile(path, capturedFileName);
		if (fullLoad == null || fullLoad == "")
		{
			return;
		}
		
		string[] hunts = fullLoad.Split("\n");
		var options = new JsonSerializerOptions
		{
			IncludeFields = true,
		};
		
		foreach (string hunt in hunts)
		{
			try
			{
				mainScreen.AddCaptured(JsonSerializer.Deserialize<CapturedData>(hunt, options)!);
			}
			catch (Exception e)
			{
				string path = ProjectSettings.GlobalizePath("user://");
				string backupFile = "capturedbackup.save";
				json.SaveJsonToFile(path, backupFile, fullLoad);
				GD.Print(e);
			}
		}
		mainScreen.UpdateHuntIndices();
	}
	
	// Load a save file labelled v0.9.3
	private void Load093(string fullLoad)
	{
		string[] datas = fullLoad.Split("\n");
		int size = datas.Length;
		GlobalSettings.sort = datas[1].Split(':')[1];
		
		var options = new JsonSerializerOptions
		{
			IncludeFields = true,
		};
		
		try
		{
			List<HuntData> allHunts = JsonSerializer.Deserialize<List<HuntData>>(datas[size - 2], options)!;
			List<CapturedData> allCaptures = JsonSerializer.Deserialize<List<CapturedData>>(datas[size - 1], options)!;
		
			foreach (HuntData hunt in allHunts)
			{
				mainScreen.AddHunt(hunt);
			}
			
			foreach (CapturedData hunt in allCaptures)
			{
				mainScreen.AddCaptured(hunt);
			}
		}
		catch (Exception e) // If the file can't be read, dump the save into another file to be recovered later
		{
			string backupFile = "savebackup.save";
			json.SaveJsonToFile(path, backupFile, fullLoad);
			GD.Print(e);
		}
	}
	
	// Load a save file with a strange version number
	private void LoadDefault(string fullLoad)
	{
		string[] datas = fullLoad.Split("\n");
		int size = datas.Length;
		
		var options = new JsonSerializerOptions
		{
			IncludeFields = true,
		};
		
		try
		{
			List<HuntData> allHunts = JsonSerializer.Deserialize<List<HuntData>>(datas[size - 2], options)!;
			List<CapturedData> allCaptures = JsonSerializer.Deserialize<List<CapturedData>>(datas[size - 1], options)!;
		
			foreach (HuntData hunt in allHunts)
			{
				mainScreen.AddHunt(hunt);
			}
			
			foreach (CapturedData hunt in allCaptures)
			{
				mainScreen.AddCaptured(hunt);
			}
		}
		catch (Exception e) // If the file can't be read, dump the save into another file to be recovered later
		{
			string backupFile = "savebackup.save";
			json.SaveJsonToFile(path, backupFile, fullLoad);
			GD.Print(e);
		}
	}
	
	private void ErrorOccurred(Exception e)
	{
		ErrorScreen errorScreen = (ErrorScreen)GD.Load<PackedScene>("res://Scenes/ErrorScreen.tscn").Instantiate();
		AddChild(errorScreen);
		errorScreen.DisplayError(e.ToString());
		errorScreen.Visible = true;
		errorScreen.BackSignal += CloseErrorScreen;
	}
	
	private void CloseErrorScreen()
	{
		ErrorScreen errorScreen = GetNode<ErrorScreen>("ErrorScreen");
		errorScreen.Visible = false;
		RemoveChild(errorScreen);
		errorScreen.Cleanup();
	}
	
	private void AppClosing()
	{
		Save();
	}
}
