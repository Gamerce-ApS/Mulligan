using UnityEngine;

public class Example : MonoBehaviour
{
	public GameObject IOS_GUI;
	public GameObject Android_GUI;
    void Start()
    {
        #if UNITY_IPHONE
			Instantiate(IOS_GUI).transform.SetParent(gameObject.transform);
			return;
		#endif

		#if UNITY_ANDROID
			Instantiate(Android_GUI).transform.SetParent(gameObject.transform);
			return;
		#endif

		throw new System.Exception("Please switch your build setting to either iOS or Android to test the example!");
    }
}
