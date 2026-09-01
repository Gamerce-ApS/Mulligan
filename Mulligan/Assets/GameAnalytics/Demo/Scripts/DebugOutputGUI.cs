using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GameAnalyticsSDK.Events;

public class DebugOutputGUI : MonoBehaviour
{
	private Vector2 _scrollPos;
	private int _selectedMessage = 0;
	private List<string> messages;

	void Awake ()
	{
		GA_Debug.EnabledLog();
	}

	void OnGUI ()
	{
		messages = GA_Debug.Messages;

		if (messages != null)
		{
			_scrollPos = GUI.BeginScrollView(new Rect(220, 20, Screen.width - 240, Screen.height - 90), _scrollPos, new Rect(0, 0, Screen.width - 260, messages.Count * 30));
			for (int i = 0; i < messages.Count; i++)
			{
				if (i == _selectedMessage)
				{
					GUI.color = Color.green;
				}

				GUI.Label(new Rect(0, i * 30, Screen.width - 260, 30), messages[i]);

				if (i == _selectedMessage)
				{
					GUI.color = Color.white;
				}
			}
			GUI.EndScrollView();

			if (messages.Count > 0)
			{
				GUI.color = Color.green;
				GUI.Label(new Rect(10, Screen.height - 60, Screen.width - 20, 60), messages[_selectedMessage]);
				GUI.color = Color.white;
				
				if (GUI.Button(new Rect(12, Screen.height - 120, 90, 50), "Up"))
				{
					_selectedMessage--;
					if (_selectedMessage < 0)
					{
						_selectedMessage = messages.Count - 1;
					}
				}
				
				if (GUI.Button(new Rect(116, Screen.height - 120, 90, 50), "Down"))
				{
					_selectedMessage++;
					if (_selectedMessage >= messages.Count)
					{
						_selectedMessage = 0;
					}
				}
			}
		}
	}
}
