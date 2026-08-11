namespace ImpulseVibrations
{
	/// <summary>
	/// A concrete UIFeedbackGenerator subclass that creates haptics to simulate physical impacts.
	/// iOS 10+
	/// https://developer.apple.com/documentation/uikit/uiimpactfeedbackgenerator
	/// </summary>
	public enum ImpactTypeFeedback
	{
		/// <summary>
		/// The mass of the objects in the collision simulated by a UIImpactFeedbackGenerator object. A collision between small, light user interface elements.
		/// iOS 10+
		/// https://developer.apple.com/documentation/uikit/uiimpactfeedbackgenerator/feedbackstyle
		/// </summary>
		IMPACT_LIGHT = 0,
		/// <summary>
		/// The mass of the objects in the collision simulated by a UIImpactFeedbackGenerator object. A collision between moderately sized user interface elements.
		/// iOS 10+
		/// https://developer.apple.com/documentation/uikit/uiimpactfeedbackgenerator/feedbackstyle
		/// </summary>
		IMPACT_MEDIUM = 1,
		/// <summary>
		/// The mass of the objects in the collision simulated by a UIImpactFeedbackGenerator object. A collision between large, heavy user interface elements.
		/// iOS 10+
		/// https://developer.apple.com/documentation/uikit/uiimpactfeedbackgenerator/feedbackstyle
		/// </summary>
		IMPACT_HEAVY = 2,
		/// <summary>
		/// The mass of the objects in the collision simulated by a UIImpactFeedbackGenerator object.
		/// iOS 13+
		/// https://developer.apple.com/documentation/uikit/uiimpactfeedbackgenerator/feedbackstyle
		/// </summary>
		IMPACT_RIGID = 4,
		/// <summary>
		/// The mass of the objects in the collision simulated by a UIImpactFeedbackGenerator object.
		/// iOS 13+
		/// https://developer.apple.com/documentation/uikit/uiimpactfeedbackgenerator/feedbackstyle
		/// </summary>
		IMPACT_SOFT = 3
	}
}