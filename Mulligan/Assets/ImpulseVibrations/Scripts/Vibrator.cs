using UnityEngine;

namespace ImpulseVibrations
{
	public class Vibrator
	{
		/// <summary>
		/// Checks the device have a haptic engine
		/// Added in Android API level 11
		/// iOS 10+
		/// </summary>
		public static bool IsHapticEngineSupported
		{
			get
			{
	#if UNITY_IOS
				return iOSVibrator.IsHapticEngineSupported;
	#endif
	#if UNITY_ANDROID
				return AndroidVibrator.IsHapticEngineSupported;
	#endif
				return false;
			}
		}

		/// <summary>
		/// Returns Android SDL version level of the device.
		/// </summary>
		public static int GetAndroidSDKLevel()
		{
	#if UNITY_ANDROID
			return AndroidVibrator.GetAndroidSDKLevel();
	#else
			return -1;
	#endif
		}

		/// <summary>
		/// Shorthand to call the default Vibrate function provided by Unity Engine if you're already using this class.
		/// Also, Lazy hack to add `android.permission.VIBRATE` permission into the `AndroidManifest.xml`.
		/// </summary>
		public static void UnityVibrate()
		{
			Handheld.Vibrate();
		}

		/// <summary>
		/// This uses the `android.os.Vibrator` to trigger `Vibrate` function.
		/// </summary>
		public static void AndroidVibrate(long milliseconds, int amplitude = -1)
		{
	#if UNITY_ANDROID
			AndroidVibrator.Vibrate(milliseconds, amplitude);
	#endif
		}

		/// <summary>
		/// This uses `performHapticFeedback` function from the View class to trigger haptic feedbacks.
		/// </summary>
		public static bool AndroidVibrate(HapticFeedbackConstants feedbackConstant, HapticFeedbackConstants flag = HapticFeedbackConstants.FLAG_IGNORE_GLOBAL_SETTING)
		{
	#if UNITY_ANDROID
			return AndroidVibrator.PerformHapticFeedback((int) feedbackConstant, (int) flag);
	#else
			return false;
	#endif
		}

		/// <summary>
		/// This uses iOS's `UISelectionFeedbackGenerator` class for haptics.
		/// </summary>
		public static void iOSVibrate(SelectionTypeFeedback selection = SelectionTypeFeedback.SELECTION)
		{
	#if UNITY_IOS
			iOSVibrator.SelectionFeedback();
	#endif
		}

		/// <summary>
		/// This uses iOS's `UIImpactFeedbackGenerator` class for haptics.
		/// </summary>
		public static void iOSVibrate(ImpactTypeFeedback impact, float intensity = -1)
		{
	#if UNITY_IOS
			iOSVibrator.ImpactFeedback((int) impact, intensity);
	#endif
		}

		/// <summary>
		/// This uses iOS's `UINotificationFeedbackGenerator` class for haptics.
		/// </summary>

		public static void iOSVibrate(NotificationTypeFeedback notification)
		{
	#if UNITY_IOS
			iOSVibrator.NotificationFeedback((int) notification);
	#endif	
		}
	}
}
