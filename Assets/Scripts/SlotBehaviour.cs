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
    [Header("Booleans Ref")]
    private bool IsAutoSpin = false;
    private bool IsFreeSpin = false;
    private bool IsSpinning = false;
    internal bool CheckPopups = false;
    internal bool WinLine = false;
    internal bool StopSpinToggle;
    private bool IsTurboOn;
    [SerializeField] private bool WasAutoSpinOn;
    #endregion

    #region Numbers
    [Header("Numbers Ref")]
    [SerializeField]
    internal int BetCounter = 0;
    private float SpinDelay = 0.2f;
    private double currentBalance = 0;
    private double currentTotalBet = 0;
    int Lines = 3;

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
    [Header("Coroutines")]
    Coroutine AutoSpinRoutine = null;
    Coroutine tweenroutine = null;
    Coroutine FreeSpinRoutine = null;
    #endregion

    #region Audio Sources
    [Header("Audio Sources")]
    private AudioSource m_TempAudioPlayBack;
    #endregion
    #endregion

    #region Speed March Bonus
    [Header("Speed March Bonus")]
    [SerializeField]
    private Transform m_BonusSlot_Ref;
    [SerializeField]
    private Transform m_SpeedMarch_Bonus;

    private Tweener m_SpeedMarch_Bonus_Tween;
    private Tween BalanceTween;
    #endregion

    [SerializeField]
    private bool m_Bonus_Found = false;
    [SerializeField]
    private float m_Speed_Control = 0.2f;
    [SerializeField]
    private TMP_Text m_Auto_Spin_Count;
    private ImageAnimation m_FreeSpinAnimation;
    private List<ImageAnimation> m_SlotAnimations = new List<ImageAnimation>();
    private List<int> simulatedResultReel = new List<int>(); // Contains the result reel values


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
        tweenHeight = -(((myImages.Length) * IconSizeFactor) - 280);
    }

    private void InitiateButtons()
    {
        m_GameManager.OnSpinClicked += delegate { StartSlots(); };// Subscribing the event for the slot spin is clicked.
        m_GameManager.OnBetButtonClicked += delegate { ChangeBet(); };// Subscribing the event for bet button clicked.
        m_GameManager.OnAutoSpinClicked += delegate { AutoSpin(); /*m_Auto_Spin_Count.text = m_GameManager.AutoSpin_Count.ToString();*/ };// Subscribing the event for auto spin start
        m_GameManager.OnAutoSpinStopClicked += delegate { StopAutoSpin(); WasAutoSpinOn = false; };// Subscribing the event for auto spin stop.
    }

    internal void SetInitialUI()
    {
        BetCounter = 0;

        m_UIManager.GetText(m_Key.m_text_bet_amount).text = (SocketManager.initialData.Bets[BetCounter] * Lines).ToString();

        //TotalBet_text.text = (SocketManager.initialData.Bets[BetCounter] * Lines).ToString(); //To Be Implemented In Future

        m_UIManager.GetText(m_Key.m_text_win_amount).text = "0.00";

        m_UIManager.GetText(m_Key.m_text_balance_amount).text = SocketManager.playerdata.Balance.ToString();

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
                int randomIndex;
                if (i < 3)
                {
                    randomIndex = UnityEngine.Random.Range(1, myImages.Length - 6);
                    Tempimages[i].slotImages[j].sprite = myImages[randomIndex];
                }
                else
                {
                    randomIndex = UnityEngine.Random.Range(myImages.Length - 6, myImages.Length);
                    Tempimages[i].slotImages[j].sprite = myImages[randomIndex];
                }
            }
        }
    }

    private void StartSlots(bool autoSpin = false)
    {
        //if (audioController) audioController.PlaySpinButtonAudio();

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
        //if (audioController) audioController.PlaySpinButtonAudio();

        m_GameManager.AutoSpin_Count = int.Parse(m_UIManager.GetText(m_Key.m_text_total_auto_spin).text);

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
        //if (audioController) audioController.PlaySpinButtonAudio();

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

        //WasAutoSpinOn = false;
    }

    private IEnumerator StopAutoSpinCoroutine()
    {
        yield return new WaitUntil(() => !IsSpinning);
        ToggleButtonGrp(true);
        if (AutoSpinRoutine != null || tweenroutine != null)
        {
            try
            {
                StopCoroutine(AutoSpinRoutine);
                StopCoroutine(tweenroutine);
                tweenroutine = null;
                AutoSpinRoutine = null;
                StopCoroutine(StopAutoSpinCoroutine());
            }
            catch(Exception e)
            {
                Debug.Log("Error Occured..." + string.Concat("<color=red><b>", e, "</b></color>"));
            }
        }
    }

    private void CompareBalance()
    {
        if (currentBalance < currentTotalBet)
        {
            m_GameManager.OpenCloseLowBalancePopup(true);
            //m_UIManager.GetButton(m_Key.m_button_auto_spin).interactable = false;
            //m_UIManager.GetButton(m_Key.m_button_spin).interactable = false;
        }
        //else
        //{
        //    m_UIManager.GetButton(m_Key.m_button_auto_spin).interactable = true;
        //    m_UIManager.GetButton(m_Key.m_button_spin).interactable = true;
        //}
    }

    internal void ChangeBet()
    {
        if(BetCounter < SocketManager.initialData.Bets.Count)
        {
            m_UIManager.GetText(m_Key.m_text_bet_amount).text = (SocketManager.initialData.Bets[BetCounter] * Lines).ToString();
            //if (TotalBet_text) TotalBet_text.text = (SocketManager.initialData.Bets[BetCounter] * Lines).ToString(); // To Be Implemented

            currentTotalBet = SocketManager.initialData.Bets[BetCounter] * Lines;
            Debug.Log(currentTotalBet);
            Debug.Log(currentBalance);
            CompareBalance();
        }
    }

    internal void LayoutReset(int number)
    {
        //if (Slot_Elements[number]) Slot_Elements[number].ignoreLayout = true;
        m_UIManager.GetButton(m_Key.m_button_spin).interactable = true;
    }

    #region FREE SPIN

    internal void FreeSpin(int spins)
    {
        if (!IsFreeSpin)
        {
            if (!audioController.m_FreeSpin_Audio.mute && audioController.m_Player_Listener.enabled) audioController.m_FreeSpin_Audio.Play();
            IsFreeSpin = true;
            ToggleButtonGrp(false);

            if (FreeSpinRoutine != null)
            {
                StopCoroutine(FreeSpinRoutine);
                FreeSpinRoutine = null;
            }
            FreeSpinRoutine = StartCoroutine(FreeSpinCoroutine(spins));

        }
    }

    private IEnumerator FreeSpinCoroutine(int spinchances)
    {
        int i = 0;
        Debug.Log(string.Concat("<color=cyan><b>", "Free Spin Started... ", spinchances, "</b></color>"));
        yield return new WaitForSeconds(1f);
        while (i < spinchances)
        {
            Debug.Log(string.Concat("<color=green><i>", "Free Spin Executing...", "</i></color>"));
            StartSlots();
            yield return tweenroutine;
            yield return new WaitForSeconds(SpinDelay);
            Debug.Log(string.Concat("<color=yellow><b>", i, " : ", "</b></color>"));
            i++;
        }
        if (WasAutoSpinOn)
        {
            AutoSpin();
        }
        else
        {
            ToggleButtonGrp(true);
        }
        IsFreeSpin = false;
        m_GameManager.InvokeFreeSpinEnd();
        if (m_FreeSpinAnimation) m_FreeSpinAnimation.StopAnimation();
        m_FreeSpinAnimation = null; // Code Line For Cross Check
    }

    #endregion

    private IEnumerator TweenRoutine()
    {
        // Clear all previous animations and reset the state for a new spin
        ClearAllImageAnimations();
        ResetSlotAnimations();
        m_Bonus_Found = false;
        WinLine = false;
        m_Speed_Control = 0.6f;;

        // Display the normal win line and hide the animated win line
        m_UIManager.GetGameObject(m_Key.m_object_normal_win_line).SetActive(true);
        PlayWinLineAnimation(false);

        currentBalance = SocketManager.playerdata.Balance;
        currentTotalBet = SocketManager.initialData.Bets[BetCounter] * Lines;

        Debug.Log("Current Balance: " + currentBalance.ToString());
        Debug.Log("Current Bet: " + currentTotalBet.ToString());

        // Check if the player has enough balance to spin and if it's not a free spin
        if (currentBalance < currentTotalBet && !IsFreeSpin)
        {
            CompareBalance();
            StopAutoSpin();
            yield return new WaitForSeconds(1);
            ToggleButtonGrp(true);
            yield break;
        }

        // Play the spin audio if the audio controller is available
        //if (audioController)
        //{
        //    audioController.PlayWLAudio("spin");
        //}

        // Set spinning state to true and disable buttons during the spin
        IsSpinning = true;
        ToggleButtonGrp(false);

        //HACK: Modification For Turbo
        if (!m_GameManager.TurboSpin && !IsFreeSpin && !IsAutoSpin)
        {
            m_UIManager.GetButton(m_Key.m_button_stop_spin_button).gameObject.SetActive(true);
        }

        // Initialize tweening for each slot with a small delay between them
        for (int i = 0; i < numberOfSlots; i++)
        {
            if (!IsFreeSpin)
            {
                InitializeTweening(Slot_Transform[i]);
            }
            else
            {
                if(i < numberOfSlots - 1)
                {
                    InitializeTweening(Slot_Transform[i]);
                }
            }
            yield return new WaitForSeconds(0.01f);
        }

        // Handle the betting and balance updates
        double bet = IsFreeSpin ? 0.0 : currentTotalBet; // No bet deduction during free spins
        double balance = currentBalance;

        // Update the balance after placing the bet (only if it's not a free spin)
        if (!IsFreeSpin)
        {
            double initAmount = balance;

            balance = balance - bet;

            BalanceTween = DOTween.To(() => initAmount, (val) => initAmount = val, balance, 0.8f).OnUpdate(() =>
            {
                m_UIManager.GetText(m_Key.m_text_balance_amount).text = initAmount.ToString("f3");
            });
        }


        #region SPIN_DEMO_SIMULATION
        //HACK: For demo purposes: Custom result data (simulated results)
        //List<int> simulatedResultReel = new List<int>
        //{
        //    UnityEngine.Random.Range(0, myImages.Length),
        //    UnityEngine.Random.Range(0, myImages.Length),
        //    UnityEngine.Random.Range(0, myImages.Length),
        //    UnityEngine.Random.Range(0, myBonusImages.Length)
        //    //3, 3, 3, 0
        //}; // Custom input: Simulated slot result

        //AssignResultSpritesWin(simulatedResultReel); // Assign the simulated results to the slot and bonus slots
        #endregion

        #region SPIN_BACKEND_SIMULATION
        simulatedResultReel.Clear();
        simulatedResultReel.TrimExcess();
        SocketManager.AccumulateResult(BetCounter);
        currentBalance = SocketManager.playerdata.Balance;
        yield return new WaitUntil(() => SocketManager.isResultdone);
        foreach (var m in SocketManager.resultData.resultSymbols[1])
        {
            simulatedResultReel.Add(m);
        }
        Debug.Log(string.Concat("<color=green>From Slot Machine - Upper: ", string.Join(", ", SocketManager.resultData.resultSymbols[0]), "</color>"));
        Debug.Log(string.Concat("<color=green>From Slot Machine - Middle: ", string.Join(", ", simulatedResultReel),"</color>"));
        Debug.Log(string.Concat("<color=green>From Slot Machine - Lower: ", string.Join(", ", SocketManager.resultData.resultSymbols[2]), "</color>"));
        AssignResultSpritesWin(SocketManager.resultData.resultSymbols[1], SocketManager.resultData.resultSymbols[0], SocketManager.resultData.resultSymbols[2]); // Assign the simulated results to the slot and bonus slots
        #endregion

        //HACK: Code for the delay between the start and stop tweening routines
        //if (m_GameManager.TurboSpin)
        //{
        //    yield return new WaitForSeconds(0f);
        //}
        //else
        //{
        //    yield return new WaitForSeconds(0.5f);
        //}

        if (m_GameManager.TurboSpin || IsFreeSpin)
        {
            yield return new WaitForSeconds(0.1f);
        }
        else
        {
            for (int i = 0; i < 5; i++)
            {
                yield return new WaitForSeconds(0.1f);
                if (StopSpinToggle)
                {
                    break;
                }
            }
            m_UIManager.GetButton(m_Key.m_button_stop_spin_button).gameObject.SetActive(false);
        }

        // Stop all tweens running for each slot
        for (int i = 0; i < numberOfSlots - 1; i++)
        {
            yield return StopTweening(5, Slot_Transform[i], i, simulatedResultReel[i] != 0 ? 0 : m_GameManager.StopPos_Plus, StopSpinToggle);
        }
        if (!IsFreeSpin)
        {
            if (m_Bonus_Found && !m_GameManager.TurboSpin)
            {
                //yield return new WaitForSeconds(0.5f);
                yield return StopTweening(5, Slot_Transform[numberOfSlots - 1], numberOfSlots - 1, simulatedResultReel[simulatedResultReel.Count - 1] != 0 ? 0 : m_GameManager.StopPos_Plus, StopSpinToggle);
            }
            else
            {
                yield return StopTweening(5, Slot_Transform[numberOfSlots - 1], numberOfSlots - 1, simulatedResultReel[simulatedResultReel.Count - 1] != 0 ? 0 : m_GameManager.StopPos_Plus, StopSpinToggle);
            }
        }

        StopSpinToggle = false;

        StartSlotAnimations();

        //yield return new WaitForSeconds(0.3f);

        // Optional: Logic to check payout line (To be implemented with backend data)
        // TODO: Implement backend logic to check payout line and jackpots

        // Kill all active tweens to ensure no lingering animations

        yield return alltweens[^1].WaitForCompletion();
        KillAllTweens();

        if (SocketManager.playerdata.currentWining > 0)
        {
            SpinDelay = 1.2f;
        }
        else
        {
            SpinDelay = 0.2f;
        }

        m_UIManager.GetText(m_Key.m_text_win_amount).text = SocketManager.playerdata.currentWining.ToString("F3");
        m_UIManager.GetText(m_Key.m_text_balance_amount).text = SocketManager.playerdata.Balance.ToString("F3");

        // TODO: Implement backend logic to check for bonus games and win popups
        CheckPopups = false; // Simulated: No popups for demo

        BalanceTween?.Kill();

        // Wait until all popups are dismissed
        yield return new WaitUntil(() => !CheckPopups);

        // Handle the end of the spin, either by re-enabling the buttons or starting the next auto-spin
        if (!IsAutoSpin && !IsFreeSpin)
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

        // Count number of auto spins
        //if(IsAutoSpin && !IsFreeSpin)
        //{
        //    m_Auto_Spin_Count.text = m_GameManager.AutoSpin_Count.ToString();
        //    if (m_GameManager.AutoSpin_Count > 0)
        //    {
        //        m_GameManager.AutoSpin_Count--;
        //        m_Auto_Spin_Count.text = m_GameManager.AutoSpin_Count.ToString();

        //    }
        //    else
        //    {
        //        StopAutoSpin();
        //    }
        //}

        //Free Spin
        if(SocketManager.resultData.isFreeSpin)
        {
            if (IsAutoSpin)
            {
                WasAutoSpinOn = true;
                Debug.Log($"<color=red>Free Spin In Auto Spin {WasAutoSpinOn}</color>");
                StopAutoSpin();
            }
            try
            {
                m_GameManager.InvokeFreeSpin();
                Debug.Log(string.Concat("<color=cyan><b>", "Successfully Executed... ", SocketManager.resultData.freeSpinCount, "</b></color>"));
            }
            catch(Exception e)
            {
                Debug.Log(string.Concat("<color=orange><b>", "Error Occured..." + e, "</b></color>"));
            }
        }
    }

    #region RESULT_FUNCTIONALITIES

    private void AssignResultSpritesWin(List<int> m_slot_values_mid, List<int> m_slot_value_upper, List<int> m_slot_value_lower)
    {
        for (int j = 0; j < m_slot_values_mid.Count; j++)
        {
            Tempimages[j].slotImages[0].sprite = myImages[m_slot_value_upper[j]];
            //Debug.Log(m_slot_value_upper[j]);
            Tempimages[j].slotImages[2].sprite = myImages[m_slot_value_lower[j]];
            //Debug.Log(m_slot_value_lower[j]);

            if (m_slot_values_mid[j] != 0)
            {
                if (!IsFreeSpin)
                {
                    Tempimages[j].slotImages[1].sprite = myImages[m_slot_values_mid[j]];
                }
                else
                {
                    //If Free Spin Enabled Then Donot Change The Sprite Of The Free Spin
                    if(j < m_slot_values_mid.Count - 1)
                    {
                        Tempimages[j].slotImages[1].sprite = myImages[m_slot_values_mid[j]];
                    }
                }
            }
        }

        Tempimages[Tempimages.Count - 1].slotImages[0].sprite = myImages[UnityEngine.Random.Range(6, myImages.Length - 1)];
        Tempimages[Tempimages.Count - 1].slotImages[2].sprite = myImages[UnityEngine.Random.Range(6, myImages.Length - 1)];

        CheckWin(m_slot_values_mid);
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
            //Debug.Log(string.Concat("<color=red><b>", "Zero Found...", "</b></color>"));
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
                    PlaySpriteAnimation(false, result_reel, audioController.m_Win_Audio);
                    if (CheckCombo(result_reel))
                    {
                        m_Bonus_Found = true;
                        m_Speed_Control = 0.6f;;

                        
                        PlaySpriteAnimation(true, result_reel, audioController.m_Win_Audio);
                        //audioController.m_Win_Audio.Play();
                    }
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

                //Make the last slot to be speed and run for little more time
                m_Speed_Control = 0.1f;
                //if (!IsFreeSpin)
                //{
                //    InitializeBonusTweening(m_SpeedMarch_Bonus);
                //}
            }

            PlaySpriteAnimation(true, result_reel, audioController.m_Bonus_Audio);
            //audioController.m_Bonus_Audio.Play();
        }
    }

    private void PlayWinLineAnimation(bool m_Play_Pause)
    {
        if (m_Play_Pause)
        {
            m_UIManager.GetGameObject(m_Key.m_object_animated_win_line).SetActive(true);
            m_UIManager.GetGameObject(m_Key.m_object_animated_win_line).GetComponent<ImageAnimation>().StartAnimation();
        }
        else
        {
            m_UIManager.GetGameObject(m_Key.m_object_animated_win_line).SetActive(false);
            m_UIManager.GetGameObject(m_Key.m_object_animated_win_line).GetComponent<ImageAnimation>().StopAnimation();
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

    private void PlaySpriteAnimation(bool m_config, List<int> m_reel, AudioSource m_play_audio)
    {
        for(int i = 0; i < m_reel.Count; i++)
        {
            if (m_config)
            {
                if (i < m_reel.Count - 1)
                {
                    ImageAnimation m_anim_obj = Tempimages[i].slotImages[1].gameObject.GetComponent<ImageAnimation>();
                    SlotAnimationsSwitch(true, m_reel[i], m_anim_obj, m_play_audio);
                    //Debug.Log(string.Concat("<color=cyan><b>", "Bonus Available...", "</b></color>"));
                }
                else if(i == m_reel.Count - 1)
                {
                    ImageAnimation m_anim_obj = Tempimages[i].slotImages[1].gameObject.GetComponent<ImageAnimation>();
                    SlotAnimationsSwitch(false, m_reel[i], m_anim_obj, m_play_audio);
                    //Debug.Log(string.Concat("<color=green><b>", "Bonus Found...", "</b></color>"));
                }
            }
            else
            {
                if (i < m_reel.Count - 1)
                {
                    ImageAnimation m_anim_obj = Tempimages[i].slotImages[1].gameObject.GetComponent<ImageAnimation>();
                    SlotAnimationsSwitch(true, m_reel[i], m_anim_obj, m_play_audio);
                    Debug.Log(string.Concat("<color=cyan><b>", "Bonus Not Found...", "</b></color>"));
                }
            }
        }

        WinLine = true;
    }

    private void SlotAnimationsSwitch(bool m_config_slot_bonus, int slot_id, ImageAnimation m_anim_object, AudioSource m_play_audio)
    {
        // If Slots
        if (m_config_slot_bonus)
        {
            Transform m_obj;
            int child_count;
            switch (slot_id)
            {
                case 1:
                    m_anim_object.textureArray = GetSlotAnimationList(m_Key.m_anim_slot_combo777);
                    //m_anim_object.StartAnimation();
                    m_SlotAnimations.Add(m_anim_object);

                    if (m_Bonus_Found)
                    {
                        m_obj = m_UIManager.GetGameObject(m_Key.m_object_triple7_combo).transform.GetChild(0);
                        child_count = m_obj.childCount;
                        for (int i = 0; i < child_count; i++)
                        {
                            ImageAnimation m_anim = m_obj.GetChild(i).GetComponent<ImageAnimation>();
                            //m_anim.StartAnimation();
                            m_SlotAnimations.Add(m_anim);
                        }
                    }
                    break;
                case 2:
                    m_anim_object.textureArray = GetSlotAnimationList(m_Key.m_anim_slot_combo77);
                    //m_anim_object.StartAnimation();
                    m_SlotAnimations.Add(m_anim_object);

                    if (m_Bonus_Found)
                    {
                        m_obj = m_UIManager.GetGameObject(m_Key.m_object_double7_combo).transform.GetChild(0);
                        child_count = m_obj.childCount;
                        for (int i = 0; i < child_count; i++)
                        {
                            ImageAnimation m_anim = m_obj.GetChild(i).GetComponent<ImageAnimation>();
                            //m_anim.StartAnimation();
                            m_SlotAnimations.Add(m_anim);
                        }
                    }
                    break;
                case 3:
                    m_anim_object.textureArray = GetSlotAnimationList(m_Key.m_anim_slot_combo7);
                    //m_anim_object.StartAnimation();
                    m_SlotAnimations.Add(m_anim_object);

                    if (m_Bonus_Found)
                    {
                        m_obj = m_UIManager.GetGameObject(m_Key.m_object_single7_combo).transform.GetChild(0);
                        child_count = m_obj.childCount;
                        for (int i = 0; i < child_count; i++)
                        {
                            ImageAnimation m_anim = m_obj.GetChild(i).GetComponent<ImageAnimation>();
                            //m_anim.StartAnimation();
                            m_SlotAnimations.Add(m_anim);
                        }
                    }
                    break;
                case 4:
                    m_anim_object.textureArray = GetSlotAnimationList(m_Key.m_anim_slot_bar_bar);
                    //m_anim_object.StartAnimation();
                    m_SlotAnimations.Add(m_anim_object);

                    if (m_Bonus_Found)
                    {
                        m_obj = m_UIManager.GetGameObject(m_Key.m_object_double_bar_combo).transform.GetChild(0);
                        child_count = m_obj.childCount;
                        for (int i = 0; i < child_count; i++)
                        {
                            ImageAnimation m_anim = m_obj.GetChild(i).GetComponent<ImageAnimation>();
                            //m_anim.StartAnimation();
                            m_SlotAnimations.Add(m_anim);
                        }
                    }
                    break;
                case 5:
                    m_anim_object.textureArray = GetSlotAnimationList(m_Key.m_anim_slot_bar);
                    //m_anim_object.StartAnimation();
                    m_SlotAnimations.Add(m_anim_object);

                    if (m_Bonus_Found)
                    {
                        m_obj = m_UIManager.GetGameObject(m_Key.m_object_single_bar_combo).transform.GetChild(0);
                        child_count = m_obj.childCount;
                        for (int i = 0; i < child_count; i++)
                        {
                            ImageAnimation m_anim = m_obj.GetChild(i).GetComponent<ImageAnimation>();
                            //m_anim.StartAnimation();
                            m_SlotAnimations.Add(m_anim);
                        }
                    }
                    break;
            }
        }
        // If Bonus
        else
        {
            //Debug.Log(string.Concat("<color=yellow><b>", "Bonus Called", "</b></color>"));
            ImageAnimation m_anim;
            switch (slot_id)
            {
                case 6:
                    m_anim_object.textureArray = GetBonusAnimationList(m_Key.m_anim_bonus_10X);
                    m_anim_object.StartAnimation();
                    m_anim = m_UIManager.GetGameObject(m_Key.m_object_10x_anim).GetComponent<ImageAnimation>();
                    //m_anim.StartAnimation();
                    m_SlotAnimations.Add(m_anim);
                    break;
                case 7:
                    m_anim_object.textureArray = GetBonusAnimationList(m_Key.m_anim_bonus_5X);
                    m_anim_object.StartAnimation();
                    m_anim = m_UIManager.GetGameObject(m_Key.m_object_5x_anim).GetComponent<ImageAnimation>();
                    //m_anim.StartAnimation();
                    m_SlotAnimations.Add(m_anim);
                    break;
                case 8:
                    m_anim_object.textureArray = GetBonusAnimationList(m_Key.m_anim_bonus_2X);
                    m_anim_object.StartAnimation();
                    m_anim = m_UIManager.GetGameObject(m_Key.m_object_2x_anim).GetComponent<ImageAnimation>();
                    //m_anim.StartAnimation();
                    m_SlotAnimations.Add(m_anim);
                    break;
                case 9:
                    m_anim_object.textureArray = GetBonusAnimationList(m_Key.m_anim_bonus_dollar2);
                    m_anim_object.StartAnimation();
                    m_anim = m_UIManager.GetGameObject(m_Key.m_object_2dollar_anim).GetComponent<ImageAnimation>();
                    //m_anim.StartAnimation();
                    m_SlotAnimations.Add(m_anim);
                    break;
                case 10:
                    m_anim_object.textureArray = GetBonusAnimationList(m_Key.m_anim_bonus_dollar);
                    m_anim_object.StartAnimation();
                    m_anim = m_UIManager.GetGameObject(m_Key.m_object_1dollar_anim).GetComponent<ImageAnimation>();
                    //m_anim.StartAnimation();
                    m_SlotAnimations.Add(m_anim);
                    break;
                case 11:
                    m_anim_object.textureArray = GetBonusAnimationList(m_Key.m_anim_bonus_respin);
                    m_anim_object.StartAnimation();
                    m_anim = m_UIManager.GetGameObject(m_Key.m_object_respin_anim).GetComponent<ImageAnimation>();
                    //m_anim.StartAnimation();
                    //HACK: This code is used to push the animation to the stack so that all could erased on next tween. But we need in case of free spin to be the animation played
                    //m_SlotAnimations.Add(m_anim);
                    m_FreeSpinAnimation = m_anim;
                    break;
            }
        }

        m_TempAudioPlayBack = m_play_audio;
    }

    private void StartSlotAnimations()
    {
        foreach (var i in m_SlotAnimations)
        {
            i.StartAnimation();
        }
        if (WinLine)
        {
            PlayWinLineAnimation(true);
            if(!m_TempAudioPlayBack.mute && audioController.m_Player_Listener.enabled) m_TempAudioPlayBack.Play();
            m_UIManager.GetGameObject(m_Key.m_object_normal_win_line).SetActive(false);
        }
        if (IsFreeSpin)
        {
            if(m_FreeSpinAnimation) m_FreeSpinAnimation.StartAnimation();
        }
    }

    private void ResetSlotAnimations()
    {
        foreach(var i in m_SlotAnimations)
        {
            i.StopAnimation();
        }
        m_SlotAnimations.Clear();
    }

    #endregion

    internal void DeactivateGamble()
    {
        StopAutoSpin();
    }

    internal void CheckWinPopups()
    {
        //if (SocketManager.resultData.WinAmout >= currentTotalBet * 10 && SocketManager.resultData.WinAmout < currentTotalBet * 15)
        //{
        //    //uiManager.PopulateWin(1, SocketManager.resultData.WinAmout);
        //}
        //else if (SocketManager.resultData.WinAmout >= currentTotalBet * 15 && SocketManager.resultData.WinAmout < currentTotalBet * 20)
        //{
        //    //uiManager.PopulateWin(2, SocketManager.resultData.WinAmout);
        //}
        //else if (SocketManager.resultData.WinAmout >= currentTotalBet * 20)
        //{
        //    //uiManager.PopulateWin(3, SocketManager.resultData.WinAmout);
        //}
        //else
        //{
            CheckPopups = false;
        //}
    }

    void ToggleButtonGrp(bool toggle)
    {
        m_UIManager.GetButton(m_Key.m_button_spin).interactable = toggle;
        m_UIManager.GetButton(m_Key.m_button_bet_button).interactable = toggle;
        m_UIManager.GetButton(m_Key.m_button_auto_spin).interactable = toggle;

        if (!toggle)
        {
            m_UIManager.GetGameObject(m_Key.m_object_bet_panel).SetActive(toggle);
        }
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

    #region [[===TWEENING CODE===]]
    //HACK: TWEENING CODE
    private void InitializeTweening(Transform slotTransform)
    {
        slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, 0);
        Tweener tweener;
        if (m_GameManager.TurboSpin)
        {
            tweener = slotTransform.DOLocalMoveY(tweenHeight, 0.5f).SetLoops(-1, LoopType.Restart).SetEase(Ease.Linear).SetDelay(0);
        }
        else
        {
            tweener = slotTransform.DOLocalMoveY(tweenHeight, 0.7f).SetLoops(-1, LoopType.Restart).SetEase(Ease.Linear).SetDelay(0);
        }

        tweener.Play();
        alltweens.Add(tweener);
    }

    private IEnumerator StopTweening(int reqpos, Transform slotTransform, int index, int m_StopPos, bool stopImmediately)
    {
        bool IsRegister = false;

        // Wait for the tween to register completion callback
        yield return alltweens[index].OnStepComplete(delegate { IsRegister = true; });

        // Wait until the step completes
        yield return new WaitUntil(() => IsRegister);

        if (stopImmediately)
        {
            // Move the transform to its final position instantly to simulate completion
            int tweenpos = (reqpos * (IconSizeFactor + SpaceFactor)) - (IconSizeFactor + (2 * SpaceFactor)) - 20;
            float finalYPosition = -tweenpos + (100 + m_StopPos) + (SpaceFactor > 0 ? SpaceFactor / 4 : 0);
            slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, finalYPosition, slotTransform.localPosition.z);

            // Kill the tween to stop it completely
            alltweens[index]?.Kill();
            yield break;
        }

        alltweens[index]?.Kill();

        int tweenpos1 = (reqpos * (IconSizeFactor + SpaceFactor)) - (IconSizeFactor + (2 * SpaceFactor)) - 20;

        if (m_GameManager.TurboSpin)
        {
            alltweens[index] = slotTransform.DOLocalMoveY((-tweenpos1 + (100 + m_StopPos)) + (SpaceFactor > 0 ? SpaceFactor / 4 : 0), 0.3f).SetEase(Ease.OutQuad);
        }
        else
        {
            alltweens[index] = slotTransform.DOLocalMoveY((-tweenpos1 + (100 + m_StopPos)) + (SpaceFactor > 0 ? SpaceFactor / 4 : 0), 0.5f).SetEase(Ease.OutQuad);
        }

        // Wait for the tween to complete
        yield return alltweens[index].WaitForCompletion();
        alltweens[index]?.Kill();
        //StopBonusTweening();
    }



    #region Bonus Tweening
    //private void InitializeBonusTweening(Transform slotTransform)
    //{
    //    m_BonusSlot_Ref.gameObject.SetActive(false);
    //    m_SpeedMarch_Bonus.gameObject.SetActive(true);

    //    slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, 0);
    //    Tweener tweener;
    //    tweener = slotTransform.DOLocalMoveY(tweenHeight, 0.2f).SetLoops(-1, LoopType.Restart).SetEase(Ease.Linear).SetDelay(0);
    //    tweener.Play();
    //    m_SpeedMarch_Bonus_Tween = tweener;
    //}

    //private void StopBonusTweening()
    //{
    //    m_SpeedMarch_Bonus_Tween.Kill();
    //    m_BonusSlot_Ref.gameObject.SetActive(true);
    //    m_SpeedMarch_Bonus.gameObject.SetActive(false);
    //}
    #endregion

    private void KillAllTweens()
    {
        for (int i = 0; i < alltweens.Count; i++)
        {
            alltweens[i].Kill();
        }
        alltweens.Clear();
    }
    #endregion

    #region Animated Sprites Handling
    private void ClearAllImageAnimations()
    {
        for(int i = 0; i < Tempimages.Count; i ++)
        {
            if(i < Tempimages.Count - 1)
            {
                if (Tempimages[i].slotImages[1].GetComponent<ImageAnimation>())
                {
                    Tempimages[i].slotImages[1].GetComponent<ImageAnimation>().StopAnimation();
                }
            }
            else if (!IsFreeSpin)
            {
                Tempimages[i].slotImages[1].GetComponent<ImageAnimation>().StopAnimation();
            }
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
        m_GameManager.OnBetButtonClicked -= delegate { ChangeBet(); };
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