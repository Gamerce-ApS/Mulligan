using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_IOS
using Unity.Notifications.iOS;
#elif UNITY_ANDROID
using Unity.Notifications.Android;
#endif

[Serializable]
public class NotificationData
{
    public string Title;
    public string Message;
    public float HoursAfterLeavingGame;
}

public class LocalNotificationManager : Singleton<LocalNotificationManager>
{
    private const string AndroidChannelId = "mulligan_reminders";

    public List<NotificationData> Notifications = new List<NotificationData>();

    [Header("Testing")]
    public string TestTitle = "Mulligan";
    public string TestMessage = "This is a test notification.";
    public float TestDelaySeconds = 10f;
    public bool ShowTestNotificationInForeground = true;

    private bool isInitialized = false;
    private bool skipNextPauseSchedule = false;

    protected override void Awake()
    {
        base.Awake();

        if (Instance == this)
            DontDestroyOnLoad(gameObject);
    }

    public void Init()
    {
        if (isInitialized)
            return;

        isInitialized = true;
        InitializePlatform();
        RequestPermission();
        CancelNotifications();
    }

    private void OnApplicationPause(bool pause)
    {
        if (!isInitialized)
            return;

        if (pause)
        {
            if (skipNextPauseSchedule)
            {
                skipNextPauseSchedule = false;
                return;
            }

            ScheduleNotifications();
        }
        else
        {
            CancelNotifications();
        }
    }

    public void ScheduleNotifications()
    {
        skipNextPauseSchedule = false;
        CancelNotifications();

        foreach (var notification in Notifications)
        {
            if (notification == null || notification.HoursAfterLeavingGame <= 0)
                continue;

            ScheduleNotification(notification.Title, notification.Message, TimeSpan.FromHours(notification.HoursAfterLeavingGame));
        }
    }

    public void CancelNotifications()
    {
#if UNITY_IOS
        iOSNotificationCenter.RemoveAllScheduledNotifications();
        iOSNotificationCenter.RemoveAllDeliveredNotifications();
        iOSNotificationCenter.ApplicationBadge = 0;
#elif UNITY_ANDROID
        AndroidNotificationCenter.CancelAllNotifications();
#endif
    }

    public void ScheduleTestNotification()
    {
        skipNextPauseSchedule = true;
        CancelNotifications();
        ScheduleNotification(TestTitle, TestMessage, TimeSpan.FromSeconds(TestDelaySeconds), ShowTestNotificationInForeground);
        Debug.Log("Scheduled local test notification in " + TestDelaySeconds + " seconds.");
    }

    private void InitializePlatform()
    {
#if UNITY_ANDROID
        var channel = new AndroidNotificationChannel()
        {
            Id = AndroidChannelId,
            Name = "Mulligan Reminders",
            Importance = Importance.Default,
            Description = "Reminders to return to Mulligan"
        };

        AndroidNotificationCenter.RegisterNotificationChannel(channel);
#endif
    }

    private void RequestPermission()
    {
#if UNITY_IOS
        StartCoroutine(RequestIOSPermission());
#elif UNITY_ANDROID
        StartCoroutine(RequestAndroidPermission());
#endif
    }

    private void ScheduleNotification(string title, string message, TimeSpan delay, bool showInForeground = false)
    {
#if UNITY_IOS
        var trigger = new iOSNotificationTimeIntervalTrigger()
        {
            TimeInterval = delay,
            Repeats = false
        };

        var notification = new iOSNotification()
        {
            Identifier = "mulligan_reminder_" + delay.TotalSeconds,
            Title = title,
            Body = message,
            ShowInForeground = showInForeground,
            ForegroundPresentationOption = showInForeground ? PresentationOption.Alert | PresentationOption.Sound : PresentationOption.None,
            Trigger = trigger
        };

        iOSNotificationCenter.ScheduleNotification(notification);
#elif UNITY_ANDROID
        var notification = new AndroidNotification()
        {
            Title = title,
            Text = message,
            FireTime = DateTime.Now.Add(delay)
        };

        AndroidNotificationCenter.SendNotification(notification, AndroidChannelId);
#endif
    }

#if UNITY_IOS
    private IEnumerator RequestIOSPermission()
    {
        using (var request = new AuthorizationRequest(AuthorizationOption.Alert | AuthorizationOption.Sound, false))
        {
            while (!request.IsFinished)
                yield return null;

            if (!request.Granted)
                Debug.Log("Local notification permission was not granted: " + request.Error);
        }
    }
#endif

#if UNITY_ANDROID
    private IEnumerator RequestAndroidPermission()
    {
        if (AndroidNotificationCenter.UserPermissionToPost == PermissionStatus.Allowed)
            yield break;

        var request = new PermissionRequest();
        while (request.Status == PermissionStatus.RequestPending)
            yield return null;

        if (request.Status != PermissionStatus.Allowed)
            Debug.Log("Local notification permission was not granted: " + request.Status);
    }
#endif
}
