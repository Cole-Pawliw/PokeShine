using Godot;
using System;
using System.Collections.Generic;

public partial class MainMenu : Control
{
	List<HuntData> hunts;
	List<CapturedData> finished;
	List<ActiveHunt> activeHunts;
	List<Captured> completedHunts;
	
	TabContainer tabContainer;
	Panel huntPanel, completedPanel;
	Button mainButton, completedButton;
	TextureButton newHuntButton, sortButton;
	
	int x = 7, y = 10, huntY = 84, halfHuntY = 42;
	
	bool sortMode = false;
	int selectedHuntToSort = -1; // -1 means no hunt selected
	int selectedHuntSiblingIndex = -1;
	List<int> flags; // List of ActiveHunts that hav been flagged to be moved
	
	public string sortType = "";
	public bool[] globalSettings = {true, true, true, true, true, true};
	
	[Signal]
	public delegate void HuntButtonPressedEventHandler(int selectedHuntID);
	[Signal]
	public delegate void NewHuntButtonPressedEventHandler(int tab);
	[Signal]
	public delegate void CapturedButtonPressedEventHandler(int selectedHuntID);
	[Signal]
	public delegate void SettingsButtonPressedEventHandler();
	[Signal]
	public delegate void RequestFullSaveEventHandler();
	[Signal]
	public delegate void RequestSmallSaveEventHandler();
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		hunts = new List<HuntData>();
		finished = new List<CapturedData>();
		activeHunts = new List<ActiveHunt>();
		completedHunts = new List<Captured>();
		flags = new List<int>();
		
		tabContainer = GetNode<TabContainer>("TabContainer");
		huntPanel = GetNode<Panel>("TabContainer/HuntContainer/HuntPanel");
		completedPanel = GetNode<Panel>("TabContainer/CompletedContainer/CompletedPanel");
		mainButton = GetNode<Button>("MainButton");
		completedButton = GetNode<Button>("CompletedButton");
		newHuntButton = GetNode<TextureButton>("NewHuntButton");
		sortButton = GetNode<TextureButton>("ToggleSortButton");
		
		SetColors();
	}
	
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (sortMode && selectedHuntToSort != -1)
		{
			int changeIndex = 0;
			Vector2 mousePos = GetViewport().GetMousePosition();
			
			// The relative y coordinate for the hunt, translated from the global y coordinate of the mouse
			float translatedY = mousePos.Y - halfHuntY - activeHunts[selectedHuntToSort].GetGlobalTransformWithCanvas().Origin.Y + activeHunts[selectedHuntToSort].Position.Y;
			
			Vector2 huntPos = new Vector2(x, translatedY); // The Viewport coordinates for the location of the hunt
			activeHunts[selectedHuntToSort].Position = huntPos;
			
			if (selectedHuntToSort != 0 && huntPos.Y < activeHunts[selectedHuntToSort-1].Position.Y)
			{
				changeIndex = -1;
			}
			else if (selectedHuntToSort != activeHunts.Count - 1 && huntPos.Y > activeHunts[selectedHuntToSort+1].Position.Y)
			{
				changeIndex = 1;
			}
			
			if (changeIndex != 0) // Check if the hunt has moved above or below another in the list
			{
				SwapHunts(selectedHuntToSort, changeIndex); // Swap positions with the other hunt
				flags.Add(selectedHuntToSort); // This position now contains the swapped hunt
				selectedHuntToSort += changeIndex; // Change the recorded index of the selected hunt
				if (flags.Contains(selectedHuntToSort))
					flags.Remove(selectedHuntToSort); // Prevent the selected hunt from being adjusted in the case of a swap-back
			}
			
			// Get range of Y coordinates for the ScrollContainer
			ScrollContainer container = GetNode<ScrollContainer>("TabContainer/HuntContainer");
			int topY = (int)container.GetGlobalTransformWithCanvas().Origin.Y; // The y coordinate for the top of the container
			int sizeY = (int)container.Size.Y; // The size of the y dimension for the container
			int botY = topY + sizeY; // The coordinate for the bottom of the container
			
			// Check if the mouse is near the top or bottom of the ScrollContainer
			int edgeBuffer = (int)(sizeY * 0.05); // 5% of the size of the container
			if (mousePos.Y < topY + edgeBuffer)
			{
				container.ScrollVertical = container.ScrollVertical - 3; // Scroll up 10 pixels
			}
			else if (mousePos.Y > botY - edgeBuffer)
			{
				container.ScrollVertical = container.ScrollVertical + 3; // Scroll down 10 pixels
			}
		}
		
		if (flags.Count > 0)
		{
			AdjustPositions(); // Move any swapped hunts toward their appropriate spot
		}
	}
	
	public void SetColors()
	{
		TextureButton settingsButton;
		settingsButton = GetNode<TextureButton>("SettingsButton");
		
		settingsButton.TextureNormal = (Texture2D)GD.Load($"res://Assets/Buttons/{GameHuntInformation.colorMode}/settings.png");
		newHuntButton.TextureNormal = (Texture2D)GD.Load($"res://Assets/Buttons/{GameHuntInformation.colorMode}/create.png");
		sortButton.TextureNormal = (Texture2D)GD.Load($"res://Assets/Buttons/{GameHuntInformation.colorMode}/filter_off.png");
		
		foreach (ActiveHunt hunt in activeHunts)
		{
			hunt.SetColors();
		}
		
		ColorRect bg = GetNode<ColorRect>("Background");
		bg.Color = new Color(GameHuntInformation.backgrounds[GameHuntInformation.colorMode - 1]);
	}
	
	public void UpdateAllSettings(bool[] settings)
	{
		globalSettings = settings;
		foreach (HuntData hunt in hunts)
		{
			hunt.SetSettings(settings);
		}
	}
	
	public void PauseHunts()
	{
		foreach (ActiveHunt hunt in activeHunts)
		{
			hunt.StopTimer();
		}
	}
	
	// Returns a list of HuntData for each hunt in the scene
	public List<HuntData> GetHunts()
	{
		List<HuntData> list = new List<HuntData>(hunts);
		return list;
	}
	
	public List<CapturedData> GetFinished()
	{
		List<CapturedData> list = new List<CapturedData>(finished);
		return list;
	}
	
	private void AddActiveHunt(HuntData hunt)
	{
		// Add a new scene to hold the HuntData
		ActiveHunt newHuntScene = (ActiveHunt)GD.Load<PackedScene>("res://Scenes/ActiveHunt.tscn").Instantiate();
		
		// Insert at the right location
		if (hunt.huntIndex >= activeHunts.Count)
		{
			activeHunts.Add(newHuntScene);
		}
		else
		{
			activeHunts.Insert(hunt.huntIndex, newHuntScene);
		}
		
		// Add the hunt to the current scene at the bottom of the list
		huntPanel.AddChild(newHuntScene);
		UpdateActivePositions();
		
		// Connect signals and initialize the hunt
		newHuntScene.SelectButtonPressed += EmitHuntButtonPressed;
		newHuntScene.SortButtonDown += HuntToSortSelected;
		newHuntScene.SortButtonUp += HuntToSortDeselected;
		newHuntScene.HuntIncremented += SaveActive;
		newHuntScene.InitializeHunt(hunt);
	}
	
	private void AddCompletedHunt(CapturedData hunt)
	{
		// Add a new scene to hold the HuntData
		Captured newHuntScene = (Captured)GD.Load<PackedScene>("res://Scenes/Captured.tscn").Instantiate();
		
		// Insert at the right location
		if (hunt.huntIndex >= completedHunts.Count)
		{
			completedHunts.Add(newHuntScene);
		}
		else
		{
			completedHunts.Insert(hunt.huntIndex, newHuntScene);
		}
		
		// Add the hunt to the current scene in the right position
		completedPanel.AddChild(newHuntScene);
		UpdateCompletedPositions();
		
		// Connect signals and initialize the hunt
		newHuntScene.SelectButtonPressed += EmitCapturedButtonPressed;
		newHuntScene.InitializeInfo(hunt, sortType);
	}
	
	public void AddHunt(HuntData hunt)
	{
		// Check if the hunt already exists, and update if it does
		if (UpdateHunt(hunt)) {
			return; // Return to prevent adding the same hunt twice
		}
		
		// Insert at the right location
		if (hunt.huntIndex > hunts.Count)
		{
			hunts.Add(hunt);
		}
		else
		{
			hunts.Insert(hunt.huntIndex, hunt);
		}
		
		AddActiveHunt(hunt);
		UpdateHuntIndices();
	}
	
	public void AddCaptured(CapturedData hunt)
	{
		// Check if the hunt already exists, and update if it does
		if (UpdateCaptured(hunt)) {
			return; // Return to prevent adding the same hunt twice
		}
		
		// Insert at the right location
		if (hunt.huntIndex > finished.Count)
		{
			finished.Add(hunt);
		}
		else
		{
			finished.Insert(hunt.huntIndex, hunt);
		}
		
		AddCompletedHunt(hunt);
		UpdateHuntIndices();
	}
	
	public bool UpdateHunt(HuntData updatedHunt)
	{
		for (int i = 0; i < hunts.Count; i++)
		{
			if (hunts[i].huntID == updatedHunt.huntID)
			{
				if (updatedHunt.isComplete == false)
				{
					hunts[i] = updatedHunt;
					UpdateHuntLabel(i);
				}
				else
				{
					CapturedData finish = new CapturedData(updatedHunt);
					finish.huntIndex = 0; // Place at the start of the completed hunts
					RemoveActiveHunt(hunts[i].huntID);// Remove the hunt from the active panel
					AddCompletedHunt(finish);// Add the hunt to the completed panel
					hunts.Remove(updatedHunt); // Update the list of hunts
					AddCaptured(finish); // Update the list of finished hunts
					
					// Update display information
					UpdateHuntIndices();
					UpdateActivePositions();
					UpdateCompletedPositions();
				}
				return true;
			}
		}
		
		return false;
	}
	
	public bool UpdateCaptured(CapturedData updatedHunt)
	{
		for (int i = 0; i < finished.Count; i++)
		{
			if (finished[i].huntID == updatedHunt.huntID)
			{
				finished[i] = updatedHunt;
				UpdateCapturedLabel(i);
				return true;
			}
		}
		
		return false;
	}
	
	public void UpdateHuntLabel(int huntIndex)
	{
		foreach (ActiveHunt hunt in activeHunts)
		{
			if (hunt.data.huntID == hunts[huntIndex].huntID)
			{
				hunt.data = hunts[huntIndex];
				hunt.UpdateLabels();
			}
		}
	}
	
	public void UpdateCapturedLabel(int huntIndex)
	{
		foreach (Captured hunt in completedHunts)
		{
			if (hunt.data.huntID == finished[huntIndex].huntID)
			{
				hunt.data = finished[huntIndex];
				hunt.UpdateLabel(sortType);
			}
		}
	}
	
	public void RemoveHunt(HuntData deletedHunt)
	{
		foreach (HuntData hunt in hunts)
		{
			if (hunt.huntID == deletedHunt.huntID)
			{
				RemoveActiveHunt(hunt.huntID);
				hunts.Remove(hunt);
				return;
			}
		}
		UpdateHuntIndices();
	}
	
	public void RemoveCaptured(CapturedData deletedHunt)
	{
		foreach (CapturedData hunt in finished)
		{
			if (hunt.huntID == deletedHunt.huntID)
			{
				RemoveCompletedHunt(hunt.huntID);
				finished.Remove(hunt);
				return;
			}
		}
		UpdateHuntIndices();
	}
	
	private void RemoveActiveHunt(int deletedHuntID)
	{
		foreach (ActiveHunt hunt in activeHunts)
		{
			if (hunt.data.huntID == deletedHuntID)
			{
				huntPanel.RemoveChild(hunt); // Remove the child from the scene tree
				activeHunts.Remove(hunt); // Remove the hunt from the list of activeHunts
				UpdateActivePositions(); // Update the visible activeHunts to reflect the removed hunt
				hunt.Cleanup(); // Delete the removed hunt from memory
				return;
			}
		}
	}
	
	private void RemoveCompletedHunt(int deletedHuntID)
	{
		foreach (Captured hunt in completedHunts)
		{
			if (hunt.data.huntID == deletedHuntID)
			{
				completedPanel.RemoveChild(hunt); // Remove the child from the scene tree
				completedHunts.Remove(hunt); // Remove the hunt from the list of activeHunts
				UpdateCompletedPositions(); // Update the visible activeHunts to reflect the removed hunt
				hunt.Cleanup(); // Delete the removed hunt from memory
				return;
			}
		}
	}
	
	public HuntData GetHunt(int id)
	{
		foreach (HuntData hunt in hunts)
		{
			if (hunt.huntID == id)
			{
				return hunt;
			}
		}
		
		return null;
	}
	
	public CapturedData GetCaptured(int id)
	{
		foreach (CapturedData hunt in finished)
		{
			if (hunt.huntID == id)
			{
				return hunt;
			}
		}
		
		return null;
	}
	
	public void UpdateHuntIndices()
	{
		int i = 0;
		foreach (ActiveHunt hunt in activeHunts)
		{
			hunt.data.huntIndex = i;
			i++;
		}
		
		i = 0;
		foreach (Captured hunt in completedHunts)
		{
			hunt.data.huntIndex = i;
			i++;
		}
	}
	
	public void UpdateHuntSprite(int id)
	{
		int index = GetActiveHuntIndex(id);
		ActiveHunt hunt = activeHunts[index];
		// This is bad but it saves writing a function that does 90% of this function
		// Basically jump starts the ActiveHunt to change its sprite
		hunt.InitializeHunt(hunt.data);
	}
	
	public void UpdateCapturedSprite(int id)
	{
		int index = GetCompletedHuntIndex(id);
		Captured hunt = completedHunts[index];
		// This is bad but it saves writing a function that does 90% of this function
		// Basically jump starts the ActiveHunt to change its sprite
		hunt.InitializeInfo(hunt.data, sortType);
	}
	
	public void SortHunts()
	{
		hunts.Sort((x, y) => x.huntIndex.CompareTo(y.huntIndex));
	}
	
	public int GetHuntIndex(int id)
	{
		for (int i = 0; i < hunts.Count; i++)
		{
			if (hunts[i].huntID == id)
			{
				return i;
			}
		}
		
		return -1;
	}
	
	public int GetCapturedIndex(int id)
	{
		for (int i = 0; i < finished.Count; i++)
		{
			if (finished[i].huntID == id)
			{
				return i;
			}
		}
		
		return -1;
	}
	
	public int GetActiveHuntIndex(int id)
	{
		for (int i = 0; i < activeHunts.Count; i++)
		{
			if (activeHunts[i].data.huntID == id)
			{
				return i;
			}
		}
		
		return -1;
	}
	
	public int GetCompletedHuntIndex(int id)
	{
		for (int i = 0; i < completedHunts.Count; i++)
		{
			if (completedHunts[i].data.huntID == id)
			{
				return i;
			}
		}
		
		return -1;
	}
	
	private void UpdateActivePositions()
	{
		int increment = huntY + y; // The difference between the tops of two ActiveHunts
		int yTracker = y; // The y coordinate for the position of the next ActiveHunt to be placed
		foreach (ActiveHunt hunt in activeHunts)
		{
			hunt.Position = new Vector2(x, yTracker);
			yTracker += increment;
		}
		
		// Change the size of the container panel to fit all the hunts
		if (yTracker != huntPanel.CustomMinimumSize.Y)
		{
			huntPanel.CustomMinimumSize = new Vector2(tabContainer.Size.X, Math.Max(yTracker, tabContainer.Size.Y));
		}
	}
	
	private void UpdateCompletedPositions()
	{
		int increment = huntY + y; // The difference between the tops of two ActiveHunts
		int yTracker = y; // The y coordinate for the position of the next ActiveHunt to be placed
		foreach (Captured hunt in completedHunts)
		{
			hunt.Position = new Vector2(x, yTracker);
			yTracker += increment;
		}
		
		// Change the size of the container panel to fit all the hunts
		if (yTracker != completedPanel.CustomMinimumSize.Y)
		{
			completedPanel.CustomMinimumSize = new Vector2(tabContainer.Size.X, Math.Max(yTracker, tabContainer.Size.Y));
		}
	}
	
	private void SetMainPanel(bool button_pressed)
	{
		if (button_pressed == true) {
			tabContainer.CurrentTab = 0;
		}
		SetButtonTextures("create");
	}
	
	private void SetCompletedPanel(bool button_pressed)
	{
		if (button_pressed == true) {
			tabContainer.CurrentTab = 1;
		}
		PauseHunts();
		SetButtonTextures("plus");
	}
	
	private void SetButtonTextures(string baseName)
	{
		newHuntButton.TextureNormal = (Texture2D)GD.Load($"res://Assets/Buttons/{GameHuntInformation.colorMode}/{baseName}.png");
		newHuntButton.TextureDisabled = (Texture2D)GD.Load($"res://Assets/Buttons/disabled/{baseName}_disabled.png");
	}
	
	private void SortButtonPressed()
	{
		if (tabContainer.CurrentTab == 0)
		{
			ToggleSortMode();
		}
		else
		{
			OpenSelector();
		}
	}
	
	private void ToggleSortMode()
	{
		sortMode = !sortMode;
		if (sortMode)
		{
			PauseHunts();
			sortButton.TextureNormal = (Texture2D)GD.Load($"res://Assets/Buttons/{GameHuntInformation.colorMode}/filter.png");
			mainButton.Disabled = true;
			completedButton.Disabled = true;
			newHuntButton.Disabled = true;
		}
		else
		{
			sortButton.TextureNormal = (Texture2D)GD.Load($"res://Assets/Buttons/{GameHuntInformation.colorMode}/filter_off.png");
			mainButton.Disabled = false;
			completedButton.Disabled = false;
			newHuntButton.Disabled = false;
			SaveAll();
		}
		
		// Iterate through all ActiveHunts and call ToggleSort()
		foreach (ActiveHunt hunt in activeHunts)
		{
			hunt.ToggleSort();
		}
	}
	
	private void HuntToSortSelected(int id)
	{
		selectedHuntToSort = GetActiveHuntIndex(id);
		selectedHuntSiblingIndex = activeHunts[selectedHuntToSort].GetIndex();
		huntPanel.MoveChild(activeHunts[selectedHuntToSort], -1);
	}
	
	private void HuntToSortDeselected()
	{
		huntPanel.MoveChild(activeHunts[selectedHuntToSort], selectedHuntSiblingIndex);
		flags.Add(selectedHuntToSort); // The held hunt will end wherever the mouse was and must return to its natural position
		selectedHuntSiblingIndex = -1;
		selectedHuntToSort = -1;
	}
	
	private void SwapHunts(int index, int change)
	{
		if (change == 0)
		{
			return;
		}
		
		// Swap positions in the activeHunts list
		ActiveHunt temp = activeHunts[index];
		activeHunts[index] = activeHunts[index + change];
		activeHunts[index + change] = temp;
		
		// Swap HuntData indices
		int tempI = activeHunts[index].data.huntIndex;
		activeHunts[index].data.huntIndex = activeHunts[index + change].data.huntIndex;
		activeHunts[index + change].data.huntIndex = tempI;
	}
	
	private void AdjustPositions()
	{
		if (flags.Count == 0)
		{
			return;
		}
		for (int i = flags.Count - 1; i >= 0; i--) // Iterate backwards to allow for removal of elements
		{
			int expectedY = (huntY + y) * flags[i] + y; // The expected y value for the current index
			int yDifference = expectedY - (int)activeHunts[flags[i]].Position.Y; // The difference between expected and actual y values
			if (yDifference <= 5 && yDifference >= -5) // If the hunt is close to where it should be
			{
				activeHunts[flags[i]].Position = new Vector2(x, expectedY); // Move the hunt to the right spot
				flags.RemoveAt(i); // Hunt is in the right spot, no longer flagged to be moved
			}
			else
			{
				activeHunts[flags[i]].Position = new Vector2(x, activeHunts[flags[i]].Position.Y + 5 * Math.Sign(yDifference)); // Increase or decrease based on the difference
			}
		}
	}
	
	private void OpenSelector()
	{
		OptionSelect selectScreen = (OptionSelect)GD.Load<PackedScene>("res://Scenes/OptionSelect.tscn").Instantiate();
		selectScreen.Name = "TypeSelect";
		AddChild(selectScreen);
		List<string> itemList = new List<string>(["Start Date", "End Date", "Pokemon", "Game", "Generation"]);
		
		selectScreen.CreateList(itemList, false);
		selectScreen.CloseMenu += SortTypeSelected;
	}
	
	private void SortTypeSelected(string selectedOption)
	{
		CloseSelector("TypeSelect");
		if (selectedOption == "")
		{
			return;
		}
		sortType = selectedOption;
		SortCaptured(selectedOption);
		
		OptionSelect selectScreen = (OptionSelect)GD.Load<PackedScene>("res://Scenes/OptionSelect.tscn").Instantiate();
		selectScreen.Name = "AscendingSelect";
		AddChild(selectScreen);
		List<string> itemList = new List<string>(["Ascending", "Descending"]);
		
		selectScreen.CreateList(itemList, false);
		selectScreen.CloseMenu += SortAscendingSelected;
		
		foreach (Captured hunt in completedHunts)
		{
			hunt.UpdateLabel(sortType);
		}
	}
	
	private void SortAscendingSelected(string selectedOption)
	{
		if (selectedOption == "Descending")
		{
			completedHunts.Reverse();
			UpdateCompletedPositions();
			UpdateHuntIndices();
		}
		CloseSelector("AscendingSelect");
	}
	
	private void CloseSelector(string selectorName)
	{
		OptionSelect selector = GetNode<OptionSelect>(selectorName);
		selector.Visible = false;
		RemoveChild(selector);
		selector.Cleanup();
		SaveAll();
	}
	
	private void SortCaptured(string sortMethod)
	{
		switch (sortMethod)
		{
			case "Start Date":
				completedHunts.Sort((x, y) => x.data.startDate.CompareTo(y.data.startDate));
				break;
			case "End Date":
				completedHunts.Sort((x, y) => x.data.endDate.CompareTo(y.data.endDate));
				break;
			case "Pokemon":
				completedHunts.Sort((x, y) => x.data.pokemon.CompareTo(y.data.pokemon));
				break;
			case "Game":
				completedHunts.Sort((x, y) => x.data.huntGame.CompareTo(y.data.huntGame));
				break;
			case "Generation":
				// Start by sorting by game
				completedHunts.Sort((x, y) => x.data.huntGame.CompareTo(y.data.huntGame));
				
				// Then sort by generation
				completedHunts.Sort((x, y) => 
					GameHuntInformation.gameInfoDict[x.data.huntGame].methodID.CompareTo(
						GameHuntInformation.gameInfoDict[y.data.huntGame].methodID
					));
				break;
		}
		
		UpdateCompletedPositions();
		UpdateHuntIndices();
	}
	
	private void EmitHuntButtonPressed(int id)
	{
		EmitSignal("HuntButtonPressed", id);
	}
	
	private void EmitCapturedButtonPressed(int id)
	{
		EmitSignal("CapturedButtonPressed", id);
	}
	
	private void OpenNewHuntScreen()
	{
		EmitSignal("NewHuntButtonPressed", tabContainer.CurrentTab);
	}
	
	private void OpenSettingsScreen()
	{
		EmitSignal("SettingsButtonPressed");
	}
	
	private void SaveAll()
	{
		EmitSignal("RequestFullSave");
	}
	
	private void SaveActive()
	{
		EmitSignal("RequestSmallSave");
	}
	
	// Destroy this UI element
	public void Cleanup()
	{
		QueueFree();
	}
}
