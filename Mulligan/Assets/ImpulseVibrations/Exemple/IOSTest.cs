using UnityEngine;
using ImpulseVibrations;

public class IOSTest : MonoBehaviour
{
   public void SELECTION()
   {
	   Vibrator.iOSVibrate();
   }
   public void IMPACT_LIGHT()
   {
	   Vibrator.iOSVibrate(ImpactTypeFeedback.IMPACT_LIGHT);
   }
   public void IMPACT_MEDIUM()
   {
	   Vibrator.iOSVibrate(ImpactTypeFeedback.IMPACT_MEDIUM);
   }
   public void IMPACT_HEAVY()
   {
	   Vibrator.iOSVibrate(ImpactTypeFeedback.IMPACT_HEAVY);
   }
   public void IMPACT_RIGID()
   {
	   Vibrator.iOSVibrate(ImpactTypeFeedback.IMPACT_RIGID);
   }
   public void IMPACT_SOFT()
   {
	   Vibrator.iOSVibrate(ImpactTypeFeedback.IMPACT_RIGID);
   }
   public void NOTIFICATION_SUCCESS()
   {
	   Vibrator.iOSVibrate(NotificationTypeFeedback.NOTIFICATION_SUCCESS);
   }
   public void NOTIFICATION_WARNING()
   {
	   Vibrator.iOSVibrate(NotificationTypeFeedback.NOTIFICATION_WARNING);
   }
   public void NOTIFICATION_ERROR()
   {
	   Vibrator.iOSVibrate(NotificationTypeFeedback.NOTIFICATION_ERROR);
   }
}
