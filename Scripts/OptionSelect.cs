using Godot;
using System;
using System.Collections.Generic;

public partial class OptionSelect : Control
{
	[Signal]
	public delegate void CloseMenuEventHandler(string selected);
	
	string selectedValue;
	List<string> allValues;
	ItemList list;
	LineEdit searchBar;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		list = GetNode<ItemList>("ListContainer/List");
		searchBar = GetNode<LineEdit>("Search");
	}
	
	public void CreateList(int listType, List<string> items)
	{
		allValues = items;
		foreach (string item in allValues)
		{
			list.AddItem(item);
		}
	}
	
	private void ItemSelected(long index)
	{
		selectedValue = list.GetItemText((int)index);
	}
	
	private void SearchUpdated(string newText)
	{
		list.Clear(); // Clear every item from the list
		// Re-add the items that match the search term
		foreach (string item in allValues)
		{
			// Only add the item if the current search text is in the string
			if (item.ToLower().Contains(newText.ToLower())) { list.AddItem(item); }
		}
	}
	
	private void BackButtonPressed()
	{
		Back(""); // Return to the previous screen with no new item
	}
	
	private void ConfirmButtonPressed()
	{
		Back(selectedValue); // Return to the previous screen with the newly selected item
	}
	
	private void Back(string itemName)
	{
		EmitSignal("CloseMenu", itemName);
	}
	
	// Destroy this UI element
	public void Cleanup()
	{
		QueueFree();
	}
}
