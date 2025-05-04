using Godot;
using System;

public partial class ItemScrollList : ItemList
{
	bool mouseDown = false;
	Vector2 initialPos;
	int i = 0;
	int selectedIndex = -1;
	
	[Export]
	public int ScrollDeadzone = 0;
	
	[Signal]
	public delegate void ItemPressedEventHandler(int index);
	[Signal]
	public delegate void MultiPressedEventHandler(int index, bool selected);
	
	private void CheckInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton && mouseDown && !@event.IsPressed())
		{
			Vector2 finalPos = GetViewport().GetMousePosition();
			if ((finalPos - initialPos).Length() <= ScrollDeadzone)
			{
				bool single = true;
				bool selected;
				if (IsSelected(i))
				{
					Deselect(i);
					selected = false;
				}
				else
				{
					if (SelectMode != 0)
					{
						single = false;
					}
					Select(i, single);
					selectedIndex = i;
					selected = true;
				}
				
				if (SelectMode == 0)
				{
					EmitSignal("ItemPressed", i);
				}
				else
				{
					EmitSignal("MultiPressed", i, selected);
				}
			}
			
			mouseDown = false;
			i = 0;
		}
	}
	
	private void SingleDown(int index)
	{
		mouseDown = true;
		initialPos = GetViewport().GetMousePosition();
		i = index;
		
		if (selectedIndex == -1)
		{
			Deselect(index);
		}
		else
		{
			Select(selectedIndex, true);
		}
	}
	
	private void MultiDown(int index, bool selected)
	{
		mouseDown = true;
		initialPos = GetViewport().GetMousePosition();
		i = index;
		
		if (selected)
		{
			Deselect(index);
		}
		else
		{
			Select(index, false);
		}
	}
}
