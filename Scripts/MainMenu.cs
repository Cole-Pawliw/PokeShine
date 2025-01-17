using Godot;
using System;
using System.Collections.Generic;

public partial class MainMenu : Control
{
	List<ActiveHunt> hunts;
	List<Captured> finishedHunts;
	
	TabContainer tabContainer;
	Panel huntPanel;
	Panel capturedPanel;
	
	[Signal]
	public delegate void HuntButtonPressedEventHandler(int selectedHuntID);
	[Signal]
	public delegate void NewHuntButtonPressedEventHandler();
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		hunts = new List<ActiveHunt>();
		finishedHunts = new List<Captured>();
		
		tabContainer = GetNode<TabContainer>("TabContainer");
		huntPanel = GetNode<Panel>("TabContainer/HuntContainer/HuntPanel");
		capturedPanel = GetNode<Panel>("TabContainer/CapturedContainer/CapturedPanel");
	}
	
	// Returns a list of ActiveHunt for each hunt in the scene
	public List<ActiveHunt> GetHunts()
	{
		List<ActiveHunt> list = new List<ActiveHunt>(hunts);
		return list;
	}
	
	public void AddHunt(HuntData hunt)
	{
		// Check if the hunt already exists, and update if it does
		if (UpdateHunt(hunt)) {
			return; // Return to prevent adding the same hunt twice
		}
		
		// Add a new scene to hold the HuntData
		ActiveHunt newHuntScene = (ActiveHunt)GD.Load<PackedScene>("res://Scenes/ActiveHunt.tscn").Instantiate();
		hunts.Add(newHuntScene);
		
		// Add the hunt to the current scene at the bottom of the list
		huntPanel.AddChild(newHuntScene);
		UpdateHuntPositions();
		
		// Connect signals and initialize the hunt
		newHuntScene.SelectButtonPressed += EmitHuntButtonPressed;
		newHuntScene.InitializeHunt(hunt);
	}
	
	public bool UpdateHunt(HuntData updatedHunt)
	{
		foreach (ActiveHunt hunt in hunts)
		{
			if (hunt.data.huntID == updatedHunt.huntID)
			{
				hunt.data = updatedHunt;
				hunt.UpdateLabel();
				return true;
			}
		}
		
		return false;
	}
	
	public void HuntCompleted(HuntData completedHunt)
	{
		// Remove ActiveHunt from hunts list
		// Remove the ActiveHunt from the scene tree
		// Make a new Captured and add to the scene tree 0
		// Add the new Captured to finishedHunts list
	}
	
	public void RemoveHunt(HuntData deletedHunt)
	{
		foreach (ActiveHunt hunt in hunts)
		{
			if (hunt.data.huntID == deletedHunt.huntID)
			{
				huntPanel.RemoveChild(hunt); // Remove the child from the scene tree
				hunts.Remove(hunt); // Remove the hunt from the list of hunts
				UpdateHuntPositions(); // Update the visible hunts to reflect the removed hunt
				hunt.Cleanup(); // Delete the removed hunt from memory
				return;
			}
		}
	}
	
	public HuntData GetHunt(int id)
	{
		foreach (ActiveHunt hunt in hunts)
		{
			if (hunt.data.huntID == id)
			{
				return hunt.data;
			}
		}
		
		return null;
	}
	
	private void UpdateHuntPositions()
	{
		int x = 7, y = 10;
		int increment = 84 + 10; // y dimension of an ActiveHunt + buffer space
		foreach (ActiveHunt hunt in hunts)
		{
			hunt.Position = new Vector2(x, y);
			y += increment;
			
			if (y > tabContainer.Size.Y) {
				huntPanel.CustomMinimumSize = new Vector2(tabContainer.Size.X, y);
			}
		}
	}
	
	private void SetMainPanel(bool button_pressed)
	{
		if (button_pressed == true) {
			tabContainer.CurrentTab = 0;
		}
	}
	
	private void SetCapturedPanel(bool button_pressed)
	{
		if (button_pressed == true) {
			tabContainer.CurrentTab = 1;
		}
	}
	
	public void EmitHuntButtonPressed(int id)
	{
		EmitSignal("HuntButtonPressed", id);
	}
	
	// Destroy this UI element
	public void Cleanup()
	{
		QueueFree();
	}
	
	private void OpenNewHuntScreen()
	{
		EmitSignal("NewHuntButtonPressed");
	}
}
