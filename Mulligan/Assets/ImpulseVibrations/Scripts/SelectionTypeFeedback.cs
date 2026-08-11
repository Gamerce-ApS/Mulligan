namespace ImpulseVibrations
{
	/// <summary>
	/// A concrete UIFeedbackGenerator subclass that creates haptics to indicate a change in selection.
	/// iOS 10+
	/// https://developer.apple.com/documentation/uikit/uiselectionfeedbackgenerator
	/// </summary>
	public enum SelectionTypeFeedback
	{
		/// <summary>
		/// Use selection feedback to communicate movement through a series of discrete values.
		/// iOS 10+
		/// https://developer.apple.com/documentation/uikit/uiselectionfeedbackgenerator
		/// </summary>
		SELECTION = 1
	}
}