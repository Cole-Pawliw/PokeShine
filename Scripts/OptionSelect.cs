using Godot;
using System;

public partial class OptionSelect : Control
{
	[Signal]
	public delegate void BackButtonPressedEventHandler();
	
	ItemList list;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		list = GetNode<ItemList>("ListContainer/List");
	}
	
	public void CreateList(int listType, string[] items)
	{
		foreach (string item in items)
		{
			list.AddItem(item);
		}
	}
	
	private void ItemSelected(long index)
	{
		
	}
	
	private void Back()
	{
		EmitSignal("BackButtonPressed");
	}
}
