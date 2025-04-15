using Godot;
using System;
using System.Collections.Generic;

public partial class MainMenu : Control
{
	List<HuntData> hunts;
	List<ActiveHunt> activeHunts;
	List<Captured> completedHunts;
	
	TabContainer tabContainer;
	Panel huntPanel;
	Panel completedPanel;
	
	int x = 7, y = 10, huntY = 84, halfHuntY = 42;
	
	bool sortMode = false;
	int selectedHuntToSort = -1; // -1 means no hunt selected
	int selectedHuntSiblingIndex = -1;
	List<int> flags; // List of ActiveHunts that hav been flagged to be moved
	
	[Signal]
	public delegate void HuntButtonPressedEventHandler(int selectedHuntID);
	[Signal]
	public delegate void NewHuntButtonPressedEventHandler();
	[Signal]
	public delegate void CapturedButtonPressedEventHandler(int selectedHuntID);
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		hunts = new List<HuntData>();
		activeHunts = new List<ActiveHunt>();
		completedHunts = new List<Captured>();
		flags = new List<int>();
		
		tabContainer = GetNode<TabContainer>("TabContainer");
		huntPanel = GetNode<Panel>("TabContainer/HuntContainer/HuntPanel");
		completedPanel = GetNode<Panel>("TabContainer/CompletedContainer/CompletedPanel");
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
	
	// Returns a list of HuntData for each hunt in the scene
	public List<HuntData> GetHunts()
	{
		List<HuntData> list = new List<HuntData>(hunts);
		return list;
	}
	
	private void AddActiveHunt(HuntData hunt)
	{
		// Add a new scene to hold the HuntData
		ActiveHunt newHuntScene = (ActiveHunt)GD.Load<PackedScene>("res://Scenes/ActiveHunt.tscn").Instantiate();
		activeHunts.Add(newHuntScene);
		
		// Add the hunt to the current scene at the bottom of the list
		huntPanel.AddChild(newHuntScene);
		UpdateActivePositions();
		
		// Connect signals and initialize the hunt
		newHuntScene.SelectButtonPressed += EmitHuntButtonPressed;
		newHuntScene.SortButtonDown += HuntToSortSelected;
		newHuntScene.SortButtonUp += HuntToSortDeselected;
		newHuntScene.InitializeHunt(hunt);
	}
	
	private void AddCompletedHunt(HuntData hunt)
	{
		// Add a new scene to hold the HuntData
		Captured newHuntScene = (Captured)GD.Load<PackedScene>("res://Scenes/Captured.tscn").Instantiate();
		completedHunts.Add(newHuntScene);
		
		// Add the hunt to the current scene at the bottom of the list
		completedPanel.AddChild(newHuntScene);
		UpdateCompletedPositions();
		
		// Connect signals and initialize the hunt
		newHuntScene.SelectButtonPressed += EmitCapturedButtonPressed;
		newHuntScene.InitializeInfo(hunt);
	}
	
	public void AddHunt(HuntData hunt)
	{
		// Check if the hunt already exists, and update if it does
		if (UpdateHunt(hunt)) {
			return; // Return to prevent adding the same hunt twice
		}
		
		hunts.Add(hunt);
		
		if (hunt.isComplete)
		{
			AddCompletedHunt(hunt);
		}
		else
		{
			AddActiveHunt(hunt);
		}
	}
	
	public bool UpdateHunt(HuntData updatedHunt)
	{
		for (int i = 0; i < hunts.Count; i++)
		{
			if (hunts[i].huntID == updatedHunt.huntID)
			{
				if (updatedHunt.isComplete == hunts[i].isComplete)
				{
					hunts[i] = updatedHunt;
					UpdateHuntLabel(i);
				}
				else if (updatedHunt.isComplete && !hunts[i].isComplete)
				{
					hunts[i] = updatedHunt; // Update the list of hunts
					RemoveActiveHunt(hunts[i].huntID);// Remove the hunt from the active panel
					AddCompletedHunt(hunts[i]);// Add the hunt to the completed panel
					
					// Update display information
					UpdateActivePositions();
					UpdateCompletedPositions();
				}
				return true;
			}
		}
		
		return false;
	}
	
	public void UpdateHuntLabel(int huntIndex)
	{
		if (hunts[huntIndex].isComplete)
		{
			foreach (Captured hunt in completedHunts)
			{
				if (hunt.data.huntID == hunts[huntIndex].huntID)
				{
					hunt.data = hunts[huntIndex];
					hunt.UpdateLabel();
				}
			}
		}
		else
		{
			foreach (ActiveHunt hunt in activeHunts)
			{
				if (hunt.data.huntID == hunts[huntIndex].huntID)
				{
					hunt.data = hunts[huntIndex];
					hunt.UpdateLabel();
				}
			}
		}
	}
	
	public void RemoveHunt(HuntData deletedHunt)
	{
		foreach (HuntData hunt in hunts)
		{
			if (hunt.huntID == deletedHunt.huntID)
			{
				if (hunt.isComplete)
				{
					RemoveCompletedHunt(hunt.huntID);
				}
				else
				{
					RemoveActiveHunt(hunt.huntID);
				}
				hunts.Remove(hunt);
				return;
			}
		}
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
			hunt.Position = new Vector2(x, y);
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
	}
	
	private void SetCompletedPanel(bool button_pressed)
	{
		if (button_pressed == true) {
			tabContainer.CurrentTab = 1;
		}
	}
	
	private void ToggleSortMode()
	{
		TextureButton button = GetNode<TextureButton>("ToggleSortButton");
		
		sortMode = !sortMode;
		if (sortMode)
		{
			button.TextureNormal = (Texture2D)GD.Load($"res://Assets/filter.png");
		}
		else
		{
			button.TextureNormal = (Texture2D)GD.Load($"res://Assets/filter_off.png");
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
		
		ActiveHunt temp = activeHunts[index];
		activeHunts[index] = activeHunts[index + change];
		activeHunts[index + change] = temp;
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
		EmitSignal("NewHuntButtonPressed");
	}
	
	// Destroy this UI element
	public void Cleanup()
	{
		QueueFree();
	}
}
