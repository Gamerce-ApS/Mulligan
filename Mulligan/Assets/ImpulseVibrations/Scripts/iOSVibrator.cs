#if UNITY_IOS
using System.Runtime.InteropServices;

namespace ImpulseVibrations
{
	internal class iOSVibrator
	{
		[DllImport("__Internal")]
		private static extern bool isHapticEngineSupported();
		[DllImport("__Internal")]
		private static extern void SelectionFeedbackGenerator();
		[DllImport("__Internal")]
		private static extern void ImpactFeedbackGenerator(int style, float intensity);
		[DllImport("__Internal")]
		private static extern void NotificationFeedbackGenerator(int style);

		public static bool IsHapticEngineSupported
		{
			get
			{
				try {
					return isHapticEngineSupported();
				} catch {}

				return false;
			}
		}

		public static void SelectionFeedback()
		{
			try {
				SelectionFeedbackGenerator();
			} catch {}
		}

		public static void ImpactFeedback(int impact, float intensity)
		{
			try {
				ImpactFeedbackGenerator(impact, intensity);
			} catch {}
		}

		public static void NotificationFeedback(int notification)
		{
			try {
				NotificationFeedbackGenerator(notification);
			} catch {}
		}
	}
}
#endif
