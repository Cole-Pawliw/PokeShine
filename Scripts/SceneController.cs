using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;

/*
Bugs and Changes
- Add clear all button to OptionSelect
- Different font
- Edit button design
- REMOVE TRY CATCH ON EVERYTHING BEFORE RELEASE
*/

/*
Extra features
- Active hunt stats page (odds graph, other detailed info)
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
	string versionNumber = "0.9.6";
	
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
		try
		{
			HuntData selectedHunt = mainScreen.GetHunt(selectedHuntID);
			mainScreen.PauseHunts();
			huntScreen.InitializeHunt(new HuntData(selectedHunt));
			huntScreen.Visible = true;
			mainScreen.Visible = false;
		}
		catch (Exception e)
		{
			ErrorOccurred(e);
		}
			
	}
	
	private void OpenStats(int selectedHuntID)
	{
		try
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
		catch (Exception e)
		{
			ErrorOccurred(e);
		}
	}
	
	private void CloseHunt()
	{
		try
		{
			HuntData data = huntScreen.data;
			mainScreen.UpdateHunt(data);
			mainScreen.Visible = true;
			huntScreen.Visible = false;
			Save();
		}
		catch (Exception e)
		{
			ErrorOccurred(e);
		}
	}
	
	private void CloseStats()
	{
		try
		{
			FinishedStats statsScreen = GetNode<FinishedStats>("FinishedStats");
			mainScreen.Visible = true;
			statsScreen.Visible = false;
			RemoveChild(statsScreen);
			statsScreen.Cleanup();
		}
		catch (Exception e)
		{
			ErrorOccurred(e);
		}
	}
	
	private void CreateNewHunt(int mode)
	{
		try
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
		catch (Exception e)
		{
			ErrorOccurred(e);
		}
	}
	
	private void DeleteHunt()
	{
		try
		{
			HuntData data = huntScreen.data;
			mainScreen.RemoveHunt(data);
			mainScreen.Visible = true;
			huntScreen.Visible = false;
			Save();
		}
		catch (Exception e)
		{
			ErrorOccurred(e);
		}
	}
	
	private void DeleteCaptured()
	{
		try
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
		catch (Exception e)
		{
			ErrorOccurred(e);
		}
	}
	
	private void UpdateActiveSprite()
	{
		try
		{
			HuntData data = huntScreen.data;
			mainScreen.UpdateHunt(data);
			mainScreen.UpdateHuntSprite(data.huntID);
			Save();
		}
		catch (Exception e)
		{
			ErrorOccurred(e);
		}
	}
	
	private void UpdateCaptured()
	{
		try
		{
			FinishedStats screen = GetNode<FinishedStats>("FinishedStats");
			CapturedData data = screen.data;
			mainScreen.UpdateCaptured(data);
			mainScreen.UpdateCapturedSprite(data.huntID);
			Save();
		}
		catch (Exception e)
		{
			ErrorOccurred(e);
		}
	}
	
	private void CloseHuntCreator()
	{
		try
		{
			HuntCreator startHuntScreen = GetNode<HuntCreator>("HuntCreator");
			mainScreen.Visible = true;
			startHuntScreen.Visible = false;
			RemoveChild(startHuntScreen);
			startHuntScreen.Cleanup();
		}
		catch (Exception e)
		{
			ErrorOccurred(e);
		}
	}
	
	private void StartHuntSignalReceiver(string gameName, string method, string route, bool charm, int oddsBonus)
	{
		try
		{
			string startDT = Time.GetDatetimeStringFromSystem().Split('T')[0];
			HuntCreator startHuntScreen = GetNode<HuntCreator>("HuntCreator");
			HuntData huntToAdd = new HuntData(startHuntScreen.pokemonSelected, gameName, method, route, charm, oddsBonus, startDT);
			
			// Set common settings (not made yet
			huntToAdd.showShiny = mainScreen.globalSettings[0];
			huntToAdd.showRegular = mainScreen.globalSettings[1];
			huntToAdd.showOdds = mainScreen.globalSettings[2];
			huntToAdd.showFullTimer = mainScreen.globalSettings[4];
			huntToAdd.showMiniTimer = mainScreen.globalSettings[5];
			// Gross if statement but this function is rarely called
			if (huntToAdd.huntMethod == "Poke Radar" || huntToAdd.huntMethod == "Chain Fishing" || huntToAdd.huntMethod == "Dex Nav"
				|| huntToAdd.huntMethod == "SOS Chain" || huntToAdd.huntMethod == "Catch Combo" || (huntToAdd.huntMethod == "Mass Outbreak"
				&& (huntToAdd.huntGame == "Scarlet" || huntToAdd.huntGame == "Violet")))
			{
				huntToAdd.showCombo = mainScreen.globalSettings[3];
			}
			
			mainScreen.AddHunt(huntToAdd);
			Save(); // Update save file with newly added hunt
			
			CloseHuntCreator();
		}
		catch (Exception e)
		{
			ErrorOccurred(e);
		}
	}
	
	private void CloseCapturedCreator()
	{
		try
		{
			CapturedCreator startHuntScreen = GetNode<CapturedCreator>("CapturedCreator");
			mainScreen.Visible = true;
			startHuntScreen.Visible = false;
			RemoveChild(startHuntScreen);
			startHuntScreen.Cleanup();
		}
		catch (Exception e)
		{
			ErrorOccurred(e);
		}
	}
	
	private void AddCaptured()
	{
		try
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
		catch (Exception e)
		{
			ErrorOccurred(e);
		}
	}
	
	private void FinishHunt(string nickname, string ball, string gender)
	{
		try
		{
			string endDT = Time.GetDatetimeStringFromSystem().Split('T')[0];
			CapturedData finishedHunt = new CapturedData(huntScreen.data, endDT, nickname, ball, gender);
			mainScreen.RemoveHunt(huntScreen.data);
			mainScreen.AddCaptured(finishedHunt);
			mainScreen.Visible = true;
			huntScreen.Visible = false;
			Save();
		}
		catch (Exception e)
		{
			ErrorOccurred(e);
		}
	}
	
	private void OpenSettings()
	{
		try
		{
			UserSettings settingsScreen = (UserSettings)GD.Load<PackedScene>("res://Scenes/UserSettings.tscn").Instantiate();
			AddChild(settingsScreen);
			settingsScreen.BackButtonPressed += CloseSettings;
			settingsScreen.NewColors += SetColors;
			settingsScreen.SetSettings(mainScreen.globalSettings);
			
			settingsScreen.Visible = true;
			mainScreen.Visible = false;
			huntScreen.Visible = false;
			
			mainScreen.PauseHunts();
		}
		catch (Exception e)
		{
			ErrorOccurred(e);
		}
	}
	
	private void CloseSettings()
	{
		try
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
		catch (Exception e)
		{
			ErrorOccurred(e);
		}
	}
	
	private void SetColors()
	{
		mainScreen.SetColors();
		huntScreen.SetColors();
		Theme = (Theme)GD.Load($"res://ColorTheme{GameHuntInformation.colorMode}.tres");
	}
	
	private void Save()
	{
		try
		{
			var options = new JsonSerializerOptions
			{
				IncludeFields = true,
			};
			
			string fullSave = $"v{versionNumber}\nsortby:{mainScreen.sortType}\n";
			fullSave += $"colors:{GameHuntInformation.colorMode}\n";
			fullSave += $"show shiny:{mainScreen.globalSettings[0]}\n";
			fullSave += $"show regular:{mainScreen.globalSettings[1]}\n";
			fullSave += $"show odds:{mainScreen.globalSettings[2]}\n";
			fullSave += $"show combo:{mainScreen.globalSettings[3]}\n";
			fullSave += $"show hunt timer:{mainScreen.globalSettings[4]}\n";
			fullSave += $"show encounter timer:{mainScreen.globalSettings[5]}\n";
			json.SaveJsonToFile(path, saveFileName, fullSave);
			SaveActiveHunts();
			SaveCaptured();
		}
		catch (Exception e)
		{
			ErrorOccurred(e);
		}
			
		
	}
	
	private void SaveActiveHunts()
	{
		try
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
		catch (Exception e)
		{
			ErrorOccurred(e);
		}
			
	}
	
	private void SaveCaptured()
	{
		try
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
		catch (Exception e)
		{
			ErrorOccurred(e);
		}
			
	}
	
	private void Load()
	{
		try
		{
			string fullLoad = json.LoadResourceFromFile(path, saveFileName);
			if (fullLoad == null || fullLoad == "")
			{
				return;
			}
			
			string[] datas = fullLoad.Split("\n");
			
			switch (datas[0])
			{
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
		catch (Exception e)
		{
			ErrorOccurred(e);
		}
			
	}
	
	private void Load096(string fullLoad)
	{
		try
		{
			string[] datas = fullLoad.Split("\n");
			int size = datas.Length;
			mainScreen.sortType = datas[1].Split(':')[1];
			GameHuntInformation.colorMode = Int32.Parse(datas[2].Split(':')[1]);
			mainScreen.globalSettings = [ bool.Parse(datas[3].Split(':')[1]), bool.Parse(datas[4].Split(':')[1]),
										  bool.Parse(datas[5].Split(':')[1]), bool.Parse(datas[6].Split(':')[1]),
										  bool.Parse(datas[7].Split(':')[1]), bool.Parse(datas[8].Split(':')[1]) ];
			
			LoadActiveHunts();
			LoadCaptured();
		}
		catch (Exception e)
		{
			ErrorOccurred(e);
		}
	}
	
	private void Load094(string fullLoad)
	{
		try
		{
			string[] datas = fullLoad.Split("\n");
			int size = datas.Length;
			mainScreen.sortType = datas[1].Split(':')[1];
			
			LoadActiveHunts();
			LoadCaptured();
		}
		catch (Exception e)
		{
			ErrorOccurred(e);
		}
	}
	
	private void LoadActiveHunts()
	{
		try
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
		}
		catch (Exception e)
		{
			ErrorOccurred(e);
		}
			
	}
	
	private void LoadCaptured()
	{
		try
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
					ErrorOccurred(e);
				}
			}
		}
		catch (Exception e)
		{
			ErrorOccurred(e);
		}
		
	}
	
	// Load a save file labelled v0.9.3
	private void Load093(string fullLoad)
	{
		try
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
				ErrorOccurred(e);
			}
		}
		catch (Exception e)
		{
			ErrorOccurred(e);
		}
		
	}
	
	// Load a save file with a strange version number
	private void LoadDefault(string fullLoad)
	{
		try
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
				ErrorOccurred(e);
			}
		}
		catch (Exception e)
		{
			ErrorOccurred(e);
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
