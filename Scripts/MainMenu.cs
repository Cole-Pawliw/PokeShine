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
		
		tabContainer = GetNode<TabContainer>("TabContainer");
		huntPanel = GetNode<Panel>("TabContainer/HuntContainer/HuntPanel");
		completedPanel = GetNode<Panel>("TabContainer/CompletedContainer/CompletedPanel");
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
	
	private void UpdateActivePositions()
	{
		int x = 7, y = 10;
		int increment = 84 + y; // y dimension of an ActiveHunt + buffer space
		foreach (ActiveHunt hunt in activeHunts)
		{
			hunt.Position = new Vector2(x, y);
			y += increment;
		}
		
		// Change the size of the container panel to fit all the hunts
		if (y != huntPanel.CustomMinimumSize.Y)
		{
			huntPanel.CustomMinimumSize = new Vector2(tabContainer.Size.X, Math.Max(y, tabContainer.Size.Y));
		}
	}
	
	private void UpdateCompletedPositions()
	{
		int x = 7, y = 10;
		int increment = 84 + y; // y dimension of an ActiveHunt + buffer space
		foreach (Captured hunt in completedHunts)
		{
			hunt.Position = new Vector2(x, y);
			y += increment;
		}
		
		// Change the size of the container panel to fit all the hunts
		if (y != completedPanel.CustomMinimumSize.Y)
		{
			completedPanel.CustomMinimumSize = new Vector2(tabContainer.Size.X, Math.Max(y, tabContainer.Size.Y));
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
