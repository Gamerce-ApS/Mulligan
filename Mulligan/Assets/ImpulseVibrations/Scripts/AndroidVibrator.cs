#if UNITY_ANDROID
using UnityEngine;

namespace ImpulseVibrations
{
	internal class AndroidVibrator
	{
		private static AndroidJavaObject Vibrator;
		private static AndroidJavaObject RootView;
		private static AndroidJavaClass VibrationEffect;

		static AndroidVibrator()
		{
			try {
				AndroidJavaClass androidUnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
				AndroidJavaObject androidCurrentActivity = androidUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
				
				Vibrator = androidCurrentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
				VibrationEffect = new AndroidJavaClass("android.os.VibrationEffect");

				RootView = androidCurrentActivity.Get<AndroidJavaObject>("mUnityPlayer");
			} catch {}
		}

		public static bool IsHapticEngineSupported
		{
			get
			{
				if (Vibrator != null)
				{
					return Vibrator.Call<bool>("hasVibrator");
				}

				return false;
			}
		}

		public static int GetAndroidSDKLevel()
		{
			try {
				return new AndroidJavaClass("android.os.Build$VERSION").GetStatic<int>("SDK_INT");
			} catch {}
			
			return -1;
		}

		public static void Vibrate(long milliseconds, int amplitude = -1)
		{
			if (Vibrator != null && GetAndroidSDKLevel() >= 26)
			{
				int validAmlitude = Mathf.Min(Mathf.Max(-1, amplitude), 255);
				AndroidJavaObject vibrationEffect = VibrationEffect.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, validAmlitude);
				Vibrator.Call("vibrate", vibrationEffect);
			}
		}

		public static bool PerformHapticFeedback(int feedbackConstant, int flag)
		{
			if (RootView != null)
			{
				return RootView.Call<bool>("performHapticFeedback", feedbackConstant, flag);
			}

			return false;
		}
	}
}
#endif
