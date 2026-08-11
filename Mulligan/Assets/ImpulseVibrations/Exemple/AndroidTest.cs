using System;
using UnityEngine;
using UnityEngine.UI;
using ImpulseVibrations;

public class AndroidTest : MonoBehaviour
{
	public Slider millisecondsSlider;
	public Slider amplitudeSlider;
    public void Vibrate()
	{
		Vibrator.AndroidVibrate(Convert.ToInt64(millisecondsSlider.value), Convert.ToInt32( amplitudeSlider.value));
	}
	public void CLOCK_TICK()
	{
		Vibrator.AndroidVibrate(HapticFeedbackConstants.CLOCK_TICK);
	}
	public void CONFIRM()
	{
		Vibrator.AndroidVibrate(HapticFeedbackConstants.CONFIRM);
	}
	public void CONTEXT_CLICK()
	{
		Vibrator.AndroidVibrate(HapticFeedbackConstants.CONTEXT_CLICK);
	}
	public void GESTURE_END()
	{
		Vibrator.AndroidVibrate(HapticFeedbackConstants.GESTURE_END);
	}
	public void GESTURE_START()
	{
		Vibrator.AndroidVibrate(HapticFeedbackConstants.GESTURE_START);
	}
	public void KEYBOARD_PRESS()
	{
		Vibrator.AndroidVibrate(HapticFeedbackConstants.KEYBOARD_PRESS);
	}
	public void KEYBOARD_RELEASE()
	{
		Vibrator.AndroidVibrate(HapticFeedbackConstants.KEYBOARD_RELEASE);
	}
	public void KEYBOARD_TAP()
	{
		Vibrator.AndroidVibrate(HapticFeedbackConstants.KEYBOARD_TAP);
	}
	public void LONG_PRESS()
	{
		Vibrator.AndroidVibrate(HapticFeedbackConstants.LONG_PRESS);
	}
	public void REJECT()
	{
		Vibrator.AndroidVibrate(HapticFeedbackConstants.REJECT);
	}
	public void TEXT_HANDLE_MOVE()
	{
		Vibrator.AndroidVibrate(HapticFeedbackConstants.TEXT_HANDLE_MOVE);
	}
	public void VIRTUAL_KEY()
	{
		Vibrator.AndroidVibrate(HapticFeedbackConstants.VIRTUAL_KEY);
	}
	public void VIRTUAL_KEY_RELEASE()
	{
		Vibrator.AndroidVibrate(HapticFeedbackConstants.VIRTUAL_KEY_RELEASE);
	}
}
