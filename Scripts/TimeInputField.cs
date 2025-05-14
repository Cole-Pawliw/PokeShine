using Godot;
using System;

public partial class TimeInputField : Control
{
	NumberInputField hour, minute, second;
	public int totalTime = 0;
	
	public override void _Ready()
	{
		hour = GetNode<NumberInputField>("Hour");
		minute = GetNode<NumberInputField>("Minute");
		second = GetNode<NumberInputField>("Second");
		
		hour.ValueChanged += UpdateTime;
		minute.ValueChanged += UpdateTime;
		second.ValueChanged += UpdateTime;
	}
	
	private void UpdateTime()
	{
		totalTime = hour.Value * 3600 + minute.Value * 60 + second.Value;
	}
	
	public void UpdateTime(int newTime)
	{
		// The following lines cause assignments which will indirectly call UpdateTime()
		hour.Text = $"{newTime / 3600}";
		newTime %= 3600;
		minute.Text = $"{newTime / 60}";
		second.Text = $"{newTime % 60}";
	}
}
