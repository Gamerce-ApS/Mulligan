using System;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Analytics;
using UnityEngine;
using UnityEngine.UI;

public class TutorialController : Singleton<TutorialController>
{
    public enum TutorialActionsEnum
    {
        NONE,
        SELECT_ORCS,
        CLICK_ATTACK,
        CLICK_ReRollCards,
        CLICK_REROLL,
        SELECT_WARRIORS,
        ARROW_Artifact,
        ARROW_Potion,
        ARROW_Battle,
        ARTIFACT_Triggered,
        ARROW_UnitUpgrade,
        END_TUTORIAL,

    };
    private struct OverlayLayout
    {
        public Vector2 topPos;
        public Vector2 topSize;

        public Vector2 bottomPos;
        public Vector2 bottomSize;

        public Vector2 leftPos;
        public Vector2 leftSize;

        public Vector2 rightPos;
        public Vector2 rightSize;
    }
    [Serializable]
    public class TutorialStep
    {
        [Header("Step Info")]
        public string Id;

        [TextArea(2, 5)]
        public string Dialogue;

        [Header("Dialogue UI")]
        public RectTransform DialogueParent;
        public Vector2 DialoguePosition;

        [Header("Focus Area In Canvas Space")]
        public Vector2 FocusPosition;
        public float Width = 300f;
        public float Height = 200f;



        [Header("Overlay")]
        [Range(0f, 1f)]
        public float OverlayAlpha = 0.75f;

        public float WaitTime = 3f;

        public TutorialActionsEnum myAction;

        public bool freezeTimeDelta = false;
        public float freezeTimeDelay = 0.2f;

        public bool closeAfter = false;
        [Header("Input")]
        public bool blockInput = false;

        public bool showGnome = true;

        [Header("Resolution Scaling")]
        public bool keepBottomOffset = false;
        public bool keepTopOffset = false;

    }
    public TutorialActionsEnum myCurrentAction = TutorialActionsEnum.NONE;
    [Header("References")]
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Overlay Panels")]
    [SerializeField] private Image topOverlay;
    [SerializeField] private Image bottomOverlay;
    [SerializeField] private Image leftOverlay;
    [SerializeField] private Image rightOverlay;
    [SerializeField] public List<Image> ArrowDownList;

    [Header("Steps")]
    [SerializeField] private List<TutorialStep> steps = new List<TutorialStep>();

    [Header("Transition")]
    [SerializeField] private float transitionDuration = 0.35f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);



    [Header("Input Blocking")]
    [SerializeField] private Button fullScreenBlockButton;
    [SerializeField] private GameObject clickToContinueObject;
    [SerializeField] private TMP_Text clickToContinueText;
    [SerializeField] private string clickToContinueString = "Click to continue";
    public int EnemyIndex = 0;
    public int BossIndex = 0;
    public List<EnemyData> myEnemiesList;
    public List<BossData> myBossList;

    public string LastStepPlayed = "";


    private Coroutine transitionRoutine;

    public Action<string> OnStepShown;

    public int currentStepIndex = -1;
    [SerializeField] private bool updateLiveInEditor = true;

    public int CurrentStepIndex => currentStepIndex;
    public TutorialStep CurrentStep =>
        currentStepIndex >= 0 && currentStepIndex < steps.Count ? steps[currentStepIndex] : null;

    private bool waitingForContinueClick = false;
    private int stepRequestId = 0;

    public GameObject GnomeObject;
    public GameObject ShopMerchant;

    [Header("Tutorial Designed Resolution")]
    [SerializeField] private Vector2 designedCanvasSize = new Vector2(2532f, 1170f);
    [SerializeField] private bool scaleFocusFromDesignedResolution = true;

    void Awake()
    {
        HideTutorial();

        if (fullScreenBlockButton != null)
            fullScreenBlockButton.onClick.AddListener(OnFullScreenBlockButtonClicked);
    }
    public bool HasRunTutorial()
    {
        if (PlayerPrefs.GetInt("HasRunTutorial", 0) == 1)
            return true;
        else
            return false;
    }
    public void StartTutorial()
    {
        if (steps.Count == 0)
            return;

        ShowStepImmediate(0);


        AnalyticsService.Instance.RecordEvent("tutorial_started");
        Debug.Log("tutorial_started");

    }

    public void ShowNextStep()
    {
        ArrowDownList.ForEach(c => c.gameObject.SetActive(false));
        int nextIndex = currentStepIndex + 1;

        if (nextIndex >= steps.Count)
        {
            HideTutorial();
            return;
        }

        ShowStep(nextIndex);
    }
    public EnemyData GetCurrentEnemy()
    {
        return myEnemiesList[EnemyIndex];
    }
    public BossData GetCurrentBoss()
    {
        return myBossList[0];
    }
    public void ShowPreviousStep()
    {
        int prevIndex = currentStepIndex - 1;

        if (prevIndex < 0)
            prevIndex = 0;

        ShowStep(prevIndex);
    }

    public void ShowStepById(string stepId)
    {
        for (int i = 0; i < steps.Count; i++)
        {
            if (steps[i].Id == stepId)
            {
                ShowStep(i);
                return;
            }
        }

        Debug.LogWarning($"Tutorial step with ID '{stepId}' was not found.");
    }
    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.I))
        {
            HideTutorial();


            TutorialController.Instance.LastStepPlayed = "Step3_Potion";

        }

        if (!Application.isPlaying)
            return;

        if (!updateLiveInEditor)
            return;

        if (currentStepIndex < 0 || currentStepIndex >= steps.Count)
            return;

        if (transitionRoutine != null)
            return;

        var step = steps[currentStepIndex];

        UpdateOverlay(step);

        if (dialogueText != null)
            dialogueText.text = step.Dialogue;

        if (step.DialogueParent != null)
            step.DialogueParent.anchoredPosition = step.DialoguePosition;

        if (waitingForContinueClick)
            clickToContinueText.alpha = 0.5f + Mathf.PingPong(Time.unscaledTime, 0.5f);
    }
    public void ShowStep(int index)
    {
        if (index < 0 || index >= steps.Count)
            return;

        stepRequestId++;

        HandleExitAction();

        if (currentStepIndex != -1 && steps[currentStepIndex].DialogueParent != null)
            steps[currentStepIndex].DialogueParent.gameObject.SetActive(false);

        currentStepIndex = index;
        TutorialStep step = steps[index];

        if (dialogueText != null)
            dialogueText.text = step.Dialogue;

        if (step.DialogueParent != null)
        {
            step.DialogueParent.gameObject.SetActive(true);
        }

        TransitionToStep(step);
        OnStepShown?.Invoke(step.Id);

        SetupStepInput(step, stepRequestId);

        GnomeObject.SetActive(step.showGnome);
        ShopMerchant.SetActive(!step.showGnome);




        myCurrentAction = step.myAction;

        if (step.freezeTimeDelta)
        {
            int freezeRequestId = stepRequestId;

            UnityHelper.RunAfterDelay(this, step.freezeTimeDelay, () =>
            {
                if (freezeRequestId != stepRequestId)
                    return;

                Time.timeScale = 0;
            }, true);
        }
        else
        {
            Time.timeScale = 1;
        }

        HandleStartAction();

        LastStepPlayed = step.Id;

        AnalyticsService.Instance.RecordEvent("tutorial_step_started_" + index);
        Debug.Log("tutorial_step_started_" + index);
    }

    public void HideTutorial()
    {
        stepRequestId++;
        waitingForContinueClick = false;

        if (currentStepIndex != -1 && steps[currentStepIndex].DialogueParent != null)
            steps[currentStepIndex].DialogueParent.gameObject.SetActive(false);

        currentStepIndex = -1;

        if (dialogueText != null)
            dialogueText.text = string.Empty;

        HideClickToContinue();
        SetBlockInputActive(false);
        SetOverlayActive(false);

        Time.timeScale = 1;
        ArrowDownList.ForEach(c => c.gameObject.SetActive(false));
        ShopMerchant.SetActive(false);
    }

    private void UpdateOverlay(TutorialStep step)
    {
        OverlayLayout layout = CalculateOverlayLayout(step);
        ApplyOverlayLayout(layout, step.OverlayAlpha);
    }
    private OverlayLayout CalculateOverlayLayout(TutorialStep step)
    {
        OverlayLayout layout = new OverlayLayout();

        if (canvasRect == null)
        {
            Debug.LogError("TutorialController: Canvas RectTransform is missing.");
            return layout;
        }

        float canvasWidth = canvasRect.rect.width;
        float canvasHeight = canvasRect.rect.height;

        Vector2 focusPosition = step.FocusPosition;
        float focusWidth = step.Width;
        float focusHeight = step.Height;

        if (scaleFocusFromDesignedResolution && designedCanvasSize.x > 0 && designedCanvasSize.y > 0)
        {
            float scaleX = canvasWidth / designedCanvasSize.x;
            float scaleY = canvasHeight / designedCanvasSize.y;

            // Important: use the same scale as Canvas Scaler "Match Width Or Height"
            // If your Canvas Scaler Match is 0.5, keep this 0.5.
            float canvasScalerMatch = 0.0f;
            float scale = Mathf.Lerp(scaleX, scaleY, canvasScalerMatch);

            float x = step.FocusPosition.x * scale;
            float y;

            if (step.keepBottomOffset)
            {
                float designedBottom = -designedCanvasSize.y * 0.5f;
                float currentBottom = -canvasHeight * 0.5f;

                float designedOffsetFromBottom = step.FocusPosition.y - designedBottom;

                y = currentBottom + designedOffsetFromBottom * scale;
            }
            else if (step.keepTopOffset)
            {
                float designedTop = designedCanvasSize.y * 0.5f;
                float currentTop = canvasHeight * 0.5f;

                float designedOffsetFromTop = designedTop - step.FocusPosition.y;

                y = currentTop - designedOffsetFromTop * scale;
            }
            else
            {
                y = step.FocusPosition.y * scale;
            }

            focusPosition = new Vector2(x, y);

            focusWidth = step.Width * scale;
            focusHeight = step.Height * scale;
        }

        float left = focusPosition.x - focusWidth * 0.5f;
        float right = focusPosition.x + focusWidth * 0.5f;
        float bottom = focusPosition.y - focusHeight * 0.5f;
        float top = focusPosition.y + focusHeight * 0.5f;

        float canvasLeft = -canvasWidth * 0.5f;
        float canvasRight = canvasWidth * 0.5f;
        float canvasBottom = -canvasHeight * 0.5f;
        float canvasTop = canvasHeight * 0.5f;

        left = Mathf.Clamp(left, canvasLeft, canvasRight);
        right = Mathf.Clamp(right, canvasLeft, canvasRight);
        bottom = Mathf.Clamp(bottom, canvasBottom, canvasTop);
        top = Mathf.Clamp(top, canvasBottom, canvasTop);

        layout.topPos = new Vector2(0f, (canvasTop + top) * 0.5f);
        layout.topSize = new Vector2(canvasWidth, canvasTop - top);

        layout.bottomPos = new Vector2(0f, (canvasBottom + bottom) * 0.5f);
        layout.bottomSize = new Vector2(canvasWidth, bottom - canvasBottom);

        layout.leftPos = new Vector2((canvasLeft + left) * 0.5f, focusPosition.y);
        layout.leftSize = new Vector2(left - canvasLeft, focusHeight);

        layout.rightPos = new Vector2((canvasRight + right) * 0.5f, focusPosition.y);
        layout.rightSize = new Vector2(canvasRight - right, focusHeight);

        return layout;
    }

    private void ApplyOverlayLayout(OverlayLayout layout, float alpha)
    {
        SetOverlayActive(true);

        SetImageAlpha(topOverlay, alpha);
        SetImageAlpha(bottomOverlay, alpha);
        SetImageAlpha(leftOverlay, alpha);
        SetImageAlpha(rightOverlay, alpha);

        SetRect(topOverlay.rectTransform, layout.topPos, layout.topSize);
        SetRect(bottomOverlay.rectTransform, layout.bottomPos, layout.bottomSize);
        SetRect(leftOverlay.rectTransform, layout.leftPos, layout.leftSize);
        SetRect(rightOverlay.rectTransform, layout.rightPos, layout.rightSize);
    }

    private void SetRect(RectTransform rect, Vector2 anchoredPos, Vector2 size)
    {
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(Mathf.Max(0f, size.x), Mathf.Max(0f, size.y));
    }

    private void SetOverlayActive(bool active)
    {
        if (topOverlay != null) topOverlay.gameObject.SetActive(active);
        if (bottomOverlay != null) bottomOverlay.gameObject.SetActive(active);
        if (leftOverlay != null) leftOverlay.gameObject.SetActive(active);
        if (rightOverlay != null) rightOverlay.gameObject.SetActive(active);
    }

    private void SetImageAlpha(Image image, float alpha)
    {
        if (image == null)
            return;

        Color c = image.color;
        c.a = alpha;
        image.color = c;
    }
    public void HandleStartAction()
    {
        if (myCurrentAction == TutorialActionsEnum.SELECT_ORCS)
        {
            ArrowDownList.ForEach(c => c.gameObject.SetActive(true));
            Vector3 offset = new Vector3(0, 30, 0);
            ArrowDownList[0].transform.position = HandManager.Instance.CurrentHand[0].CardGO.transform.position + offset;
            ArrowDownList[1].transform.position = HandManager.Instance.CurrentHand[2].CardGO.transform.position + offset;
            ArrowDownList[2].transform.position = HandManager.Instance.CurrentHand[3].CardGO.transform.position + offset;
            ArrowDownList[3].transform.position = HandManager.Instance.CurrentHand[4].CardGO.transform.position + offset;
        }
        if (myCurrentAction == TutorialActionsEnum.CLICK_ATTACK)
        {
            ArrowDownList[0].gameObject.SetActive(true);
            Vector3 offset = new Vector3(0, 25, 0);
            ArrowDownList[0].transform.position = UIManager.Instance.AttackButton.transform.position + offset;
        }
        if (myCurrentAction == TutorialActionsEnum.CLICK_REROLL)
        {
            ArrowDownList[0].gameObject.SetActive(true);
            Vector3 offset = new Vector3(0, 25, 0);
            ArrowDownList[0].transform.position = UIManager.Instance.ReRollButton.transform.position + offset;
        }
        if (myCurrentAction == TutorialActionsEnum.CLICK_ReRollCards)
        {
            ArrowDownList[0].gameObject.SetActive(true);
            ArrowDownList[1].gameObject.SetActive(true);

            Vector3 offset = new Vector3(0, 30, 0);
            ArrowDownList[0].transform.position = HandManager.Instance.CurrentHand[0].CardGO.transform.position + offset;
            ArrowDownList[1].transform.position = HandManager.Instance.CurrentHand[2].CardGO.transform.position + offset;
        }
        if (myCurrentAction == TutorialActionsEnum.SELECT_WARRIORS)
        {
            ArrowDownList.ForEach(c => c.gameObject.SetActive(true));
            Vector3 offset = new Vector3(0, 30, 0);

            List<CardInstance> warriorInHand = HandManager.Instance.CurrentHand.FindAll(c => c.data.cardClass == CardClass.Warrior);

            ArrowDownList[0].transform.position = warriorInHand[0].CardGO.transform.position + offset;
            ArrowDownList[1].transform.position = warriorInHand[1].CardGO.transform.position + offset;
            ArrowDownList[2].transform.position = warriorInHand[2].CardGO.transform.position + offset;
            ArrowDownList[3].transform.position = warriorInHand[3].CardGO.transform.position + offset;
        }
        if (myCurrentAction == TutorialActionsEnum.ARROW_Artifact)
        {
            GameManager.Instance.AddGold(6);

            ArrowDownList[0].gameObject.SetActive(true);
            Vector3 offset = new Vector3(10, 5, 0);

            ArrowDownList[0].transform.position = ShopManager.Instance.ArtifactParent.GetChild(0).transform.position + offset;
            ArrowDownList[0].GetComponent<Animator>().Play("arrow_buy");

        }
        if (myCurrentAction == TutorialActionsEnum.ARROW_Potion)
        {
            GameManager.Instance.AddGold(3);

            if (UIManager.Instance.PotionSlotParent.childCount > 0)
                PotionManager.Instance.SellPotion(UIManager.Instance.PotionSlotParent.GetChild(0).GetComponent<Potion>());

            ArrowDownList[0].gameObject.SetActive(true);
            Vector3 offset = new Vector3(10, 5, 0);

            ArrowDownList[0].transform.position = ShopManager.Instance.PotionParent.GetChild(0).transform.position + offset;
            ArrowDownList[0].GetComponent<Animator>().Play("arrow_buy");

        }
        if (myCurrentAction == TutorialActionsEnum.ARROW_Battle)
        {

            ArrowDownList[2].gameObject.SetActive(true);
            Vector3 offset = new Vector3(0, 30, 0);

            ArrowDownList[2].transform.position = ShopManager.Instance.BattleButton.transform.position + offset;

        }
        if (myCurrentAction == TutorialActionsEnum.ARROW_UnitUpgrade)
        {
            GameManager.Instance.AddGold(6);

            ArrowDownList[0].gameObject.SetActive(true);
            Vector3 offset = new Vector3(10, 5, 0);

            ArrowDownList[0].transform.position = ShopManager.Instance.UnitPackParent.GetChild(0).transform.position + offset;
            ArrowDownList[0].GetComponent<Animator>().Play("arrow_buy");

        }
        if (myCurrentAction == TutorialActionsEnum.END_TUTORIAL)
        {
            PlayerPrefs.SetInt("HasRunTutorial", 1);
            ResetAfterTutorialFinished();
            AnalyticsService.Instance.RecordEvent("tutorial_finished");
            Debug.Log("tutorial_finished");

        }


    }
    public void HandleExitAction()
    {
        if (myCurrentAction == TutorialActionsEnum.SELECT_ORCS)
        {
            ArrowDownList.ForEach(c => c.gameObject.SetActive(false));
        }
        if (myCurrentAction == TutorialActionsEnum.CLICK_ReRollCards)
        {
            ArrowDownList.ForEach(c => c.gameObject.SetActive(false));
        }
        if (myCurrentAction == TutorialActionsEnum.CLICK_ATTACK)
        {
            ArrowDownList.ForEach(c => c.gameObject.SetActive(false));
        }
        if (myCurrentAction == TutorialActionsEnum.CLICK_REROLL)
        {
            ArrowDownList.ForEach(c => c.gameObject.SetActive(false));
        }
        if (myCurrentAction == TutorialActionsEnum.SELECT_WARRIORS)
        {
            ArrowDownList.ForEach(c => c.gameObject.SetActive(false));
        }
        if (myCurrentAction == TutorialActionsEnum.ARROW_Artifact)
        {
            ArrowDownList.ForEach(c => c.gameObject.SetActive(false));
        }
        if (myCurrentAction == TutorialActionsEnum.ARROW_Potion)
        {
            ArrowDownList.ForEach(c => c.gameObject.SetActive(false));
        }
        if (myCurrentAction == TutorialActionsEnum.ARROW_Battle)
        {
            ArrowDownList.ForEach(c => c.gameObject.SetActive(false));
        }
        if (myCurrentAction == TutorialActionsEnum.ARROW_UnitUpgrade)
        {
            ArrowDownList.ForEach(c => c.gameObject.SetActive(false));
        }

    }
    private OverlayLayout GetCurrentOverlayLayout()
    {
        OverlayLayout layout = new OverlayLayout();

        layout.topPos = topOverlay.rectTransform.anchoredPosition;
        layout.topSize = topOverlay.rectTransform.sizeDelta;

        layout.bottomPos = bottomOverlay.rectTransform.anchoredPosition;
        layout.bottomSize = bottomOverlay.rectTransform.sizeDelta;

        layout.leftPos = leftOverlay.rectTransform.anchoredPosition;
        layout.leftSize = leftOverlay.rectTransform.sizeDelta;

        layout.rightPos = rightOverlay.rectTransform.anchoredPosition;
        layout.rightSize = rightOverlay.rectTransform.sizeDelta;

        return layout;
    }

    private float GetCurrentOverlayAlpha()
    {
        return topOverlay != null ? topOverlay.color.a : 0f;
    }
    private void TransitionToStep(TutorialStep step)
    {
        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        transitionRoutine = StartCoroutine(AnimateToStep(step));
    }

    private System.Collections.IEnumerator AnimateToStep(TutorialStep step)
    {
        SetOverlayActive(true);

        OverlayLayout startLayout = GetCurrentOverlayLayout();
        OverlayLayout targetLayout = CalculateOverlayLayout(step);

        float startAlpha = GetCurrentOverlayAlpha();
        float targetAlpha = step.OverlayAlpha;

        RectTransform dialogueRect = step.DialogueParent;
        Vector2 startDialoguePos = dialogueRect != null ? dialogueRect.anchoredPosition : Vector2.zero;
        Vector2 targetDialoguePos = step.DialoguePosition;

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            float eased = transitionCurve.Evaluate(t);

            OverlayLayout current = new OverlayLayout
            {
                topPos = Vector2.Lerp(startLayout.topPos, targetLayout.topPos, eased),
                topSize = Vector2.Lerp(startLayout.topSize, targetLayout.topSize, eased),

                bottomPos = Vector2.Lerp(startLayout.bottomPos, targetLayout.bottomPos, eased),
                bottomSize = Vector2.Lerp(startLayout.bottomSize, targetLayout.bottomSize, eased),

                leftPos = Vector2.Lerp(startLayout.leftPos, targetLayout.leftPos, eased),
                leftSize = Vector2.Lerp(startLayout.leftSize, targetLayout.leftSize, eased),

                rightPos = Vector2.Lerp(startLayout.rightPos, targetLayout.rightPos, eased),
                rightSize = Vector2.Lerp(startLayout.rightSize, targetLayout.rightSize, eased)
            };

            float alpha = Mathf.Lerp(startAlpha, targetAlpha, eased);
            ApplyOverlayLayout(current, alpha);

            if (dialogueRect != null)
                dialogueRect.anchoredPosition = Vector2.Lerp(startDialoguePos, targetDialoguePos, eased);

            yield return null;
        }

        ApplyOverlayLayout(targetLayout, targetAlpha);

        if (dialogueRect != null)
            dialogueRect.anchoredPosition = targetDialoguePos;

        transitionRoutine = null;
    }
    public void ShowStepImmediate(int index)
    {
        if (index < 0 || index >= steps.Count)
            return;

        stepRequestId++;

        currentStepIndex = index;
        TutorialStep step = steps[index];

        if (dialogueText != null)
            dialogueText.text = step.Dialogue;

        if (step.DialogueParent != null)
        {
            step.DialogueParent.gameObject.SetActive(true);
            step.DialogueParent.anchoredPosition = step.DialoguePosition;
        }

        UpdateOverlay(step);
        OnStepShown?.Invoke(step.Id);

        SetupStepInput(step, stepRequestId);

        myCurrentAction = step.myAction;

        if (step.freezeTimeDelta)
        {
            int freezeRequestId = stepRequestId;

            UnityHelper.RunAfterDelay(this, step.freezeTimeDelay, () =>
            {
                if (freezeRequestId != stepRequestId)
                    return;

                Time.timeScale = 0;
            }, true);
        }
        else
        {
            Time.timeScale = 1;
        }

        HandleStartAction();
    }
    private void OnFullScreenBlockButtonClicked()
    {
        if (currentStepIndex < 0 || currentStepIndex >= steps.Count)
            return;

        TutorialStep step = steps[currentStepIndex];

        if (!step.blockInput)
            return;

        if (step.WaitTime > 0)
        {
            if (!waitingForContinueClick)
                return;

            VibrationsManager.TryVibrate(VibrationType.ButtonTap);
            SoundManager.TryPlay(SoundType.ButtonTap);
            waitingForContinueClick = false;
            HideClickToContinue();

            if (step.closeAfter)
                HideTutorial();
            else
                ShowNextStep();

            return;
        }

        // Optional:
        // for blockInput steps without wait time, do nothing
    }
    private void SetBlockInputActive(bool active)
    {
        if (fullScreenBlockButton != null)
            fullScreenBlockButton.gameObject.SetActive(active);
    }

    private void ShowClickToContinue()
    {
        if (clickToContinueObject != null)
            clickToContinueObject.SetActive(true);

        if (clickToContinueText != null)
            clickToContinueText.text = clickToContinueString;
    }

    private void HideClickToContinue()
    {
        if (clickToContinueObject != null)
            clickToContinueObject.SetActive(false);
    }
    private void SetupStepInput(TutorialStep step, int requestId)
    {
        waitingForContinueClick = false;
        HideClickToContinue();
        SetBlockInputActive(step.blockInput);

        if (step.WaitTime <= 0)
            return;

        UnityHelper.RunAfterDelay(this, step.WaitTime, () =>
        {
            if (requestId != stepRequestId)
                return;

            if (currentStepIndex < 0 || currentStepIndex >= steps.Count)
                return;

            if (steps[currentStepIndex] != step)
                return;

            // If input is blocked, wait for player tap
            if (step.blockInput)
            {
                ShowClickToContinue();
                waitingForContinueClick = true;
            }
            else
            {
                // Otherwise keep old auto-next behavior
                if (step.closeAfter)
                    HideTutorial();
                else
                    ShowNextStep();
            }
        }, true);
    }
    public void ResetAfterTutorialFinished()
    {

    }
}
