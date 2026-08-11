namespace ImpulseVibrations
{
	/// <summary>
	/// Constants to be used to perform haptic feedback effects via View#performHapticFeedback(int)
	/// Added in API level 3
	/// https://developer.android.com/reference/android/view/HapticFeedbackConstants
	/// </summary>
	public enum HapticFeedbackConstants
	{
		/// <summary>
		/// The user has pressed either an hour or minute tick of a Clock.
		/// Added in API level 21
		/// https://developer.android.com/reference/android/view/HapticFeedbackConstants
		/// </summary>
		CLOCK_TICK = 4,
		/// <summary>
		/// A haptic effect to signal the confirmation or successful completion of a user interaction.
		/// Added in API level 30
		/// https://developer.android.com/reference/android/view/HapticFeedbackConstants
		/// </summary>
		CONFIRM = 16,
		/// <summary>
		/// The user has performed a context click on an object.
		/// Added in API level 23
		/// https://developer.android.com/reference/android/view/HapticFeedbackConstants
		/// </summary>
		CONTEXT_CLICK = 6,
		/// <summary>
		/// Flag for View#performHapticFeedback(int, int): Ignore the global setting for whether to perform haptic feedback, do it always.
		/// Added in API level 3
		/// https://developer.android.com/reference/android/view/HapticFeedbackConstants
		/// </summary>
		FLAG_IGNORE_GLOBAL_SETTING = 2,
		/// <summary>
		/// Flag for View#performHapticFeedback(int, int): Ignore the setting in the view for whether to perform haptic feedback, do it always.
		/// Added in API level 3
		/// https://developer.android.com/reference/android/view/HapticFeedbackConstants
		/// </summary>
		FLAG_IGNORE_VIEW_SETTING = 1,
		/// <summary>
		/// The user has finished a gesture (e.g. on the soft keyboard).
		/// Added in API level 30
		/// https://developer.android.com/reference/android/view/HapticFeedbackConstants
		/// </summary>
		GESTURE_END = 13,
		/// <summary>
		/// The user has started a gesture (e.g. on the soft keyboard).
		/// Added in API level 30
		/// https://developer.android.com/reference/android/view/HapticFeedbackConstants
		/// </summary>
		GESTURE_START = 12,
		/// <summary>
		/// The user has pressed a virtual or software keyboard key.
		/// Added in API level 27
		/// https://developer.android.com/reference/android/view/HapticFeedbackConstants
		/// </summary>
		KEYBOARD_PRESS = 3,
		/// <summary>
		/// The user has released a virtual keyboard key.
		/// Added in API level 27
		/// https://developer.android.com/reference/android/view/HapticFeedbackConstants
		/// </summary>
		KEYBOARD_RELEASE = 7,
		/// <summary>
		/// The user has pressed a soft keyboard key.
		/// Added in API level 8
		/// https://developer.android.com/reference/android/view/HapticFeedbackConstants
		/// </summary>
		KEYBOARD_TAP = 3,
		/// <summary>
		/// The user has performed a long press on an object that is resulting in an action being performed.
		/// Added in API level 3
		/// https://developer.android.com/reference/android/view/HapticFeedbackConstants
		/// </summary>
		LONG_PRESS = 0,
		/// <summary>
		/// A haptic effect to signal the rejection or failure of a user interaction.
		/// Added in API level 30
		/// https://developer.android.com/reference/android/view/HapticFeedbackConstants
		/// </summary>
		REJECT = 17,
		/// <summary>
		/// The user has performed a selection/insertion handle move on text field.
		/// Added in API level 27
		/// https://developer.android.com/reference/android/view/HapticFeedbackConstants
		/// </summary>
		TEXT_HANDLE_MOVE = 9,
		/// <summary>
		/// The user has pressed on a virtual on-screen key.
		/// Added in API level 5
		/// https://developer.android.com/reference/android/view/HapticFeedbackConstants
		/// </summary>
		VIRTUAL_KEY = 1,
		/// <summary>
		/// The user has released a virtual key.
		/// Added in API level 27
		/// https://developer.android.com/reference/android/view/HapticFeedbackConstants
		/// </summary>
		VIRTUAL_KEY_RELEASE = 8
	}
}