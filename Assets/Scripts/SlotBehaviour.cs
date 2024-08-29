using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using System;

public class SlotBehaviour : MonoBehaviour
{
    #region VARIABLES_AND_REFERENCES

    [Header("Audio Controller...")]
    [SerializeField]
    private AudioController audioController;

    [Header("SockerManager")]
    [SerializeField]
    private SocketIOManager SocketManager;

    [Header("UI Manager")]
    [SerializeField]
    private UIManager m_UIManager;

    [Header("Game Manager")]
    [SerializeField]
    private GameManager m_GameManager;

    [Header("Key")]
    private KeyStruct m_Key;


    #region Arrays
    [Header("Sprites")]
    [SerializeField]
    private Sprite[] myImages;

    [SerializeField]
    private Sprite[] myBonusImages;

    [Header("Slots Objects")]
    [SerializeField]
    private GameObject[] Slot_Objects;

    [Header("Slots Transforms")]
    [SerializeField]
    private Transform[] Slot_Transform;
    #endregion

    #region Lists
    [Header("Slot Images")]
    [SerializeField]
    private List<SlotImage> m_images;

    [Header("Animated Slot Images")]
    [SerializeField]
    private List<AnimatedSlots> m_animated_slot_images;
    private Dictionary<string, List<Sprite>> m_animated_slot_dictionary = new Dictionary<string, List<Sprite>>();

    [SerializeField]
    private List<AnimatedSlots> m_animated_bonus_images;
    private Dictionary<string, List<Sprite>> m_animated_bonus_dictionary = new Dictionary<string, List<Sprite>>();

    [SerializeField]
    private List<SlotImage> Tempimages;

    [Header("Tweeners")]
    [SerializeField]
    private List<Tweener> alltweens = new List<Tweener>();

    [Header("Image Animations")]
    [SerializeField]
    private List<ImageAnimation> TempList;
    #endregion

    #region Booleans
    private bool IsAutoSpin = false;
    private bool IsFreeSpin = false;
    private bool IsSpinning = false;
    internal bool CheckPopups = false;
    #endregion

    #region Numbers
    private int BetCounter = 0;
    private double currentBalance = 0;
    private double currentTotalBet = 0;
    int Lines = 1;

    [SerializeField]
    int tweenHeight = 0;
    [SerializeField]
    private int numberOfSlots = 4;
    [SerializeField]
    private int IconSizeFactor = 100;
    [SerializeField]
    private int SpaceFactor = 0;
    [SerializeField]
    private int verticalVisibility = 3;
    #endregion

    #region Coroutines
    Coroutine AutoSpinRoutine = null;
    Coroutine tweenroutine = null;
    Coroutine FreeSpinRoutine = null;
    #endregion

    #endregion

    [SerializeField]
    private bool m_Bonus_Found = false;
    [SerializeField]
    private float m_Speed_Control = 0.2f;

    private List<ImageAnimation> m_imageAnimations = new List<ImageAnimation>();

    private void OnEnable()
    {
        InitiateButtons();
    }

    private void Awake()
    {
        m_Key = new KeyStruct();
    }

    private void Start()
    {
        ValidateAnimationDictionary();
        tweenHeight = (myImages.Length * IconSizeFactor) - 280;
    }

    private void InitiateButtons()
    {
        m_GameManager.OnSpinClicked += delegate { StartSlots(); };
        m_GameManager.OnBetButtonClicked += delegate { OnBetOne(); };
        m_GameManager.OnAutoSpinClicked += delegate { AutoSpin(); };
        m_GameManager.OnAutoSpinStopClicked += delegate { StopAutoSpin(); };
    }

    internal void SetInitialUI()
    {
        BetCounter = SocketManager.initialData.Bets.Count - 1;

        m_UIManager.GetText(m_Key.m_text_bet_amount).text = SocketManager.initialData.Bets[BetCounter].ToString();

        //TotalBet_text.text = (SocketManager.initialData.Bets[BetCounter] * Lines).ToString(); //To Be Implemented In Future

        m_UIManager.GetText(m_Key.m_text_win_amount).text = "0.00";

        m_UIManager.GetText(m_Key.m_text_balance_amount).text = SocketManager.playerdata.Balance.ToString("f2");

        currentBalance = SocketManager.playerdata.Balance;
        currentTotalBet = SocketManager.initialData.Bets[BetCounter] * Lines;

        CompareBalance();
    }

    internal void shuffleInitialMatrix()
    {
        for (int i = 0; i < Tempimages.Count; i++)
        {
            for (int j = 0; j < Tempimages[i].slotImages.Count; j++)
            {
                int randomIndex = UnityEngine.Random.Range(0, myImages.Length);
                Tempimages[i].slotImages[j].sprite = myImages[randomIndex];
            }
        }
    }

    private void StartSlots(bool autoSpin = false)
    {
        if (audioController) audioController.PlaySpinButtonAudio();

        if (!autoSpin)
        {
            if (AutoSpinRoutine != null)
            {
                StopCoroutine(AutoSpinRoutine);
                StopCoroutine(tweenroutine);
                tweenroutine = null;
                AutoSpinRoutine = null;
            }

        }

        if (TempList.Count > 0)
        {
            StopGameAnimation();
        }

        tweenroutine = StartCoroutine(TweenRoutine());
    }

    private void AutoSpin()
    {
        if (audioController) audioController.PlaySpinButtonAudio();

        if (!IsAutoSpin)
        {
            IsAutoSpin = true;
            m_UIManager.GetButton(m_Key.m_button_auto_spin_stop).gameObject.SetActive(true);
            m_UIManager.GetButton(m_Key.m_button_auto_spin).gameObject.SetActive(false);

            if (AutoSpinRoutine != null)
            {
                StopCoroutine(AutoSpinRoutine);
                AutoSpinRoutine = null;
            }
            AutoSpinRoutine = StartCoroutine(AutoSpinCoroutine());
        }
    }

    private void StopAutoSpin()
    {
        if (audioController) audioController.PlaySpinButtonAudio();

        if (IsAutoSpin)
        {
            IsAutoSpin = false;
            m_UIManager.GetButton(m_Key.m_button_auto_spin_stop).gameObject.SetActive(false);
            m_UIManager.GetButton(m_Key.m_button_auto_spin).gameObject.SetActive(true);
            StartCoroutine(StopAutoSpinCoroutine());
        }
    }

    private IEnumerator AutoSpinCoroutine()
    {
        while (IsAutoSpin)
        {
            StartSlots(IsAutoSpin);
            yield return tweenroutine;
        }
    }

    private IEnumerator StopAutoSpinCoroutine()
    {
        yield return new WaitUntil(() => !IsSpinning);
        ToggleButtonGrp(true);
        if (AutoSpinRoutine != null || tweenroutine != null)
        {
            StopCoroutine(AutoSpinRoutine);
            StopCoroutine(tweenroutine);
            tweenroutine = null;
            AutoSpinRoutine = null;
            StopCoroutine(StopAutoSpinCoroutine());
        }
    }

    private void CompareBalance()
    {
        if (currentBalance < currentTotalBet)
        {
            m_UIManager.GetButton(m_Key.m_button_auto_spin).interactable = false;
            m_UIManager.GetButton(m_Key.m_button_spin).interactable = false;
        }
        else
        {
            m_UIManager.GetButton(m_Key.m_button_auto_spin).interactable = true;
            m_UIManager.GetButton(m_Key.m_button_spin).interactable = true;
        }
    }

    void OnBetOne()
    {
        if (audioController) audioController.PlayButtonAudio();

        if (BetCounter < SocketManager.initialData.Bets.Count - 1)
        {
            BetCounter++;
        }
        else
        {
            BetCounter = 0;
        }
        m_UIManager.GetText(m_Key.m_text_bet_amount).text = SocketManager.initialData.Bets[BetCounter].ToString();
        //if (TotalBet_text) TotalBet_text.text = (SocketManager.initialData.Bets[BetCounter] * Lines).ToString(); // To Be Implemented

        currentTotalBet = SocketManager.initialData.Bets[BetCounter] * Lines;
        CompareBalance();
    }

    internal void LayoutReset(int number)
    {
        //if (Slot_Elements[number]) Slot_Elements[number].ignoreLayout = true;
        m_UIManager.GetButton(m_Key.m_button_spin).interactable = true;
    }

    private void OnApplicationFocus(bool focus)
    {
        if (focus)
        {
            if (!IsSpinning)
            {
                if (audioController) audioController.StopWLAaudio();
            }
        }
    }

    private IEnumerator TweenRoutine()
    {
        ClearAllImageAnimations();
        m_Bonus_Found = false;
        m_Speed_Control = 0.2f;
        m_UIManager.GetGameObject(m_Key.m_object_normal_win_line).SetActive(true);
        m_UIManager.GetGameObject(m_Key.m_object_animated_win_line).SetActive(false);
        m_UIManager.GetGameObject(m_Key.m_object_animated_win_line).GetComponent<ImageAnimation>().StopAnimation();

        // Check if the player has enough balance to spin and if it's not a free spin
        if (currentBalance < currentTotalBet && !IsFreeSpin)
        {
            CompareBalance();
            StopAutoSpin();
            yield return new WaitForSeconds(1);
            yield break;
        }

        // Play the spin audio if the audio controller is available
        if (audioController)
        {
            audioController.PlayWLAudio("spin");
        }

        // Set spinning state to true and disable buttons during the spin
        IsSpinning = true;
        ToggleButtonGrp(false);

        // Initialize tweening for each slot with a small delay between them
        for (int i = 0; i < numberOfSlots; i++)
        {
            InitializeTweening(Slot_Transform[i]);
            yield return new WaitForSeconds(0.01f);
        }

        // For demo purposes: Custom inputs for balance and bet
        double bet = 10.0; // Custom input: Fixed bet amount for demo
        double balance = currentBalance;

        // Update the balance after placing the bet
        double initAmount = balance;
        balance -= bet;

        // Tween the balance display to reflect the new balance
        DOTween.To(() => initAmount, (val) => initAmount = val, balance, 0.8f).OnUpdate(() =>
        {
            m_UIManager.GetText(m_Key.m_text_balance_amount).text = initAmount.ToString("f2");
        });

        // For demo purposes: Custom result data (simulated results)
        List<int> simulatedResultReel = new List<int>
        {
            //UnityEngine.Random.Range(0, myImages.Length),
            //UnityEngine.Random.Range(0, myImages.Length),
            //UnityEngine.Random.Range(0, myImages.Length),
            1, 1, 1,
            UnityEngine.Random.Range(0, myBonusImages.Length)
        }; // Custom input: Simulated slot result

        AssignResultSpritesWin(simulatedResultReel); //Takes the list as argument and assigns to the slot and bonus slots

        if (m_GameManager.TurboSpin)
        {
            yield return new WaitForSeconds(0f);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        // Stop all tweens running for each slot
        for (int i = 0; i < numberOfSlots-1; i++)
        {
            yield return StopTweening(5, Slot_Transform[i], i);
        }
        if (m_Bonus_Found)
        {
            yield return new WaitForSeconds(1.5f);
            yield return StopTweening(5, Slot_Transform[numberOfSlots - 1], numberOfSlots - 1);
        }
        else
        {
            yield return StopTweening(5, Slot_Transform[numberOfSlots - 1], numberOfSlots - 1);
        }

        yield return new WaitForSeconds(0.3f);

        // Optional: Logic to check payout line (To be implemented with backend data)
        // TODO: Implement backend logic to check payout line and jackpots

        // Kill all active tweens to ensure no lingering animations
        KillAllTweens();

        // For demo purposes: Custom win amount and balance updates
        double simulatedWinAmount = 50.0; // Custom input: Simulated win amount

        m_UIManager.GetText(m_Key.m_text_win_amount).text = simulatedWinAmount.ToString("f2");
        m_UIManager.GetText(m_Key.m_text_balance_amount).text = (balance + simulatedWinAmount).ToString("f2");

        // TODO: Implement backend logic to check for bonus games and win popups
        CheckPopups = false; // Simulated: No popups for demo

        // Wait until all popups are dismissed
        yield return new WaitUntil(() => !CheckPopups);

        // Handle the end of the spin, either by re-enabling the buttons or starting the next auto-spin
        if (!IsAutoSpin)
        {
            //ActivateGamble();
            ToggleButtonGrp(true);
            IsSpinning = false;
        }
        else
        {
            //ActivateGamble();
            yield return new WaitForSeconds(2f);
            IsSpinning = false;
        }

        // Optional: Handle free spins if applicable (To be implemented)
        // TODO: Implement free spin logic based on backend data
    }

    #region RESULT_FUNCTIONALITIES

    private void AssignResultSpritesWin(List<int> m_slot_values)
    {
        for (int j = 0; j < m_slot_values.Count; j++)
        {
            if (j < m_slot_values.Count - 1)
            {
                if (m_slot_values[j] != 0)
                {
                    Tempimages[j].slotImages[0].color = new Color(255, 255, 255, 255);
                    Tempimages[j].slotImages[0].sprite = myImages[m_slot_values[j]];
                }
                else
                {
                    Tempimages[j].slotImages[0].color = new Color(255, 255, 255, 0);
                    Tempimages[j].slotImages[0].sprite = null;
                }
            }
            else
            {
                if (m_slot_values[j] != 0)
                {
                    Tempimages[j].slotImages[0].color = new Color(255, 255, 255, 255);
                    Tempimages[j].slotImages[0].sprite = myBonusImages[m_slot_values[j]];
                }
                else
                {
                    Tempimages[j].slotImages[0].color = new Color(255, 255, 255, 0);
                    Tempimages[j].slotImages[0].sprite = null;
                }
            }
            // Debug.Log("<color=yellow><b>" + j + "</b></color>");
        }

        CheckWin(m_slot_values);
    }

    /// <summary>
    /// This method takes list of integers as argument and checks for the zeros in the list
    /// 0's represents the emptyness of the slot.
    /// It will check for zeros if count > 0 that means empty is available, last index of result_reel represents the bonus slot
    /// if that is not zero then perform something if that is zero then check for other zeros if available then no animations
    /// if not available then player slot animation.
    /// </summary>
    /// <param name="result_reel"></param>
    private void CheckWin(List<int> result_reel)
    {
        int zero_count = result_reel.Count(x => x == 0);
        bool check_zero = zero_count > 0 ? false : true;

        if (!check_zero)
        {
            Debug.Log(string.Concat("<color=red><b>", "Zero Found...", "</b></color>"));
            // HACK: This if condition shows if the bonus section is not zero then check slot sections for zeros
            if (result_reel[result_reel.Count - 1] != 0)
            {
                // HACK: If bonus is not zero and still the zero count is greater than 0 that means for sure slots have zero values so no need to play animation
                return;
            }
            // HACK: This else part shows if the bonus section is zero then along with bonus check slots zeros too
            else
            {
                if ((zero_count - 1) == 0)
                {
                    // HACK: That means except bonus section there are no zeros in slot section so play the combo animations for slots
                    PlaySpriteAnimation(false, result_reel);
                }
            }
        }
        // HACK: Got the slots + bonus boiii...
        else
        {
            // TODO: Play Slots and Bonus Animations.
            Debug.Log(string.Concat("<color=green><b>", "Zero Not Found...", "</b></color>"));

            if (CheckCombo(result_reel))
            {
                m_Bonus_Found = true;
                m_Speed_Control = 0.01f;
            }

            m_UIManager.GetGameObject(m_Key.m_object_normal_win_line).SetActive(false);
            m_UIManager.GetGameObject(m_Key.m_object_animated_win_line).SetActive(true);
            m_UIManager.GetGameObject(m_Key.m_object_animated_win_line).GetComponent<ImageAnimation>().StartAnimation();
            PlaySpriteAnimation(true, result_reel);
        }
    }

    private bool CheckCombo(List<int> m_reel)
    {
        int d = m_reel[0];
        int c = 0;
        for(int i = 0; i < m_reel.Count - 1; i ++)
        {
            if(m_reel[i] == d)
            {
                c++;
            }
        }
        if (c == 3) return true;
        else return false;
    }

    private void PlaySpriteAnimation(bool m_config, List<int> m_reel)
    {
        for(int i = 0; i < m_reel.Count; i++)
        {
            if (m_config)
            {
                if (i < m_reel.Count - 1)
                {
                    ImageAnimation m_anim_obj = Tempimages[i].slotImages[0].gameObject.GetComponent<ImageAnimation>();
                    SlotAnimationsSwitch(true, m_reel[i], m_anim_obj);
                    Debug.Log(string.Concat("<color=cyan><b>", "Bonus Available...", "</b></color>"));
                }
                else if(i == m_reel.Count - 1)
                {
                    ImageAnimation m_anim_obj = Tempimages[i].slotImages[0].gameObject.GetComponent<ImageAnimation>();
                    SlotAnimationsSwitch(false, m_reel[i], m_anim_obj);
                    Debug.Log(string.Concat("<color=green><b>", "Bonus Found...", "</b></color>"));
                }
            }
            else
            {
                if (i < m_reel.Count - 1)
                {
                    ImageAnimation m_anim_obj = Tempimages[i].slotImages[0].gameObject.GetComponent<ImageAnimation>();
                    SlotAnimationsSwitch(true, m_reel[i], m_anim_obj);
                    Debug.Log(string.Concat("<color=cyan><b>", "Bonus Not Found...", "</b></color>"));
                }
            }
        }
    }

    private void SlotAnimationsSwitch(bool m_config_slot_bonus, int slot_id, ImageAnimation m_anim_object)
    {
        // If Slots
        if (m_config_slot_bonus)
        {
            switch (slot_id)
            {
                case 1:
                    m_anim_object.textureArray = GetSlotAnimationList(m_Key.m_anim_slot_combo7);
                    m_anim_object.StartAnimation();
                    Transform m_obj = m_UIManager.GetGameObject(m_Key.m_object_single7_combo).transform.GetChild(0);
                    int child_count = m_obj.childCount;
                    for(int i = 0; i < child_count; i++)
                    {
                        ImageAnimation m_anim = m_obj.GetChild(i).GetComponent<ImageAnimation>();
                        m_anim.StartAnimation();
                        m_imageAnimations.Add(m_anim);
                    }
                    break;
                case 2:
                    m_anim_object.textureArray = GetSlotAnimationList(m_Key.m_anim_slot_combo77);
                    m_anim_object.StartAnimation();
                    break;
                case 3:
                    m_anim_object.textureArray = GetSlotAnimationList(m_Key.m_anim_slot_combo777);
                    m_anim_object.StartAnimation();
                    break;
                case 4:
                    m_anim_object.textureArray = GetSlotAnimationList(m_Key.m_anim_slot_bar);
                    m_anim_object.StartAnimation();
                    break;
                case 5:
                    m_anim_object.textureArray = GetSlotAnimationList(m_Key.m_anim_slot_bar_bar);
                    m_anim_object.StartAnimation();
                    break;
            }
        }
        // If Bonus
        else
        {
            Debug.Log(string.Concat("<color=yellow><b>", "Bonus Called", "</b></color>"));

            switch (slot_id)
            {
                case 1:
                    m_anim_object.textureArray = GetBonusAnimationList(m_Key.m_anim_bonus_dollar);
                    m_anim_object.StartAnimation();
                    break;
                case 2:
                    m_anim_object.textureArray = GetBonusAnimationList(m_Key.m_anim_bonus_dollar2);
                    m_anim_object.StartAnimation();
                    break;
                case 3:
                    m_anim_object.textureArray = GetBonusAnimationList(m_Key.m_anim_bonus_2X);
                    m_anim_object.StartAnimation();
                    break;
                case 4:
                    m_anim_object.textureArray = GetBonusAnimationList(m_Key.m_anim_bonus_5X);
                    m_anim_object.StartAnimation();
                    break;
                case 5:
                    m_anim_object.textureArray = GetBonusAnimationList(m_Key.m_anim_bonus_10X);
                    m_anim_object.StartAnimation();
                    break;
                case 6:
                    m_anim_object.textureArray = GetBonusAnimationList(m_Key.m_anim_bonus_respin);
                    m_anim_object.StartAnimation();
                    break;
            }
        }
    }

    #endregion

    internal void DeactivateGamble()
    {
        StopAutoSpin();
    }

    internal void GambleCollect()
    {
        SocketManager.GambleCollectCall();
    }

    internal void CheckWinPopups()
    {
        if (SocketManager.resultData.WinAmout >= currentTotalBet * 10 && SocketManager.resultData.WinAmout < currentTotalBet * 15)
        {
            //uiManager.PopulateWin(1, SocketManager.resultData.WinAmout);
        }
        else if (SocketManager.resultData.WinAmout >= currentTotalBet * 15 && SocketManager.resultData.WinAmout < currentTotalBet * 20)
        {
            //uiManager.PopulateWin(2, SocketManager.resultData.WinAmout);
        }
        else if (SocketManager.resultData.WinAmout >= currentTotalBet * 20)
        {
            //uiManager.PopulateWin(3, SocketManager.resultData.WinAmout);
        }
        else
        {
            CheckPopups = false;
        }
    }

    void ToggleButtonGrp(bool toggle)
    {
        m_UIManager.GetButton(m_Key.m_button_spin).interactable = toggle;
        m_UIManager.GetButton(m_Key.m_button_bet_button).interactable = toggle;
        m_UIManager.GetButton(m_Key.m_button_auto_spin).interactable = toggle;

    }

    internal void updateBalance()
    {
        m_UIManager.GetText(m_Key.m_text_balance_amount).text = SocketManager.playerdata.Balance.ToString("f2");
        m_UIManager.GetText(m_Key.m_text_win_amount).text = SocketManager.playerdata.currentWining.ToString("f2");
    }

    private void StartGameAnimation(GameObject animObjects)
    {
        //if (animObjects.transform.GetComponent<ImageAnimation>().isActiveAndEnabled)
        //{

        animObjects.transform.GetChild(0).gameObject.SetActive(true);
        animObjects.transform.GetChild(1).gameObject.SetActive(true);
        //}

        ImageAnimation temp = animObjects.transform.GetChild(0).GetComponent<ImageAnimation>();

        temp.StartAnimation();
        TempList.Add(temp);
    }

    private void StopGameAnimation()
    {
        for (int i = 0; i < TempList.Count; i++)
        {
            TempList[i].StopAnimation();
            if (TempList[i].transform.parent.childCount > 0)
                TempList[i].transform.parent.GetChild(1).gameObject.SetActive(false);
        }
        TempList.Clear();
        TempList.TrimExcess();
    }

    internal void CallCloseSocket()
    {
        SocketManager.CloseSocket();
    }

    #region TweeningCode
    private void InitializeTweening(Transform slotTransform)
    {
        slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, slotTransform.localPosition.y - tweenHeight);
        Tweener tweener;
        if (m_GameManager.TurboSpin)
        {
            tweener = slotTransform.DOLocalMoveY(tweenHeight, 0.5f).SetLoops(1, LoopType.Restart).SetDelay(0);
        }
        else
        {
            tweener = slotTransform.DOLocalMoveY(tweenHeight, m_Speed_Control).SetLoops(-1, LoopType.Restart).SetDelay(0);
        }

        tweener.Play();
        alltweens.Add(tweener);
    }

    private IEnumerator StopTweening(int reqpos, Transform slotTransform, int index)
    {
        alltweens[index].Pause();
        int tweenpos = (reqpos * (IconSizeFactor + SpaceFactor)) - (IconSizeFactor + (2 * SpaceFactor));
        if (m_GameManager.TurboSpin)
        {
            alltweens[index] = slotTransform.DOLocalMoveY(-tweenpos + 100 + (SpaceFactor > 0 ? SpaceFactor / 4 : 0), 0f).SetLoops(1).SetDelay(0);
        }
        else
        {
            alltweens[index] = slotTransform.DOLocalMoveY(-tweenpos + 100 + (SpaceFactor > 0 ? SpaceFactor / 4 : 0), 0.5f).SetEase(Ease.OutElastic);
        }
        yield return new WaitForSeconds(0.2f);
    }

    private void KillAllTweens()
    {
        for (int i = 0; i < numberOfSlots; i++)
        {
            alltweens[i].Kill();
        }
        alltweens.Clear();
    }
    #endregion

    #region Animated Sprites Handling
    private void ClearAllImageAnimations()
    {
        foreach(var i in Tempimages)
        {
            if(i.slotImages[0].GetComponent<ImageAnimation>())
                i.slotImages[0].GetComponent<ImageAnimation>().StopAnimation();
        }
    }

    private void ValidateAnimationDictionary()
    {
        UpdateSlotAnimationDictionary();
        UpdateBonusAnimationDictionary();
    }

    private void UpdateSlotAnimationDictionary()
    {
        m_animated_slot_dictionary.Clear();
        foreach (AnimatedSlots uiReference in m_animated_slot_images)
        {
            if (uiReference.animated_images != null && !m_animated_slot_dictionary.ContainsKey(uiReference.key))
            {
                m_animated_slot_dictionary.Add(uiReference.key, uiReference.animated_images);
            }
        }
    }

    internal List<Sprite> GetSlotAnimationList(string key)
    {
        //Debug.Log(string.Concat("<color=yellow><b>", key, "</b></color>"));
        if (m_animated_slot_dictionary.ContainsKey(key))
        {
            return m_animated_slot_dictionary[key];
        }
        return null;
    }

    private void UpdateBonusAnimationDictionary()
    {
        m_animated_bonus_dictionary.Clear();
        foreach(AnimatedSlots uiReference in m_animated_bonus_images)
        {
            if(uiReference.animated_images != null && !m_animated_bonus_dictionary.ContainsKey(uiReference.key))
            {
                m_animated_bonus_dictionary.Add(uiReference.key, uiReference.animated_images);
            }
        }
    }

    internal List<Sprite> GetBonusAnimationList(string key)
    {
        if (m_animated_bonus_dictionary.ContainsKey(key))
        {
            return m_animated_bonus_dictionary[key];
        }
        return null;
    }
    #endregion

    private void OnDisable()
    {
        m_GameManager.OnSpinClicked -= delegate { StartSlots(); };
        m_GameManager.OnBetButtonClicked -= delegate { OnBetOne(); };
        m_GameManager.OnAutoSpinClicked -= delegate { AutoSpin(); };
        m_GameManager.OnAutoSpinStopClicked -= delegate { StopAutoSpin(); };
    }
}

[System.Serializable]
public struct AnimatedSlots
{
    public string key;
    public List<Sprite> animated_images;
}