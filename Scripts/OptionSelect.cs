using Godot;
using System;
using System.Collections.Generic;

public partial class OptionSelect : Control
{
	[Signal]
	public delegate void CloseMenuEventHandler(string selected);
	
	public List<string> selectedValues;
	bool multiselect = false;
	List<string> allValues;
	ItemList list;
	LineEdit searchBar;
	Label numSelectedLabel;
	TextureButton confirmButton;
	bool screenVisible = false;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		list = GetNode<ItemList>("ListContainer/List");
		searchBar = GetNode<LineEdit>("Search");
		numSelectedLabel = GetNode<Label>("NumSelectedLabel");
		confirmButton = GetNode<TextureButton>("ConfirmButton");
		selectedValues = new List<string>();
	}
	
	public override void _Notification(int what)
	{
		if (what == NotificationWMGoBackRequest && screenVisible)
		{
			BackButtonPressed();
		}
	}
	
	public void CreateList(List<string> items, bool multi)
	{
		allValues = items;
		multiselect = multi;
		if (multiselect)
		{
			list.SelectMode = (ItemList.SelectModeEnum)2; // Allows multi select by toggling items
		}
		else
		{
			list.SelectMode = (ItemList.SelectModeEnum)0; // Single select mode
		}
		foreach (string item in allValues)
		{
			list.AddItem(item);
		}
		UpdateSelectedLabel();
		screenVisible = true;
	}
	
	// Only called in single select mode
	private void ItemSelected(int index)
	{
		selectedValues.Clear();
		selectedValues.Add(list.GetItemText((int)index));
		UpdateSelectedLabel();
		confirmButton.Disabled = false;
	}
	
	private void ItemActivated(int index)
	{
		if (!multiselect)
		{
			ConfirmButtonPressed();
		}
	}
	
	private void MultiSelected(int index, bool selected)
	{
		if (selected)
		{
			if (selectedValues.Count == 10) // Max of 10 pokemon can be selected
			{
				list.Deselect(index);
			}
			else
			{
				selectedValues.Add(list.GetItemText((int)index));
			}
		}
		else
		{
			selectedValues.Remove(list.GetItemText((int)index));
		}
		UpdateSelectedLabel();
		
		if (selectedValues.Count == 0)
		{
			confirmButton.Disabled = true;
		}
		else
		{
			confirmButton.Disabled = false;
		}
	}
	
	private void SearchUpdated(string newText)
	{
		list.Clear(); // Clear every item from the list
		// Re-add the items that match the search term
		foreach (string item in allValues)
		{
			// Only add the item if the current search text is in the string
			if (item.ToLower().Contains(newText.ToLower()))
			{
				list.AddItem(item);
				if (selectedValues.Contains(item)) // Make sure selected items remain selected after searching
				{
					list.Select(list.ItemCount - 1, false); // Reselect the previously selected pokemon, false indicates multi select
				}
			}
		}
	}
	
	private void UpdateSelectedLabel()
	{
		int maxSelect = multiselect ? 10 : 1;
		numSelectedLabel.Text = $"{selectedValues.Count}/{maxSelect} selected";
	}
	
	private void BackButtonPressed()
	{
		Back(""); // Return to the previous screen with no new item
	}
	
	private void ConfirmButtonPressed()
	{
		if (selectedValues.Count > 1)
		{
			Back("Various"); // Return to the previous screen indicating that multiple pokemon have been selected
		}
		else
		{
			Back(selectedValues[0]); // Return to the previous screen with the newly selected item
		}
	}
	
	private void Back(string itemName)
	{
		screenVisible = false;
		EmitSignal("CloseMenu", itemName);
	}
	
	// Destroy this UI element
	public void Cleanup()
	{
		QueueFree();
	}
}
