using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;

/*
Bugs and Changes
- Add forms
- Verify file loads
- Global Settings (for what sprites to show)
- Different font
- Edit button design
- Box sprites for ItemList
- Fix android back signal
- Bug with LGPE routes
*/

/*
Extra features
- Active hunt stats page (odds graph, other detailed info)
- Per-route pokemon availability (very complicated to make, might not get added)
- customizable colours (means expanding save files)
- GSC sprites?
*/

public partial class SceneController : Node
{
	MainMenu mainScreen;
	ShinyHuntScreen huntScreen;
	JsonManager json;
	string saveFileName = "savefile.save", activeFileName = "ActiveHunts.save", capturedFileName = "CapturedHunts.save";
	string path;
	string versionNumber = "0.9.4";
	
	double timer = 0;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		path = ProjectSettings.GlobalizePath("user://");
		mainScreen = GetNode<MainMenu>("MainMenu");
		huntScreen = GetNode<ShinyHuntScreen>("ShinyHuntScreen");
		json = GetNode<JsonManager>("JsonManager");
		
		mainScreen.HuntButtonPressed += OpenHunt;
		mainScreen.CapturedButtonPressed += OpenStats;
		mainScreen.NewHuntButtonPressed += CreateNewHunt;
		mainScreen.InfoButtonPressed += OpenAppInfo;
		mainScreen.RequestFullSave += Save;
		mainScreen.RequestSmallSave += SaveActiveHunts;
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
		mainScreen.Visible = true;
		huntScreen.Visible = false;
		Save();
	}
	
	private void OpenAppInfo()
	{
		AppInfoScreen infoScreen = (AppInfoScreen)GD.Load<PackedScene>("res://Scenes/AppInfoScreen.tscn").Instantiate();
		AddChild(infoScreen);
		infoScreen.BackButtonPressed += CloseAppInfo;
		
		infoScreen.Visible = true;
		mainScreen.Visible = false;
		huntScreen.Visible = false;
		
		mainScreen.PauseHunts();
	}
	
	private void CloseAppInfo()
	{
		AppInfoScreen infoScreen = GetNode<AppInfoScreen>("AppInfoScreen");
		mainScreen.Visible = true;
		infoScreen.Visible = false;
		RemoveChild(infoScreen);
		infoScreen.Cleanup();
	}
	
	private void Save()
	{
		var options = new JsonSerializerOptions
		{
			IncludeFields = true,
		};
		
		string fullSave = $"v{versionNumber}\nsortby:{mainScreen.sortType}";
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
		huntData = huntData.Remove(huntData.Length - 1); // Remove final \n
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
		capturedData = capturedData.Remove(capturedData.Length - 1); // Remove final \n
		json.SaveJsonToFile(path, capturedFileName, capturedData);
	}
	
	private void Load()
	{
		string fullLoad = json.LoadJsonFromFile(path, saveFileName);
		if (fullLoad == null)
		{
			return;
		}
		
		string[] datas = fullLoad.Split("\n");
		
		switch (datas[0])
		{
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
	
	private void Load094(string fullLoad)
	{
		string[] datas = fullLoad.Split("\n");
		int size = datas.Length;
		mainScreen.sortType = datas[1].Split(':')[1];
		
		LoadActiveHunts();
		LoadCaptured();
	}
	
	private void LoadActiveHunts()
	{
		string fullLoad = json.LoadJsonFromFile(path, activeFileName);
		if (fullLoad == null)
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
	}
	
	private void LoadCaptured()
	{
		string fullLoad = json.LoadJsonFromFile(path, capturedFileName);
		if (fullLoad == null)
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
	}
	
	// Load a save file labelled v0.9.3
	private bool Load093(string fullLoad)
	{
		string[] datas = fullLoad.Split("\n");
		int size = datas.Length;
		mainScreen.sortType = datas[1].Split(':')[1];
		
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
				if (VerifyLoadedHunt(hunt))
				{
					mainScreen.AddHunt(hunt);
				}
			}
			
			foreach (CapturedData hunt in allCaptures)
			{
				if (VerifyLoadedCaptured(hunt))
				{
					mainScreen.AddCaptured(hunt);
				}
			}
		}
		catch (Exception e) // If the file can't be read, dump the save into another file to be recovered later
		{
			string backupFile = "savebackup.save";
			json.SaveJsonToFile(path, backupFile, fullLoad);
			GD.Print(e);
		}
		
		return true;
	}
	
	// Load a save file with a strange version number
	private bool LoadDefault(string fullLoad)
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
				if (VerifyLoadedHunt(hunt))
				{
					mainScreen.AddHunt(hunt);
				}
			}
			
			foreach (CapturedData hunt in allCaptures)
			{
				if (VerifyLoadedCaptured(hunt))
				{
					mainScreen.AddCaptured(hunt);
				}
			}
		}
		catch (Exception e) // If the file can't be read, dump the save into another file to be recovered later
		{
			string backupFile = "savebackup.save";
			json.SaveJsonToFile(path, backupFile, fullLoad);
			GD.Print(e);
		}
		
		return true;
	}
	
	private bool VerifyLoadedHunt(HuntData hunt)
	{
		return true;
	}
	
	private bool VerifyLoadedCaptured(CapturedData hunt)
	{
		return true;
	}
	
	private void AppClosing()
	{
		Save();
	}
}
