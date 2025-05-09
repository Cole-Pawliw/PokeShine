using Godot;
using System;

public partial class DateInputField : Control
{
	NumberInputField year, month, day;
	public string date = "1900-1-1";
	
	int[] monthLengths = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
	
	public override void _Ready()
	{
		year = GetNode<NumberInputField>("Year");
		month = GetNode<NumberInputField>("Month");
		day = GetNode<NumberInputField>("Day");
	}
	
	private void MonthUpdated()
	{
		// Behold the insane check for a leap year
		if (month.Value == 2 && year.Value % 4 == 0 && (year.Value % 100 != 0 || year.Value % 400 == 0))
		{
			day.MaxValue = 29;
		}
		else
		{
			day.MaxValue = monthLengths[month.Value - 1]; // Assign based on the monthLengths array
		}
		
		UpdateDate();
	}
	
	private void UpdateDate()
	{
		date = $"{year.Value}-{month.Value}-{day.Value}";
	}
	
	public void UpdateDate(string newDate)
	{
		string[] nums = newDate.Split('-');
		// The following three lines cause assignments which will indirectly call UpdateDate()
		year.Text = nums[0];
		month.Text = nums[1];
		day.Text = nums[2];
	}
}
